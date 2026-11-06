using System.Data.Common;
using Npgsql;

namespace Marrow.API.IntegrationTests.Infraestructura;

public enum MotivoRechazoConexion
{
    VariableAusente,
    CadenaVacia,
    FormatoInvalido,
    ParametroNoPermitido,
    HostIncorrecto,
    PuertoIncorrecto,
    BaseIncorrecta,
    UsuarioIncorrecto,
    ContrasenaAusente,
    BaseRealIncorrecta,
    SessionUserIncorrecto,
    CurrentUserIncorrecto,
    DireccionNoLocal,
    PuertoRealIncorrecto,
    PropietarioIncorrecto,
    PrivilegiosAdministrativos,
    MembresiasEnOtrosRoles,
    BasesDeDesarrolloNoEncontradas,
    ConnectEnBasesDeDesarrollo,
    ConexionDeLaApiDistinta,
    RegistroEfCoreAlternativo
}

/// <summary>Informa solo motivos: nunca incluye valores de la conexión ni la excepción original.</summary>
public sealed class ConexionDePruebaRechazadaException(IReadOnlyList<MotivoRechazoConexion> motivos)
    : Exception($"Conexión de prueba rechazada: {string.Join(", ", motivos)}.")
{
    public IReadOnlyList<MotivoRechazoConexion> Motivos { get; } = motivos;
}

/// <summary>Primera capa: valida la cadena de conexión de pruebas antes de abrir cualquier conexión.</summary>
public static class ProteccionBaseDePruebas
{
    public const string NombreVariable = "MARROW_TEST_CONNECTION";
    public const string HostEsperado = "localhost";
    public const int PuertoEsperado = 5432;
    public const string BaseEsperada = "marrow_test";
    public const string UsuarioEsperado = "marrow_test";

    // Solo estos parámetros: si faltaran, Npgsql podría completarlos desde variables como PGPORT.
    private static readonly HashSet<string> ParametrosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Port",
        "Database",
        "Username",
        "Password"
    };

    public static string ObtenerConexionDelEntorno() => ObtenerConexionValidada(LeerVariableDelEntorno);

    public static string ObtenerConexionValidada(Func<string, string?> leerVariable)
    {
        var cadena = leerVariable(NombreVariable);
        if (cadena is null)
        {
            throw new ConexionDePruebaRechazadaException([MotivoRechazoConexion.VariableAusente]);
        }

        ExigirCadenaValida(cadena);
        return cadena;
    }

    public static void ExigirCadenaValida(string? cadena)
    {
        var motivos = ValidarCadena(cadena);
        if (motivos.Count > 0)
        {
            throw new ConexionDePruebaRechazadaException(motivos);
        }
    }

    public static IReadOnlyList<MotivoRechazoConexion> ValidarCadena(string? cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena))
        {
            return [MotivoRechazoConexion.CadenaVacia];
        }

        List<string> parametros;
        NpgsqlConnectionStringBuilder conexion;
        try
        {
            parametros = new DbConnectionStringBuilder { ConnectionString = cadena }.Keys.Cast<string>().ToList();
            conexion = new NpgsqlConnectionStringBuilder(cadena);
        }
        catch (Exception)
        {
            // La excepción original podría citar parte de la cadena: se descarta a propósito.
            return [MotivoRechazoConexion.FormatoInvalido];
        }

        var motivos = new List<MotivoRechazoConexion>();
        if (parametros.Any(parametro => !ParametrosPermitidos.Contains(parametro)))
        {
            motivos.Add(MotivoRechazoConexion.ParametroNoPermitido);
        }

        if (!string.Equals(conexion.Host, HostEsperado, StringComparison.Ordinal))
        {
            motivos.Add(MotivoRechazoConexion.HostIncorrecto);
        }

        var puertoExplicito = parametros.Contains("Port", StringComparer.OrdinalIgnoreCase);
        if (!puertoExplicito || conexion.Port != PuertoEsperado)
        {
            motivos.Add(MotivoRechazoConexion.PuertoIncorrecto);
        }

        if (!string.Equals(conexion.Database, BaseEsperada, StringComparison.Ordinal))
        {
            motivos.Add(MotivoRechazoConexion.BaseIncorrecta);
        }

        if (!string.Equals(conexion.Username, UsuarioEsperado, StringComparison.Ordinal))
        {
            motivos.Add(MotivoRechazoConexion.UsuarioIncorrecto);
        }

        if (string.IsNullOrEmpty(conexion.Password))
        {
            motivos.Add(MotivoRechazoConexion.ContrasenaAusente);
        }

        return motivos;
    }

    // Variable del proceso y, si no está, la del usuario: una terminal ya abierta no ve la recién creada.
    // No consulta User Secrets, appsettings ni ninguna otra fuente.
    private static string? LeerVariableDelEntorno(string nombre)
    {
        var delProceso = Environment.GetEnvironmentVariable(nombre);
        return string.IsNullOrWhiteSpace(delProceso)
            ? Environment.GetEnvironmentVariable(nombre, EnvironmentVariableTarget.User)
            : delProceso;
    }
}
