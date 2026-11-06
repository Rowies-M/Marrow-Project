using Marrow.API.IntegrationTests.Arquitectura;

namespace Marrow.API.IntegrationTests;

// Las muestras se leen como bytes desde otro ensamblado. Esta suite no se enlaza con él: no puede invocarlas.
public class ArquitecturaMuestrasTests
{
    private const string Ensamblado = "Marrow.API.IntegrationTests.Muestras";
    private const string Tipo = $"{Ensamblado}!{Ensamblado}.MuestrasProhibidas";

    private static readonly AnalisisDeEnsamblado Muestras =
        InspectorIl.Analizar(ArquitecturaTests.RutaEnAnalisis(Ensamblado));

    public static readonly Dictionary<string, string> Esperadas = new()
    {
        ["FabricaDirecta"] = "Fabrica alternativa",
        ["FabricaDerivada"] = "Fabrica alternativa",
        ["ServidorDirecto"] = "Fabrica alternativa",
        ["AplicacionDirecta"] = "Fabrica alternativa",
        ["VariacionHeredada"] = "Variacion sin verificar",
        ["FabricasHeredadas"] = "Variacion sin verificar",
        ["CaminoReal"] = "Camino hacia PostgreSQL real",
        ["BloqueoReal"] = "Bloqueo real sin integrar",
        ["ContextoDirecto"] = "Contexto creado directamente",
        ["ContextoCalificado"] = "Contexto creado directamente",
        ["ContextoPorAlias"] = "Contexto creado directamente",
        ["ContextoDerivado"] = "Contexto creado directamente",
        ["OtroContexto"] = "Contexto creado directamente",
        ["Reflexion"] = "Reflexion o construccion indirecta",
        ["ConActivator"] = "Reflexion o construccion indirecta",
        ["ConActivatorUtilities"] = "Reflexion o construccion indirecta",
        ["Dinamico"] = "Reflexion o construccion indirecta",
        ["RegistroDirecto"] = "Registro alternativo de EF Core",
        ["ProveedorDirecto"] = "Registro alternativo de EF Core",
        ["ConfiguracionAdicional"] = "Registro alternativo de EF Core",
        ["RegistroReemplazado"] = "Registro alternativo de EF Core",
        ["InterceptorArbitrario"] = "Interceptor arbitrario",
        ["ConexionDirecta"] = "Conexion directa a PostgreSQL",
        ["ConstructorDeDataSource"] = "Otra forma de crear conexiones",
        ["FabricaDeProveedor"] = "Otra forma de crear conexiones",
        ["CambioDeConexion"] = "Cambio de conexion",
        ["CambioDeBase"] = "Cambio de conexion",
        ["ComandoDirecto"] = "Comando SQL",
        ["SqlCrudo"] = "SQL o escritura directa",
        ["BorradoMasivo"] = "SQL o escritura directa",
        ["Migracion"] = "Creacion o destruccion de esquema",
        ["BorrarBase"] = "Creacion o destruccion de esquema",
        ["TextoDelete"] = "SQL destructivo en texto",
        ["TextoDrop"] = "SQL destructivo en texto",
        ["TextoTruncateEnMinusculas"] = "SQL destructivo en texto",
        ["TextoConcatenado"] = "SQL destructivo en texto",
        ["TextoSecrets"] = "Configuracion de desarrollo en texto",
        ["SecretosDeUsuario"] = "Configuracion o archivos externos",
        ["ArchivoExterno"] = "Configuracion o archivos externos",
        ["CambioDeEntorno"] = "Configuracion o archivos externos",
        ["ClienteHttpDirecto"] = "Red o servicios locales",
        ["ManejadorDeSockets"] = "Red o servicios locales",
        ["ManejadorHttp"] = "Red o servicios locales",
        ["ClienteTcp"] = "Red o servicios locales",
        ["SocketDirecto"] = "Red o servicios locales",
        ["ProcesoExterno"] = "Red o servicios locales"
    };

    public static TheoryData<string, string> CasosEsperados()
    {
        var datos = new TheoryData<string, string>();
        foreach (var (muestra, regla) in Esperadas)
        {
            datos.Add(muestra, regla);
        }

        return datos;
    }

