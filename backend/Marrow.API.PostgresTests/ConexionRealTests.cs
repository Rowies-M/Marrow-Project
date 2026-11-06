using System.Net;
using System.Net.Http.Headers;
using Marrow.API.Data;
using Marrow.API.IntegrationTests.Infraestructura;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marrow.API.PostgresTests;

// Solo lectura contra marrow_test. Ninguna prueba crea tablas ni modifica datos.
[Collection(ColeccionIntegracion.Nombre)]
public class ConexionRealTests(EntornoIntegracion entorno)
{
    private FabricaMarrow Fabrica => entorno.Fabrica;

    [Fact]
    public async Task IdentidadReal_CorrespondeAlRolDePruebas()
    {
        var identidad = await LeerIdentidadRealAsync();

        Assert.Equal("marrow_test", identidad.BaseActual);
        Assert.Equal("marrow_test", identidad.SessionUser);
        Assert.Equal("marrow_test", identidad.CurrentUser);
        Assert.Contains(identidad.DireccionServidor, new[] { "127.0.0.1", "::1" });
        Assert.Equal(5432, identidad.PuertoServidor);
        Assert.Equal("marrow_test", identidad.PropietarioBase);
        Assert.False(identidad.EsSuperusuario);
        Assert.False(identidad.PuedeCrearBases);
        Assert.False(identidad.PuedeCrearRoles);
        Assert.False(identidad.PuedeReplicar);
        Assert.False(identidad.OmiteRls);
        Assert.Equal(0, identidad.Membresias);
        Assert.Equal(2, identidad.BasesDeDesarrolloEncontradas);
        Assert.Equal(0, identidad.BasesDeDesarrolloConConnect);
        VerificacionIdentidadPostgres.Verificar(identidad);
    }

    [Fact]
    public async Task IdentidadReal_SiSeAltera_EsRechazada()
    {
        var real = await LeerIdentidadRealAsync();

        Assert.Equal(
            [MotivoRechazoConexion.BaseRealIncorrecta],
            VerificacionIdentidadPostgres.ValidarIdentidad(real with { BaseActual = "marrow" }));
        Assert.Equal(
            [MotivoRechazoConexion.BaseRealIncorrecta],
            VerificacionIdentidadPostgres.ValidarIdentidad(real with { BaseActual = "Marrow" }));
        Assert.Equal(
            [MotivoRechazoConexion.SessionUserIncorrecto],
            VerificacionIdentidadPostgres.ValidarIdentidad(real with { SessionUser = "postgres" }));
        Assert.Equal(
            [MotivoRechazoConexion.PrivilegiosAdministrativos],
            VerificacionIdentidadPostgres.ValidarIdentidad(real with { EsSuperusuario = true }));
        Assert.Equal(
            [MotivoRechazoConexion.ConnectEnBasesDeDesarrollo],
            VerificacionIdentidadPostgres.ValidarIdentidad(real with { BasesDeDesarrolloConConnect = 1 }));
    }

    [Fact]
    public void Api_SeEjecutaEnElEntornoDePruebas()
    {
        var entornoDeLaApi = Fabrica.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal("Testing", entornoDeLaApi.EnvironmentName);
    }

    [Fact]
    public async Task Api_TieneUnaSolaConfiguracionDeEfCoreConLaConexionValidada()
    {
        await using var alcance = Fabrica.Services.CreateAsyncScope();
        var proveedor = alcance.ServiceProvider;
        var context = proveedor.GetRequiredService<MarrowDbContext>();

        Assert.Single(proveedor.GetServices<IDbContextOptionsConfiguration<MarrowDbContext>>());
        Assert.Single(proveedor.GetServices<DbContextOptions<MarrowDbContext>>());
        Assert.Equal(FabricaMarrow.ProveedorEsperado, context.Database.ProviderName);
        Assert.True(Fabrica.UsaLaConexionValidada(context));

        var conexion = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());
        Assert.Equal("localhost", conexion.Host);
        Assert.Equal(5432, conexion.Port);
        Assert.Equal("marrow_test", conexion.Database);
        Assert.Equal("marrow_test", conexion.Username);
    }

    [Fact]
    public async Task Api_SuContextoSeConectaAMarrowTestSegunElServidor()
    {
        await using var alcance = Fabrica.Services.CreateAsyncScope();
        var context = alcance.ServiceProvider.GetRequiredService<MarrowDbContext>();

        await context.Database.OpenConnectionAsync();
        try
        {
            var identidad = await VerificacionIdentidadPostgres.LeerIdentidadAsync(
                context.Database.GetDbConnection());

            Assert.Equal("marrow_test", identidad.BaseActual);
            Assert.Equal("marrow_test", identidad.CurrentUser);
            VerificacionIdentidadPostgres.Verificar(identidad);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Host=host-ficticio.invalid;Port=1;Database=base_ficticia;Username=usuario_ficticio")]
    public void Fabrica_NoReconoceOtraConexion(string? cadena)
    {
        Assert.False(Fabrica.EsLaConexionValidada(cadena));
    }

    [Fact]
    public async Task Variacion_SinCambiosDeConexion_PasaLasVerificaciones()
    {
        await using var variacion = await Fabrica.CrearVariacionVerificadaAsync(_ => { });
        using var cliente = variacion.CreateClient();

        var respuesta = await cliente.GetAsync("/api/personajes");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public void Api_NoCargaUserSecretsAppsettingsNiVariablesDeEntorno()
    {
        var configuracion = Fabrica.Services.GetRequiredService<IConfiguration>();
        var proveedores = Assert.IsAssignableFrom<IConfigurationRoot>(configuracion).Providers.ToList();

        Assert.DoesNotContain(proveedores, proveedor => proveedor is FileConfigurationProvider);
        Assert.DoesNotContain(proveedores, proveedor => proveedor is EnvironmentVariablesConfigurationProvider);
        Assert.Null(configuracion.GetConnectionString("MarrowDatabase"));
        Assert.Null(configuracion["AllowedHosts"]);
        Assert.Equal("Marrow.IntegrationTests", configuracion["Jwt:Issuer"]);
    }

    // marrow tiene la tabla Enemigos con datos y marrow_test no tiene tablas: un 42P01 prueba
    // que el pedido llegó a marrow_test. Cambiará cuando la siguiente subetapa aplique el esquema.
    [Fact]
    public async Task Api_UnPedidoHttpRealConsultaMarrowTestYNoLaBaseDeUserSecrets()
    {
        using var cliente = Fabrica.CreateClient();

        var error = await Assert.ThrowsAnyAsync<PostgresException>(() => cliente.GetAsync("/api/enemigos"));

        Assert.Equal("42P01", error.SqlState);
    }

    [Theory]
    [InlineData("/api/personajes")]
    [InlineData("/api/partidas")]
    public async Task Api_SinToken_Devuelve401(string ruta)
    {
        using var cliente = Fabrica.CreateClient();

        var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Api_ConTokenInvalido_Devuelve401()
    {
        using var cliente = Fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token.no.valido");

        var respuesta = await cliente.GetAsync("/api/personajes");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static Task<IdentidadPostgres> LeerIdentidadRealAsync() =>
        FabricaMarrow.LeerIdentidadSoloLecturaAsync(ProteccionBaseDePruebas.ObtenerConexionDelEntorno());
}
