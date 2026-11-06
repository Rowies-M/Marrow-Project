using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Marrow.API.IntegrationTests.Arquitectura;
using Marrow.API.IntegrationTests.Infraestructura;

namespace Marrow.API.IntegrationTests;

// Analiza los DLL como bytes y los archivos de proyecto: no ejecuta su código ni abre conexiones.
public class ArquitecturaTests
{
    private const string NombreAislada = "Marrow.API.IntegrationTests";
    private const string NombreInfraestructura = "Marrow.API.IntegrationTests.Infraestructura";
    private const string NombreReales = "Marrow.API.PostgresTests";
    private const string NombreMuestras = "Marrow.API.IntegrationTests.Muestras";

    private static readonly AnalisisDeEnsamblado Aislada =
        InspectorIl.Analizar(typeof(ArquitecturaTests).Assembly.Location);

    private static readonly AnalisisDeEnsamblado Infraestructura =
        InspectorIl.Analizar(typeof(FabricaMarrow).Assembly.Location);

    private static readonly AnalisisDeEnsamblado Reales = InspectorIl.Analizar(RutaEnAnalisis(NombreReales));

    private static IEnumerable<AnalisisDeEnsamblado> Principales => [Aislada, Infraestructura, Reales];

    public static string RutaEnAnalisis(string ensamblado)
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "AnalisisIl", $"{ensamblado}.dll");
        Assert.True(File.Exists(ruta), $"Falta {ensamblado}.dll: la prueba no puede garantizar nada.");
        return ruta;
    }

    [Fact]
    public void Inspector_EncuentraLosTresEnsambladosPrincipales()
    {
        Assert.Equal(NombreAislada, Aislada.Ensamblado);
        Assert.Equal(NombreInfraestructura, Infraestructura.Ensamblado);
        Assert.Equal(NombreReales, Reales.Ensamblado);
        Assert.All(Principales, analisis => Assert.NotEmpty(analisis.Usos));
    }

    [Fact]
    public void Principales_NoTienenPatronesFueraDeLosPermisos()
    {
        var violaciones = ReglasArquitectura.BuscarViolaciones(Principales.SelectMany(analisis => analisis.Usos));

        Assert.True(violaciones.Count == 0, string.Join(Environment.NewLine, violaciones));
    }

    [Fact]
    public void Principales_NoTienenTiposQueSuplantenALosAutorizados()
    {
        Assert.Empty(ReglasArquitectura.BuscarSuplantadores(Principales.SelectMany(analisis => analisis.Tipos)));
    }

    [Fact]
    public void TiposAutorizados_ExistenConSuIdentidadExacta()
    {
        var tipos = Principales.SelectMany(analisis => analisis.Tipos).ToHashSet();

        Assert.All(ReglasArquitectura.TiposAutorizados, tipo => Assert.Contains(tipo, tipos));
    }

    [Fact]
    public void SuiteAislada_NoSeEnlazaConLasPruebasRealesNiConLasMuestras()
    {
        Assert.DoesNotContain(NombreReales, Aislada.ReferenciasDeEnsamblado);
        Assert.DoesNotContain(NombreMuestras, Aislada.ReferenciasDeEnsamblado);
        Assert.DoesNotContain(NombreMuestras, Infraestructura.ReferenciasDeEnsamblado);
        Assert.DoesNotContain(NombreMuestras, Reales.ReferenciasDeEnsamblado);
        Assert.DoesNotContain(NombreReales, Infraestructura.ReferenciasDeEnsamblado);
    }

    [Fact]
    public void SuiteAislada_NoDefineNiUsaElFixtureReal()
    {
        Assert.DoesNotContain(Aislada.Tipos, tipo => tipo.EndsWith("EntornoIntegracion", StringComparison.Ordinal));
        Assert.DoesNotContain(Aislada.Tipos, tipo => tipo.EndsWith("ConexionRealTests", StringComparison.Ordinal));

        var referencias = Aislada.Usos.Where(uso => !uso.EsTexto).Select(uso => uso.Referencia).ToList();
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Xunit.CollectionAttribute::", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Xunit.CollectionDefinitionAttribute::", StringComparison.Ordinal));
    }

    [Fact]
    public void PuntosDeSustitucion_SoloSonVisiblesParaLaSuiteAislada()
    {
        Assert.Equal([NombreAislada], Infraestructura.VisibleInternamentePara);
        Assert.Empty(Aislada.VisibleInternamentePara);
        Assert.Empty(Reales.VisibleInternamentePara);
    }

    [Fact]
    public void Solucion_NoIncluyeElProyectoDePruebasReales()
    {
        var solucion = File.ReadAllText(Path.Combine(DirectorioBackend(), "Marrow.sln"));

        Assert.DoesNotContain("PostgresTests", solucion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{NombreAislada}.csproj", solucion, StringComparison.Ordinal);
        Assert.Contains($"{NombreInfraestructura}.csproj", solucion, StringComparison.Ordinal);
        Assert.Contains($"{NombreMuestras}.csproj", solucion, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NombreAislada)]
    [InlineData(NombreInfraestructura)]
    [InlineData(NombreReales)]
    [InlineData(NombreMuestras)]
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
        NombreAislada,
        "Microsoft.AspNetCore.Mvc.Testing,Microsoft.EntityFrameworkCore.Relational,Microsoft.NET.Test.Sdk,xunit,xunit.runner.visualstudio")]
    [InlineData(NombreInfraestructura, "Microsoft.AspNetCore.Mvc.Testing,Microsoft.EntityFrameworkCore.Relational")]
    [InlineData(
        NombreReales,
        "Microsoft.AspNetCore.Mvc.Testing,Microsoft.EntityFrameworkCore.Relational,Microsoft.NET.Test.Sdk,xunit,xunit.runner.visualstudio")]
    [InlineData(NombreMuestras, "")]
    public void ArchivoDeProyecto_SoloTieneLosPaquetesEsperados(string proyecto, string esperados)
    {
        var paquetes = LeerProyecto(proyecto).Descendants("PackageReference")
            .Select(paquete => (string)paquete.Attribute("Include")!)
            .Order(StringComparer.Ordinal);

        Assert.Equal(esperados, string.Join(",", paquetes));
    }

    [Theory]
    [InlineData(NombreMuestras)]
    [InlineData(NombreReales)]
    public void SuiteAislada_SoloCompilaLosProyectosQueAnaliza(string referencia)
    {
        var proyecto = LeerProyecto(NombreAislada).Descendants("ProjectReference")
            .Single(elemento => ((string)elemento.Attribute("Include")!).Contains($"{referencia}.csproj", StringComparison.Ordinal));

        Assert.Equal("false", (string?)proyecto.Attribute("ReferenceOutputAssembly"));
        Assert.Equal("all", (string?)proyecto.Attribute("ExcludeAssets"));
    }

    [Fact]
    public void Muestras_SonUnaBibliotecaSinPruebasNiPuntoDeEntrada()
    {
        var proyecto = LeerProyecto(NombreMuestras);

        Assert.Equal("Library", proyecto.Descendants("OutputType").Select(elemento => elemento.Value).Single());
    }

    private static XDocument LeerProyecto(string proyecto)
    {
        var ruta = Path.Combine(DirectorioBackend(), proyecto, $"{proyecto}.csproj");
        Assert.True(File.Exists(ruta), $"No se encontró {proyecto}.csproj: la prueba no puede garantizar nada.");
        return XDocument.Load(ruta);
    }

    private static string DirectorioBackend([CallerFilePath] string esteArchivo = "") =>
        Path.GetDirectoryName(Path.GetDirectoryName(esteArchivo))!;
}
