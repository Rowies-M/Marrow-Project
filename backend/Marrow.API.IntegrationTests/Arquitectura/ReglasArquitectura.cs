using System.Text.RegularExpressions;

namespace Marrow.API.IntegrationTests.Arquitectura;

internal sealed record ReglaArquitectura(string Nombre, Regex Patron, bool SobreTexto, HashSet<string> Permitidos);

/// <summary>
/// Reglas sobre el IL compilado, no sobre el texto: los alias y los nombres calificados ya están resueltos y
/// las concatenaciones de constantes ya están plegadas. Cada permiso es la identidad completa de un método:
/// ensamblado, namespace, tipo, método y firma. No hay permisos por tipo ni exclusiones de archivos.
///
/// Límites conocidos: analiza solo los ensamblados de este repositorio que se le indican. No ve textos armados
/// en tiempo de ejecución, literales u8 ni argumentos genéricos, y solo revisa atributos para detectar
/// ejecución automática. Es una defensa contra errores, no una barrera contra código escrito para evadirla.
/// La barrera real sigue siendo que marrow_test no tiene permisos sobre las demás bases.
/// </summary>
internal static class ReglasArquitectura
{
    private const string Infraestructura = "Marrow.API.IntegrationTests.Infraestructura!Marrow.API.IntegrationTests.Infraestructura.";
    private const string Aislada = "Marrow.API.IntegrationTests!Marrow.API.IntegrationTests.";
    private const string Reales = "Marrow.API.PostgresTests!Marrow.API.PostgresTests.";

    private const string Fabrica = Infraestructura + "FabricaMarrow.";
    private const string Bloqueo = Infraestructura + "BloqueoEjecucionPruebas.";
    private const string Identidad = Infraestructura + "VerificacionIdentidadPostgres.";
    private const string Proteccion = Infraestructura + "ProteccionBaseDePruebas.";

    private const string CrearVerificada = "CrearVerificadaAsync(Func`2<String,String>,CancellationToken)";
    private const string CrearVerificadaCore =
        "CrearVerificadaCoreAsync(Func`2<String,String>,Func`3<String,CancellationToken,Task`1<IdentidadPostgres>>,"
        + "Func`3<String,CancellationToken,Task`1<FabricaMarrow>>,CancellationToken)";
    private const string CrearVariacion = "CrearVariacionVerificadaAsync(Action`1<IServiceCollection>,CancellationToken)";
    private const string LeerIdentidadReal = "LeerIdentidadSoloLecturaAsync(String,CancellationToken)";
    private const string AdquirirReal = "AdquirirAsync(String,CancellationToken)";

    private const string Inspector = Aislada + "Arquitectura.InspectorIl.";
    private const string TablaDeReglas = Aislada + "Arquitectura.ReglasArquitectura.";
    private const string PruebasDeArquitectura = Aislada + "ArquitecturaTests.";
    private const string PruebasDeFabricaSinBaseTipo = Aislada + "ArquitecturaFabricaTests";
    private const string PruebasDeFabricaSinBase = PruebasDeFabricaSinBaseTipo + ".";
    private const string PruebasDeFabrica = Aislada + "FabricaMarrowTests.";
    private const string PruebasDeBloqueo = Aislada + "BloqueoEjecucionPruebasTests.";
    private const string PruebasDeProteccion = Aislada + "ProteccionBaseDePruebasTests.";

    private const string PruebasReales = Reales + "ConexionRealTests.";
    private const string Entorno = Reales + "EntornoIntegracion.";

    /// <summary>Ningún otro tipo analizado puede usar el nombre corto de estos.</summary>
    public static readonly string[] TiposAutorizados =
    [
        Infraestructura + "FabricaMarrow",
        Infraestructura + "FabricaVariada",
        Infraestructura + "BloqueoEjecucionPruebas",
        Infraestructura + "VerificacionIdentidadPostgres",
        Infraestructura + "ProteccionBaseDePruebas",
        Reales + "EntornoIntegracion"
    ];

