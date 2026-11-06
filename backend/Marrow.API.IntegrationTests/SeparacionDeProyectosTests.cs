using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Marrow.API.IntegrationTests;

// Control simple contra errores accidentales: lee la solución, los proyectos y el texto de los .cs.
// Límites: un alias o un texto armado lo evaden, y un comentario o una cadena pueden dar una falsa alarma.
// Las listas son exactas: un permiso que deje de usarse también hace fallar la prueba.
public class SeparacionDeProyectosTests
{
    private const string Aislada = "Marrow.API.IntegrationTests";
    private const string Infraestructura = "Marrow.API.IntegrationTests.Infraestructura";
    private const string Reales = "Marrow.API.PostgresTests";

    private const string Fabrica = Infraestructura + "/FabricaMarrow.cs";
    private const string Bloqueo = Infraestructura + "/BloqueoEjecucionPruebas.cs";
    private const string Proteccion = Infraestructura + "/ProteccionBaseDePruebas.cs";
    private const string Identidad = Infraestructura + "/VerificacionIdentidadPostgres.cs";
    private const string FabricaSinBase = Aislada + "/ArquitecturaFabricaTests.cs";
    private const string ConexionReal = Reales + "/ConexionRealTests.cs";
    private const string Entorno = Reales + "/EntornoIntegracion.cs";

    private static readonly string Backend = DirectorioBackend();
    private static readonly Dictionary<string, string> Fuentes = LeerFuentes();

    // C# distingue mayúsculas, así que los identificadores se comparan exactos; el SQL no las distingue.
    [Theory]
    [InlineData("WebApplicationFactory", false, Fabrica)]
    [InlineData("new NpgsqlConnection(", false, Fabrica + ";" + Bloqueo)]
    [InlineData("UseNpgsql", false, Fabrica + ";" + FabricaSinBase)]
    [InlineData("CrearVerificadaAsync", false, Fabrica + ";" + Entorno)]
    [InlineData("ObtenerConexionDelEntorno", false, Fabrica + ";" + Proteccion + ";" + ConexionReal)]
    [InlineData("LeerIdentidadSoloLecturaAsync", false, Fabrica + ";" + ConexionReal)]
    [InlineData("Migrate", false, "")]
    [InlineData("EnsureCreated", false, "")]
    [InlineData("EnsureDeleted", false, "")]
    [InlineData("TRUNCATE", true, "")]
    [InlineData("ExecuteSql", false, "")]
    [InlineData("new HttpClient", false, "")]
    [InlineData("Process.Start", false, "")]
    [InlineData("AddUserSecrets", false, "")]
    [InlineData("BloqueoEjecucionPruebas.AdquirirAsync", false, "")]
    [InlineData("new MarrowDbContext", false, "")]
    [InlineData("SaveChanges", false, "")]
    [InlineData("DELETE FROM", true, "")]
    [InlineData("DROP TABLE", true, "")]
    [InlineData("DROP DATABASE", true, "")]
    [InlineData("GetEnvironmentVariable", false, Proteccion)]
    [InlineData("CreateCommand", false, Bloqueo + ";" + Identidad)]
    public void Patron_SoloApareceEnSusArchivosPermitidos(string patron, bool ignorarMayusculas, string permitidos)
    {
        var comparacion = ignorarMayusculas ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        var encontrados = Fuentes
            .Where(fuente => fuente.Value.Contains(patron, comparacion))
            .Select(fuente => fuente.Key)
            .Order(StringComparer.Ordinal);

        var esperados = permitidos.Split(';', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal);

        Assert.Equal(esperados, encontrados);
    }

    [Theory]
    [InlineData(Fabrica)]
    [InlineData(Bloqueo)]
    [InlineData(Proteccion)]
    [InlineData(Identidad)]
    [InlineData(FabricaSinBase)]
    [InlineData(ConexionReal)]
    [InlineData(Entorno)]
    public void Busqueda_LeeLosArchivosEsperados(string archivo)
    {
        Assert.True(Fuentes.TryGetValue(archivo, out var contenido), $"No se leyó {archivo}.");
        Assert.False(string.IsNullOrWhiteSpace(contenido), $"{archivo} está vacío.");
    }

    [Fact]
    public void Busqueda_EncuentraUnPatronConocidoEnSuArchivo()
    {
        Assert.Contains("WebApplicationFactory", Fuentes[Fabrica], StringComparison.Ordinal);
    }

