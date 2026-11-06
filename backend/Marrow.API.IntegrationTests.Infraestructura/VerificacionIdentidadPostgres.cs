using System.Data.Common;

namespace Marrow.API.IntegrationTests.Infraestructura;

/// <summary>Identidad y privilegios informados por PostgreSQL para una conexión abierta.</summary>
public sealed record IdentidadPostgres(
    string BaseActual,
    string SessionUser,
    string CurrentUser,
    string? DireccionServidor,
    int PuertoServidor,
    string PropietarioBase,
    bool EsSuperusuario,
    bool PuedeCrearBases,
    bool PuedeCrearRoles,
    bool PuedeReplicar,
    bool OmiteRls,
    long Membresias,
    long BasesDeDesarrolloEncontradas,
    long BasesDeDesarrolloConConnect);

/// <summary>Segunda capa: no confía en la cadena, sino en lo que informa el servidor.</summary>
public static class VerificacionIdentidadPostgres
{
    private const long BasesDeDesarrolloEsperadas = 2;

    private static readonly HashSet<string> DireccionesLocales = new(StringComparer.Ordinal)
    {
        "127.0.0.1",
        "::1"
    };

    // Las bases "marrow" y "Marrow" son distintas: se buscan por su nombre exacto.
    private const string ConsultaIdentidad = """
        SELECT current_database()::text,
               session_user::text,
               current_user::text,
               host(inet_server_addr()),
               inet_server_port(),
               pg_get_userbyid((SELECT datdba FROM pg_database WHERE datname = current_database()))::text,
               r.rolsuper,
               r.rolcreatedb,
               r.rolcreaterole,
               r.rolreplication,
               r.rolbypassrls,
               (SELECT count(*) FROM pg_auth_members a WHERE a.member = r.oid),
               (SELECT count(*) FROM pg_database d WHERE d.datname IN ('marrow', 'Marrow')),
               (SELECT count(*) FROM pg_database d
                WHERE d.datname IN ('marrow', 'Marrow')
                  AND has_database_privilege(current_user, d.oid, 'CONNECT'))
        FROM pg_roles r
        WHERE r.rolname = current_user
        """;

    public static IReadOnlyList<MotivoRechazoConexion> ValidarIdentidad(IdentidadPostgres identidad)
    {
        var motivos = new List<MotivoRechazoConexion>();
        if (identidad.BaseActual != ProteccionBaseDePruebas.BaseEsperada)
        {
            motivos.Add(MotivoRechazoConexion.BaseRealIncorrecta);
        }

        if (identidad.SessionUser != ProteccionBaseDePruebas.UsuarioEsperado)
        {
            motivos.Add(MotivoRechazoConexion.SessionUserIncorrecto);
        }

        if (identidad.CurrentUser != ProteccionBaseDePruebas.UsuarioEsperado)
        {
            motivos.Add(MotivoRechazoConexion.CurrentUserIncorrecto);
        }

        if (identidad.DireccionServidor is null || !DireccionesLocales.Contains(identidad.DireccionServidor))
        {
            motivos.Add(MotivoRechazoConexion.DireccionNoLocal);
        }

        if (identidad.PuertoServidor != ProteccionBaseDePruebas.PuertoEsperado)
        {
            motivos.Add(MotivoRechazoConexion.PuertoRealIncorrecto);
        }

        if (identidad.PropietarioBase != ProteccionBaseDePruebas.UsuarioEsperado)
        {
            motivos.Add(MotivoRechazoConexion.PropietarioIncorrecto);
        }

        if (identidad.EsSuperusuario
            || identidad.PuedeCrearBases
            || identidad.PuedeCrearRoles
            || identidad.PuedeReplicar
            || identidad.OmiteRls)
        {
            motivos.Add(MotivoRechazoConexion.PrivilegiosAdministrativos);
        }

        if (identidad.Membresias != 0)
        {
            motivos.Add(MotivoRechazoConexion.MembresiasEnOtrosRoles);
        }

        if (identidad.BasesDeDesarrolloEncontradas != BasesDeDesarrolloEsperadas)
        {
            motivos.Add(MotivoRechazoConexion.BasesDeDesarrolloNoEncontradas);
        }

        if (identidad.BasesDeDesarrolloConConnect != 0)
        {
            motivos.Add(MotivoRechazoConexion.ConnectEnBasesDeDesarrollo);
        }

        return motivos;
    }

    public static void Verificar(IdentidadPostgres identidad)
    {
        var motivos = ValidarIdentidad(identidad);
        if (motivos.Count > 0)
        {
            throw new ConexionDePruebaRechazadaException(motivos);
        }
    }

    /// <summary>Lee y verifica la identidad usando una conexión ya abierta por quien la llama.</summary>
    public static async Task VerificarConexionAsync(
        DbConnection conexion,
        CancellationToken cancellationToken = default) =>
        Verificar(await LeerIdentidadAsync(conexion, cancellationToken));

    /// <summary>Lee la identidad que informa el servidor usando una conexión ya abierta.</summary>
    public static async Task<IdentidadPostgres> LeerIdentidadAsync(
        DbConnection conexion,
        CancellationToken cancellationToken = default)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = ConsultaIdentidad;

        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("PostgreSQL no informó el rol de la conexión.");
        }

        return new IdentidadPostgres(
            lector.GetString(0),
            lector.GetString(1),
            lector.GetString(2),
            await lector.IsDBNullAsync(3, cancellationToken) ? null : lector.GetString(3),
            lector.GetInt32(4),
            lector.GetString(5),
            lector.GetBoolean(6),
            lector.GetBoolean(7),
            lector.GetBoolean(8),
            lector.GetBoolean(9),
            lector.GetBoolean(10),
            lector.GetInt64(11),
            lector.GetInt64(12),
            lector.GetInt64(13));
    }
}
