using System.Data.Common;
using System.Security.Cryptography;
using Marrow.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Marrow.API.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta la API real contra marrow_test. Solo se obtiene mediante <see cref="CrearVerificadaAsync"/>,
/// así que no existe una fábrica conectable sin haber pasado las protecciones.
/// </summary>
public sealed class FabricaMarrow : WebApplicationFactory<Program>
{
    public const string EntornoDePruebas = "Testing";
    public const string ProveedorEsperado = "Npgsql.EntityFrameworkCore.PostgreSQL";

    // Apunta a un host reservado que no existe y a una base inventada: no puede alcanzar ningún servidor.
    private const string CadenaSinBase =
        "Host=host-ficticio.invalid;Port=1;Database=base_ficticia;Username=usuario_ficticio;Password=ficticia";

    private readonly string conexionValidada;
    private readonly string claveJwt = RandomNumberGenerator.GetHexString(64);

    private FabricaMarrow(string conexionValidada)
    {
        this.conexionValidada = conexionValidada;
    }

    public static Task<FabricaMarrow> CrearVerificadaAsync(
        Func<string, string?>? leerVariable = null,
        CancellationToken cancellationToken = default) =>
        CrearVerificadaCoreAsync(
            leerVariable,
            LeerIdentidadSoloLecturaAsync,
            ConstruirYVerificarAsync,
            cancellationToken);

    /// <summary>Punto de sustitución para dobles de prueba: los pasos de validación no se pueden omitir.</summary>
    internal static async Task<FabricaMarrow> CrearVerificadaCoreAsync(
        Func<string, string?>? leerVariable,
        Func<string, CancellationToken, Task<IdentidadPostgres>> leerIdentidad,
        Func<string, CancellationToken, Task<FabricaMarrow>> construir,
        CancellationToken cancellationToken = default)
    {
        var conexion = leerVariable is null
            ? ProteccionBaseDePruebas.ObtenerConexionDelEntorno()
            : ProteccionBaseDePruebas.ObtenerConexionValidada(leerVariable);

        var identidad = await leerIdentidad(conexion, cancellationToken);
        VerificacionIdentidadPostgres.Verificar(identidad);

        return await construir(conexion, cancellationToken);
    }

    /// <summary>
    /// Levanta la API sin conectarse a ninguna base, para comprobar su configuración.
    /// No recibe cadena: siempre usa una ficticia, así que no puede apuntar a un servidor real.
    /// </summary>
    internal static FabricaMarrow CrearSinBaseParaPruebas() => new(CadenaSinBase);

    /// <summary>Consulta la identidad en una sesión propia, forzada a solo lectura por el servidor.</summary>
    public static Task<IdentidadPostgres> LeerIdentidadSoloLecturaAsync(
        string conexionValidada,
        CancellationToken cancellationToken = default) =>
        LeerIdentidadConFabricaAsync(conexionValidada, cadena => new NpgsqlConnection(cadena), cancellationToken);

    /// <summary>Punto de sustitución para dobles de prueba: la validación de la cadena siempre se ejecuta.</summary>
    internal static async Task<IdentidadPostgres> LeerIdentidadConFabricaAsync(
        string conexionValidada,
        Func<string, DbConnection> crearConexion,
        CancellationToken cancellationToken = default)
    {
        ProteccionBaseDePruebas.ExigirCadenaValida(conexionValidada);

        var soloLectura = new NpgsqlConnectionStringBuilder(conexionValidada)
        {
            Options = "-c default_transaction_read_only=on"
        };

        await using var conexion = crearConexion(soloLectura.ConnectionString);
        await conexion.OpenAsync(cancellationToken);
        return await VerificacionIdentidadPostgres.LeerIdentidadAsync(conexion, cancellationToken);
    }

    /// <summary>Compara sin exponer la cadena: el resultado es solo verdadero o falso.</summary>
    public bool EsLaConexionValidada(string? cadena) =>
        string.Equals(cadena, conexionValidada, StringComparison.Ordinal);

    public bool UsaLaConexionValidada(MarrowDbContext context) =>
        string.Equals(context.Database.ProviderName, ProveedorEsperado, StringComparison.Ordinal)
        && EsLaConexionValidada(context.Database.GetConnectionString());

    /// <summary>
    /// Única forma de variar los servicios de la API. Los servicios no pueden tocar EF Core, y se repiten
    /// las verificaciones sobre el resultado. Las futuras pruebas de rollback necesitarán inyectar fallas:
    /// se diseñará para ellas un mecanismo cerrado y específico, no un parámetro genérico.
    /// </summary>
    public async Task<FabricaVariada> CrearVariacionVerificadaAsync(
        Action<IServiceCollection> configurarServicios,
        CancellationToken cancellationToken = default)
    {
        var variacion = base.WithWebHostBuilder(builder => builder.ConfigureTestServices(servicios =>
        {
            configurarServicios(servicios);
            ExigirRegistroEfCoreUnico(servicios);
        }));

        try
        {
            await VerificarConexionDeLaApiAsync(variacion.Services, cancellationToken);
            return new FabricaVariada(variacion);
        }
        catch
        {
            await variacion.DisposeAsync();
            throw;
        }
    }