    public static readonly ReglaArquitectura[] Reglas =
    [
        Miembro(
            "Fabrica alternativa",
            @"WebApplicationFactory`1::(\.ctor|get_Server|ConfigureWebHost|CreateHost\w*|CreateServer|CreateWebHost\w*|UseKestrel|StartServer)$"
            + @"|^base::.*WebApplicationFactory`1$"
            + @"|TestServer::|TestHost\.WebHostBuilderExtensions::UseTestServer"
            + @"|Hosting\.WebHostBuilder::|Hosting\.(Host|HostBuilder|HostApplicationBuilder)::"
            + @"|Builder\.(WebApplication|WebApplicationBuilder)::|AspNetCore\.WebHost::"
            + @"|HostFactoryResolver|KestrelServer|^Program::",
            Fabrica + "<tipo>",
            Fabrica + ".ctor(String)"),
        Miembro(
            "Variacion sin verificar",
            @"::WithWebHostBuilder$|::get_Factories$",
            Fabrica + CrearVariacion,
            PruebasDeFabricaSinBase + "WithWebHostBuilder_EstaBloqueado()",
            PruebasDeFabricaSinBase + "Factories_EstaBloqueado()"),
        Miembro(
            "Puntos de sustitucion para pruebas",
            @"::(CrearVerificadaCoreAsync|CrearSinBaseParaPruebas|LeerIdentidadConFabricaAsync|AdquirirConFabricaDeConexionAsync|ReemplazarRegistroEfCore)$|FabricaVariada::\.ctor$",
            Bloqueo + AdquirirReal,
            Fabrica + CrearVerificada,
            Fabrica + LeerIdentidadReal,
            Fabrica + CrearVariacion,
            Fabrica + "ConfigureWebHost(IWebHostBuilder)",
            PruebasDeBloqueo + "Adquirir(ServidorFalso,CancellationToken)",
            PruebasDeBloqueo + "Adquirir_ConCadenaIncorrecta_SeRechazaSinCrearConexion(String,MotivoRechazoConexion)",
            PruebasDeFabrica + "Crear_IdentidadRealIncorrecta_SeRechazaSinConstruirLaFabrica()",
            PruebasDeFabrica + "Crear_TodoValido_LlegaAConstruirConLaMismaCadena()",
            PruebasDeFabrica + "LeerIdentidad_ConCadenaInvalida_SeRechazaSinCrearConexion(String,MotivoRechazoConexion)",
            PruebasDeFabrica + "VerificarRechazoSinAvanzar(String,MotivoRechazoConexion)",
            PruebasDeFabricaSinBase + "Api_TieneUnSoloRegistroDeEfCoreConLaCadenaDeLaFabrica()",
            PruebasDeFabricaSinBase + "Api_UsaElEntornoDePruebasYSoloConfiguracionEnMemoria()",
            PruebasDeFabricaSinBase + "Factories_EstaBloqueado()",
            PruebasDeFabricaSinBase + "WithWebHostBuilder_EstaBloqueado()",
            PruebasDeFabricaSinBase + "Variacion_QueAgregaConfiguracionDeEfCore_SeRechazaAntesDeConectar()",
            PruebasDeFabricaSinBase + "Variacion_QueAgregaOtroContexto_SeRechazaAntesDeConectar()",
            PruebasDeFabricaSinBase + "Variacion_QueCambiaElContextoPorUnaFactoria_SeRechazaAntesDeConectar()",
            PruebasDeFabricaSinBase + "RegistroEfCore_ReemplazaElDeProgramYQuedaUnico()"),
        Miembro(
            "Camino hacia PostgreSQL real",
            @"FabricaMarrow::(CrearVerificadaAsync|LeerIdentidadSoloLecturaAsync)$|ProteccionBaseDePruebas::ObtenerConexionDelEntorno$",
            Fabrica + CrearVerificada,
            Fabrica + CrearVerificadaCore,
            Entorno + "InitializeAsync()",
            PruebasReales + "LeerIdentidadRealAsync()"),

        // Todavía nadie debe adquirir el bloqueo real: se autorizará al integrarlo con EntornoIntegracion.
        Miembro("Bloqueo real sin integrar", @"BloqueoEjecucionPruebas::AdquirirAsync$"),
        Miembro(
            "Contexto creado directamente",
            @"MarrowDbContext::\.ctor$|^base::.*DbContext$",
            PruebasDeFabricaSinBaseTipo + "+ContextoAdicional.<tipo>"),
        Miembro(
            "Registro alternativo de EF Core",
            @"::(AddDbContext\w*|AddPooledDbContextFactory|ConfigureDbContext|AddEntityFramework\w*"
            + @"|Use(Npgsql|SqlServer|Sqlite|InMemoryDatabase|Cosmos|Oracle|MySql|MySQL|Firebird))$"
            + @"|DbContextOptionsBuilder|ServiceCollectionDescriptorExtensions::(RemoveAll|Replace)$",
            Fabrica + "ReemplazarRegistroEfCore(IServiceCollection,String)",
            PruebasDeFabricaSinBase + "RegistroEfCore_ReemplazaElDeProgramYQuedaUnico()",
            PruebasDeFabricaSinBase + "Variacion_QueAgregaConfiguracionDeEfCore_SeRechazaAntesDeConectar()",
            PruebasDeFabricaSinBase + "Variacion_QueAgregaOtroContexto_SeRechazaAntesDeConectar()"),
        Miembro("Interceptor arbitrario", @"::AddInterceptors$|^base::.*Interceptor$"),
        Miembro(
            "Conexion directa a PostgreSQL",
            @"Npgsql\.NpgsqlConnection::\.ctor$",
            Bloqueo + AdquirirReal,
            Fabrica + LeerIdentidadReal),
        Miembro(
            "Otra forma de crear conexiones",
            @"Npgsql\.Npgsql\w*DataSource\w*::|Npgsql\.NpgsqlFactory::|DbProviderFactor\w*::|::CloneWith$"
            + @"|Npgsql\.NpgsqlCommand::|Npgsql\.NpgsqlBatch\w*::"),
        Miembro(
            "Red o servicios locales",
            @"System\.Net\.Http\.(HttpClient|HttpClientHandler|SocketsHttpHandler|WinHttpHandler|HttpMessageInvoker)::\.ctor$"
            + @"|System\.Net\.Sockets\.\w+::|System\.Net\.(WebClient|WebRequest|HttpWebRequest|HttpListener)::"
            + @"|WebSockets\.ClientWebSocket::|IHttpClientFactory::|Diagnostics\.Process(StartInfo)?::"),
        Miembro("Cambio de conexion", @"::(SetConnectionString|SetDbConnection|ChangeDatabase)$"),
        Miembro(
            "Acceso a la conexion de la API",
            @"::(GetDbConnection|OpenConnection\w*)$",
            Fabrica + "VerificarConexionDeLaApiAsync(IServiceProvider,CancellationToken)",
            PruebasReales + "Api_SuContextoSeConectaAMarrowTestSegunElServidor()"),
        Miembro(
            "SQL o escritura directa",
            @"::(ExecuteSql\w*|SqlQuery\w*|FromSql\w*|ExecuteDelete\w*|ExecuteUpdate\w*|SaveChanges\w*|BeginTransaction\w*|BeginBinaryImport)$"),
        Miembro(
            "Comando SQL",
            @"::CreateCommand$",
            Bloqueo + "AdquirirConFabricaDeConexionAsync(String,Func`2<String,DbConnection>,CancellationToken)",
            Bloqueo + "VerificarRetenidoAsync(CancellationToken)",
            Bloqueo + "DisposeAsync()",
            Identidad + "LeerIdentidadAsync(DbConnection,CancellationToken)"),
        Miembro(
            "Creacion o destruccion de esquema",
            @"::(EnsureCreated\w*|EnsureDeleted\w*|Migrate\w*|GenerateCreateScript)$|IMigrator::|IDatabaseCreator::|IRelationalDatabaseCreator::"),
        Miembro(
            "Reflexion o construccion indirecta",
            @"System\.Type::(GetConstructor\w*|GetMethod\w*|GetProperty|GetProperties|GetField\w*|GetMember\w*|InvokeMember)$"
            + @"|Reflection\.(MethodBase|MethodInfo|ConstructorInfo|FieldInfo|PropertyInfo|Assembly)::"
            + @"|System\.Activator::|ActivatorUtilities::|RuntimeHelpers::GetUninitializedObject$"
            + @"|Expression::New$|DynamicMethod::|RuntimeBinder|CallSite",
            Inspector + ".cctor()",
            PruebasDeArquitectura + ".cctor()"),
        Miembro(
            "Configuracion o archivos externos",
            @"UserSecretsConfigurationExtensions::|::(AddUserSecrets|AddJsonFile|AddEnvironmentVariables|AddCommandLine|AddIniFile|AddXmlFile|AddKeyPerFile)$"
            + @"|Configuration\.ConfigurationBuilder::|System\.IO\.(File|Directory|FileStream|StreamReader)::"
            + @"|System\.Environment::(GetFolderPath|SetEnvironmentVariable)$",
            Inspector + "Analizar(String)",
            PruebasDeArquitectura + "RutaEnAnalisis(String)",
            PruebasDeArquitectura + "LeerProyecto(String)",
            PruebasDeArquitectura + "Solucion_NoIncluyeElProyectoDePruebasReales()"),
        Miembro(
            "Lectura de variables de entorno",
            @"System\.Environment::GetEnvironmentVariable$",
            Proteccion + "LeerVariableDelEntorno(String)"),
        Miembro("Ejecucion automatica", @"ModuleInitializerAttribute::\.ctor$|TestFrameworkAttribute::\.ctor$"),
        Texto(
            "SQL destructivo en texto",
            @"\b(delete\s+from|truncate\b|drop\s+(table|database|schema|role|user|owned|index|sequence|view|extension)"
            + @"|alter\s+(table|database|role|user|system|schema|default)|create\s+(table|database|role|user|schema|extension)"
            + @"|insert\s+into|update\s+\S+\s+set|grant\s|revoke\s|copy\s+\S+\s+from|reassign\s+owned"
            + @"|set\s+role|set\s+session\s+authorization)",
            TablaDeReglas + ".cctor()"),
        Texto(
            "Configuracion de desarrollo en texto",
            @"secrets\.json|usersecrets|appsettings|aspnetcore_environment|dotnet_environment|connectionstrings",
            TablaDeReglas + ".cctor()",
            PruebasDeArquitectura + "ArchivoDeProyecto_NoIncorporaCodigoNiConfiguracionExterna(String)",
            PruebasDeProteccion + "ObtenerConexionValidada_SoloConsultaLaVariableDePruebas()")
    ];