    [Fact]
    public void Solucion_NoIncluyeLasPruebasRealesPeroSiLaSuiteAislada()
    {
        var solucion = LeerArchivo(Path.Combine(Backend, "Marrow.sln"));

        Assert.DoesNotContain(Reales, solucion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{Aislada}.csproj", solucion, StringComparison.Ordinal);
    }

    [Fact]
    public void SuiteAislada_SoloCompilaLasPruebasRealesSinEnlazarse()
    {
        var referencia = LeerProyecto(Aislada).Descendants("ProjectReference")
            .Single(elemento => ((string)elemento.Attribute("Include")!).EndsWith($"{Reales}.csproj", StringComparison.Ordinal));

        Assert.Equal("false", (string?)referencia.Attribute("ReferenceOutputAssembly"));
        Assert.Equal("all", (string?)referencia.Attribute("ExcludeAssets"));
    }

    [Fact]
    public void InternalsVisibleTo_SoloPermiteAccesoALaSuiteAislada()
    {
        Assert.Equal([Aislada], VisibleInternamentePara(Infraestructura));
        Assert.Empty(VisibleInternamentePara(Aislada));
        Assert.Empty(VisibleInternamentePara(Reales));
        Assert.DoesNotContain(Fuentes.Values, fuente => fuente.Contains("assembly: InternalsVisibleTo", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(Aislada)]
    [InlineData(Infraestructura)]
    [InlineData(Reales)]
    public void ArchivoDeProyecto_NoIncorporaCodigoNiConfiguracionExterna(string proyecto)
    {
        var nombres = LeerProyecto(proyecto).Descendants().Select(elemento => elemento.Name.LocalName).ToList();

        Assert.DoesNotContain("UserSecretsId", nombres);
        Assert.DoesNotContain("Compile", nombres);
        Assert.DoesNotContain("Reference", nombres);
        Assert.DoesNotContain("Import", nombres);
    }

    [Theory]
    [InlineData(
        Aislada,
        "Microsoft.AspNetCore.Mvc.Testing,Microsoft.EntityFrameworkCore.Relational,Microsoft.NET.Test.Sdk,xunit,xunit.runner.visualstudio")]
    [InlineData(Infraestructura, "Microsoft.AspNetCore.Mvc.Testing,Microsoft.EntityFrameworkCore.Relational")]
    [InlineData(
        Reales,
        "Microsoft.AspNetCore.Mvc.Testing,Microsoft.EntityFrameworkCore.Relational,Microsoft.NET.Test.Sdk,xunit,xunit.runner.visualstudio")]
    public void ArchivoDeProyecto_SoloTieneLosPaquetesEsperados(string proyecto, string esperados)
    {
        var paquetes = LeerProyecto(proyecto).Descendants("PackageReference")
            .Select(paquete => (string)paquete.Attribute("Include")!)
            .Order(StringComparer.Ordinal);

        Assert.Equal(esperados, string.Join(",", paquetes));
    }

    private static IEnumerable<string> VisibleInternamentePara(string proyecto) =>
        LeerProyecto(proyecto).Descendants("InternalsVisibleTo").Select(elemento => (string)elemento.Attribute("Include")!);

    private static XDocument LeerProyecto(string proyecto) =>
        XDocument.Parse(LeerArchivo(Path.Combine(Backend, proyecto, $"{proyecto}.csproj")));

    private static Dictionary<string, string> LeerFuentes()
    {
        var fuentes = new Dictionary<string, string>();
        foreach (var proyecto in new[] { Aislada, Infraestructura, Reales })
        {
            foreach (var ruta in ListarFuentes(Path.Combine(Backend, proyecto)))
            {
                var relativa = Path.GetRelativePath(Backend, ruta).Replace('\\', '/');
                if (relativa != $"{Aislada}/{nameof(SeparacionDeProyectosTests)}.cs")
                {
                    fuentes[relativa] = LeerArchivo(ruta);
                }
            }
        }

        return fuentes;
    }

    private static List<string> ListarFuentes(string directorio)
    {
        var separadores = new[] { '/', '\\' };
        return Directory.EnumerateFiles(directorio, "*.cs", SearchOption.AllDirectories)
            .Where(ruta => !Path.GetRelativePath(directorio, ruta).Split(separadores).Any(parte => parte is "bin" or "obj"))
            .ToList();
    }

    private static string LeerArchivo(string ruta)
    {
        Assert.True(File.Exists(ruta), $"No se encontró {ruta}: la prueba no puede garantizar nada.");
        return File.ReadAllText(ruta);
    }

    private static string DirectorioBackend([CallerFilePath] string esteArchivo = "") =>
        Path.GetDirectoryName(Path.GetDirectoryName(esteArchivo))!;
}