    [Fact]
    public void Muestras_ExistenTodasYCadaUnaTieneSuInfraccion()
    {
        var metodos = Muestras.MetodosPublicos
            .Where(metodo => metodo.StartsWith($"{Tipo}.", StringComparison.Ordinal))
            .Select(metodo => metodo[(Tipo.Length + 1)..].Split('(')[0]);
        var anidados = Muestras.Tipos
            .Where(tipo => tipo.StartsWith($"{Tipo}+", StringComparison.Ordinal))
            .Select(tipo => tipo[(Tipo.Length + 1)..]);

        Assert.Equal(
            Esperadas.Keys.Order(StringComparer.Ordinal),
            metodos.Concat(anidados).Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(CasosEsperados))]
    public void Muestra_EsDetectadaPorSuRegla(string muestra, string regla)
    {
        var usos = Muestras.Usos
            .Where(uso => uso.Origen.StartsWith($"{Tipo}.{muestra}(", StringComparison.Ordinal)
                || uso.Origen.StartsWith($"{Tipo}+{muestra}.", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(usos);
        Assert.Contains(
            ReglasArquitectura.BuscarViolaciones(usos),
            violacion => violacion.StartsWith($"{regla}: ", StringComparison.Ordinal));
    }

    [Fact]
    public void Muestras_NoTienenEjecucionAutomatica()
    {
        Assert.DoesNotContain(
            Muestras.Usos,
            uso => uso.Referencia.Contains("ModuleInitializerAttribute", StringComparison.Ordinal));
        Assert.False(Muestras.TienePuntoDeEntrada);
    }

    [Fact]
    public void Muestras_NoReferencianElFrameworkDePruebas()
    {
        Assert.DoesNotContain(
            Muestras.ReferenciasDeEnsamblado,
            referencia => referencia.Contains("xunit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Regla_DetectaInicializadorDeModulo()
    {
        var uso = new UsoDeCodigo(
            "Cualquiera!<atributo>",
            "System.Runtime.CompilerServices.ModuleInitializerAttribute::.ctor",
            false);

        Assert.Contains(
            ReglasArquitectura.BuscarViolaciones([uso]),
            violacion => violacion.StartsWith("Ejecucion automatica: ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("X.FabricaMarrow")]
    [InlineData("Marrow.API.IntegrationTests.Infraestructura.FabricaMarrow")]
    public void Suplantador_EsDetectadoYNoHeredaPermisos(string tipoAjeno)
    {
        var identidad = $"{Ensamblado}!{tipoAjeno}";

        Assert.Contains(identidad, ReglasArquitectura.BuscarSuplantadores(Muestras.Tipos));

        var violaciones = ReglasArquitectura.BuscarViolaciones(
            Muestras.Usos.Where(uso => uso.Origen.StartsWith($"{identidad}.", StringComparison.Ordinal)));

        Assert.Contains($"Fabrica alternativa: {identidad}..ctor(String)", violaciones);
        Assert.Contains(violaciones, violacion => violacion.StartsWith("Registro alternativo de EF Core: ", StringComparison.Ordinal));
    }

    [Fact]
    public void MismaIdentidadEnOtroEnsamblado_NoUsaElPermisoDeLaAutorizada()
    {
        const string Autorizada =
            "Marrow.API.IntegrationTests.Infraestructura!Marrow.API.IntegrationTests.Infraestructura.FabricaMarrow..ctor(String)";
        const string Impostora =
            $"{Ensamblado}!Marrow.API.IntegrationTests.Infraestructura.FabricaMarrow..ctor(String)";

        Assert.True(ReglasArquitectura.EstaPermitido(Autorizada));
        Assert.False(ReglasArquitectura.EstaPermitido(Impostora));
    }

    [Fact]
    public void Permiso_DeUnaSobrecarga_NoAlcanzaALaOtra()
    {
        var tipo = $"{Ensamblado}!{Ensamblado}.MuestraDeSobrecargas";
        var regla = ReglasArquitectura.ReglaDePrueba(@"Npgsql\.NpgsqlConnection::\.ctor$", $"{tipo}.Usar(String)");
        var usos = Muestras.Usos.Where(uso => uso.Origen.StartsWith($"{tipo}.Usar(", StringComparison.Ordinal));

        var violaciones = ReglasArquitectura.BuscarViolaciones(usos, [regla]);

        Assert.Equal([$"Regla de prueba: {tipo}.Usar(Int32)"], violaciones);
    }

    [Fact]
    public void CodigoGeneradoDeSobrecargas_QuedaAmbiguoYNoHeredaPermisos()
    {
        var tipo = $"{Ensamblado}!{Ensamblado}.MuestraDeSobrecargas";
        var regla = ReglasArquitectura.ReglaDePrueba(
            @"Npgsql\.NpgsqlConnection::\.ctor$",
            $"{tipo}.UsarAsincrono(String)",
            $"{tipo}.UsarAsincrono(Int32)");
        var usos = Muestras.Usos.Where(uso => uso.Origen.StartsWith($"{tipo}.UsarAsincrono", StringComparison.Ordinal));

        var violaciones = ReglasArquitectura.BuscarViolaciones(usos, [regla]);

        Assert.Equal([$"Regla de prueba: {tipo}.UsarAsincrono{InspectorIl.FirmaAmbigua}"], violaciones);
    }
}