    public static List<string> BuscarViolaciones(IEnumerable<UsoDeCodigo> usos, IEnumerable<ReglaArquitectura>? reglas = null)
    {
        var aplicables = (reglas ?? Reglas).ToList();
        return usos
            .SelectMany(uso => aplicables
                .Where(regla => regla.SobreTexto == uso.EsTexto
                    && regla.Patron.IsMatch(uso.Referencia)
                    && !regla.Permitidos.Contains(uso.Origen))
                .Select(regla => $"{regla.Nombre}: {uso.Origen}"))
            .Distinct()
            .ToList();
    }

    /// <summary>Tipos que comparten el nombre corto de un tipo autorizado sin ser exactamente ese tipo.</summary>
    public static List<string> BuscarSuplantadores(IEnumerable<string> tipos)
    {
        var autorizados = TiposAutorizados.ToDictionary(NombreCorto, tipo => tipo);
        return tipos
            .Where(tipo => autorizados.TryGetValue(NombreCorto(tipo), out var autorizado) && autorizado != tipo)
            .ToList();
    }

    public static bool EstaPermitido(string origen) => Reglas.Any(regla => regla.Permitidos.Contains(origen));

    public static ReglaArquitectura ReglaDePrueba(string patron, params string[] permitidos) =>
        new("Regla de prueba", new Regex(patron, RegexOptions.CultureInvariant), false, [.. permitidos]);

    private static string NombreCorto(string tipo) => tipo[(tipo.LastIndexOfAny(['.', '+', '!']) + 1)..];

    private static ReglaArquitectura Miembro(string nombre, string patron, params string[] permitidos) =>
        new(nombre, new Regex(patron, RegexOptions.CultureInvariant), false, [.. permitidos]);

    private static ReglaArquitectura Texto(string nombre, string patron, params string[] permitidos) =>
        new(nombre, new Regex(patron, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), true, [.. permitidos]);
}