    /// <summary>Oculta el método heredado, que permitiría variar la API sin verificarla.</summary>
    public new WebApplicationFactory<Program> WithWebHostBuilder(Action<IWebHostBuilder> configuration) =>
        throw new NotSupportedException("Usá CrearVariacionVerificadaAsync para variar la API de pruebas.");

    /// <summary>Oculta la lista heredada, que daría acceso a fábricas internas sin envoltura.</summary>
    public new IReadOnlyList<WebApplicationFactory<Program>> Factories =>
        throw new NotSupportedException("Las variaciones solo se exponen mediante FabricaVariada.");

    /// <summary>Indica si los servicios registran exactamente un MarrowDbContext con una sola configuración.</summary>
    public static bool TieneRegistroEfCoreUnico(IEnumerable<ServiceDescriptor> servicios)
    {
        var lista = servicios.ToList();
        var contextos = lista.Where(servicio => servicio.ServiceType == typeof(MarrowDbContext)).ToList();

        return contextos.Count == 1
            && contextos[0].ImplementationType == typeof(MarrowDbContext)
            && lista.Count(servicio => servicio.ServiceType == typeof(DbContextOptions<MarrowDbContext>)) == 1
            && lista.Count(servicio =>
                servicio.ServiceType == typeof(IDbContextOptionsConfiguration<MarrowDbContext>)) == 1
            && !lista.Any(servicio => EsRegistroDeOtroContexto(servicio.ServiceType));
    }

    /// <summary>Quita el registro de Program.cs y deja como única configuración la conexión indicada.</summary>
    internal static void ReemplazarRegistroEfCore(IServiceCollection servicios, string conexion)
    {
        servicios.RemoveAll<DbContextOptions<MarrowDbContext>>();
        servicios.RemoveAll<IDbContextOptionsConfiguration<MarrowDbContext>>();
        servicios.AddDbContext<MarrowDbContext>(opciones => opciones.UseNpgsql(conexion));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EntornoDePruebas);

        // Sin appsettings, User Secrets ni variables de entorno: no hay ninguna cadena que heredar.
        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.Sources.Clear();
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = claveJwt,
                ["Jwt:Issuer"] = "Marrow.IntegrationTests",
                ["Jwt:Audience"] = "Marrow.IntegrationTests",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureTestServices(servicios =>
        {
            ReemplazarRegistroEfCore(servicios, conexionValidada);
            ExigirRegistroEfCoreUnico(servicios);
        });
    }

    private static async Task<FabricaMarrow> ConstruirYVerificarAsync(
        string conexion,
        CancellationToken cancellationToken)
    {
        var fabrica = new FabricaMarrow(conexion);
        try
        {
            await fabrica.VerificarConexionDeLaApiAsync(fabrica.Services, cancellationToken);
            return fabrica;
        }
        catch
        {
            await fabrica.DisposeAsync();
            throw;
        }
    }

    private static void ExigirRegistroEfCoreUnico(IServiceCollection servicios)
    {
        if (!TieneRegistroEfCoreUnico(servicios))
        {
            throw new ConexionDePruebaRechazadaException([MotivoRechazoConexion.RegistroEfCoreAlternativo]);
        }
    }

    private static bool EsRegistroDeOtroContexto(Type tipo)
    {
        if (tipo != typeof(MarrowDbContext) && typeof(DbContext).IsAssignableFrom(tipo))
        {
            return true;
        }

        if (!tipo.IsGenericType)
        {
            return false;
        }

        var definicion = tipo.GetGenericTypeDefinition();
        return definicion == typeof(IDbContextFactory<>)
            || ((definicion == typeof(DbContextOptions<>) || definicion == typeof(IDbContextOptionsConfiguration<>))
                && tipo.GetGenericArguments()[0] != typeof(MarrowDbContext));
    }

    private async Task VerificarConexionDeLaApiAsync(
        IServiceProvider servicios,
        CancellationToken cancellationToken)
    {
        await using var alcance = servicios.CreateAsyncScope();
        var context = alcance.ServiceProvider.GetRequiredService<MarrowDbContext>();
        if (!UsaLaConexionValidada(context))
        {
            throw new ConexionDePruebaRechazadaException([MotivoRechazoConexion.ConexionDeLaApiDistinta]);
        }

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await VerificacionIdentidadPostgres.VerificarConexionAsync(
                context.Database.GetDbConnection(),
                cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}

/// <summary>Variación ya verificada de la API. No expone la fábrica interna para que no se pueda volver a variar.</summary>
public sealed class FabricaVariada : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> interna;

    /// <summary>ArquitecturaTests exige que solo FabricaMarrow lo use.</summary>
    internal FabricaVariada(WebApplicationFactory<Program> interna)
    {
        this.interna = interna;
    }

    public IServiceProvider Services => interna.Services;

    public HttpClient CreateClient() => interna.CreateClient();

    public ValueTask DisposeAsync() => interna.DisposeAsync();
}
