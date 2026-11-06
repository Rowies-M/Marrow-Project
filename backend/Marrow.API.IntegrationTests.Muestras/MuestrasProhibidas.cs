using System.Data.Common;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using Marrow.API.Data;
using Marrow.API.IntegrationTests.Infraestructura;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ContextoConAlias = Marrow.API.Data.MarrowDbContext;

namespace Marrow.API.IntegrationTests.Muestras;

/// <summary>Cada miembro corresponde a una regla: ArquitecturaMuestrasTests exige la lista completa.</summary>
public static class MuestrasProhibidas
{
    private const string Ficticia = "Host=host-ficticio.invalid;Port=1;Database=base_ficticia;Username=x;Password=x";

    public static object FabricaDirecta() => new WebApplicationFactory<Program>();

    public static object ServidorDirecto() => new TestServer(new WebHostBuilder());

    public static object AplicacionDirecta() => WebApplication.CreateBuilder();

    public static object ContextoDirecto(DbContextOptions<MarrowDbContext> opciones) => new MarrowDbContext(opciones);

    public static object ContextoCalificado(DbContextOptions<MarrowDbContext> opciones) =>
        new global::Marrow.API.Data.MarrowDbContext(opciones);

    public static object ContextoPorAlias(DbContextOptions<MarrowDbContext> opciones) => new ContextoConAlias(opciones);

    public static object Reflexion(object[] argumentos) =>
        typeof(MarrowDbContext).GetConstructors()[0].Invoke(argumentos);

    public static object? ConActivator(object opciones) => Activator.CreateInstance(typeof(MarrowDbContext), opciones);

    public static object ConActivatorUtilities(IServiceProvider servicios) =>
        ActivatorUtilities.CreateInstance<MarrowDbContext>(servicios);

    public static object? Dinamico(dynamic valor) => valor.Database;

    public static void RegistroDirecto(IServiceCollection servicios) => servicios.AddDbContext<MarrowDbContext>();

    public static void ProveedorDirecto(DbContextOptionsBuilder opciones) => opciones.UseNpgsql(Ficticia);

    public static void ConfiguracionAdicional(IServiceCollection servicios) =>
        servicios.ConfigureDbContext<MarrowDbContext>(_ => { });

    public static void RegistroReemplazado(IServiceCollection servicios, ServiceDescriptor descriptor) =>
        servicios.Replace(descriptor);

    public static void InterceptorArbitrario(DbContextOptionsBuilder opciones) => opciones.AddInterceptors();

    public static object ConexionDirecta() => new NpgsqlConnection(Ficticia);

    public static object ConstructorDeDataSource() => new NpgsqlDataSourceBuilder(Ficticia);

    public static object? FabricaDeProveedor() => NpgsqlFactory.Instance.CreateConnection();

    public static void CambioDeConexion(MarrowDbContext context) => context.Database.SetConnectionString(Ficticia);

    public static void CambioDeBase(DbConnection conexion) => conexion.ChangeDatabase("base_ficticia");

    public static object ComandoDirecto(DbConnection conexion) => conexion.CreateCommand();

    public static Task<int> SqlCrudo(MarrowDbContext context) => context.Database.ExecuteSqlRawAsync("SELECT 1");

    public static Task<int> BorradoMasivo(MarrowDbContext context) => context.Usuarios.ExecuteDeleteAsync();

    public static Task Migracion(MarrowDbContext context) => context.Database.MigrateAsync();

    public static Task<bool> BorrarBase(MarrowDbContext context) => context.Database.EnsureDeletedAsync();

    public static string TextoDelete() => "delete from x";

    public static string TextoDrop() => "DROP TABLE x";

    public static string TextoTruncateEnMinusculas() => "truncate x";

    public static string TextoConcatenado() => "TRUN" + "CATE x";

    public static string TextoSecrets() => "secrets.json";

    public static object SecretosDeUsuario() => new ConfigurationBuilder().AddUserSecrets<Program>();

    public static string ArchivoExterno() => File.ReadAllText("archivo-ficticio");

    public static void CambioDeEntorno() => Environment.SetEnvironmentVariable("X", "Y");

    public static object VariacionHeredada(WebApplicationFactory<Program> fabrica) => fabrica.WithWebHostBuilder(_ => { });

    public static object FabricasHeredadas(WebApplicationFactory<Program> fabrica) => fabrica.Factories;

    public static object BloqueoReal() => BloqueoEjecucionPruebas.AdquirirAsync(Ficticia);

    public static object CaminoReal() => ProteccionBaseDePruebas.ObtenerConexionDelEntorno();

    public static object ClienteHttpDirecto() => new HttpClient();

    public static object ManejadorDeSockets() => new SocketsHttpHandler();

    public static object ManejadorHttp() => new HttpClientHandler();

    public static object ClienteTcp() => new TcpClient();

    public static object SocketDirecto() =>
        new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public static object? ProcesoExterno() => Process.Start("programa-ficticio");

    public sealed class FabricaDerivada : WebApplicationFactory<Program>;

    public sealed class ContextoDerivado(DbContextOptions<MarrowDbContext> opciones) : MarrowDbContext(opciones);

    public sealed class OtroContexto : DbContext;
}
