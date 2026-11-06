using System.Data.Common;
using Npgsql;

namespace Marrow.API.IntegrationTests.Infraestructura;

public sealed class BloqueoDePruebasOcupadoException()
    : Exception("Otra ejecución de la suite de integración retiene el bloqueo de marrow_test.");

public sealed class BloqueoDePruebasPerdidoException()
    : Exception("La sesión ya no retiene el bloqueo de marrow_test.");

/// <summary>
/// Exclusión mutua entre ejecuciones de la suite: debe adquirirse antes de migrar o limpiar marrow_test.
/// Usa un advisory lock de sesión. Se espera que PostgreSQL lo libere al terminar la sesión, incluso si el
/// proceso muere, pero eso todavía no se comprobó contra un servidor real.
/// </summary>
public sealed class BloqueoEjecucionPruebas : IAsyncDisposable
{
    /// <summary>Clave fija del bloqueo: los bytes ASCII de "MARROWTE".</summary>
    public const long Clave = 0x4D41_5252_4F57_5445;

    private readonly DbConnection conexion;
    private int liberado;

    private BloqueoEjecucionPruebas(DbConnection conexion)
    {
        this.conexion = conexion;
    }

    public static Task<BloqueoEjecucionPruebas> AdquirirAsync(
        string conexionValidada,
        CancellationToken cancellationToken = default) =>
        AdquirirConFabricaDeConexionAsync(
            conexionValidada,
            cadena => new NpgsqlConnection(cadena),
            cancellationToken);

    /// <summary>
    /// Punto de sustitución para dobles de prueba: solo permite reemplazar el objeto de conexión.
    /// La validación de la cadena y la verificación de identidad siempre se ejecutan.
    /// Solo es visible para la suite aislada.
    /// </summary>
    internal static async Task<BloqueoEjecucionPruebas> AdquirirConFabricaDeConexionAsync(
        string conexionValidada,
        Func<string, DbConnection> crearConexion,
        CancellationToken cancellationToken = default)
    {
        var conexion = crearConexion(CrearCadenaSinPool(conexionValidada));
        try
        {
            await conexion.OpenAsync(cancellationToken);
            await VerificacionIdentidadPostgres.VerificarConexionAsync(conexion, cancellationToken);

            await using var comando = conexion.CreateCommand();
            comando.CommandText = $"SELECT pg_try_advisory_lock({Clave})";

            // Cualquier resultado que no sea true cuenta como ocupado: ante la duda, no se continúa.
            if (await comando.ExecuteScalarAsync(cancellationToken) is not true)
            {
                throw new BloqueoDePruebasOcupadoException();
            }

            return new BloqueoEjecucionPruebas(conexion);
        }
        catch
        {
            await CerrarSinOcultarElErrorOriginalAsync(conexion);
            throw;
        }
    }

    /// <summary>Con Pooling=false, cerrar la conexión debe terminar la sesión en lugar de devolverla a un pool.</summary>
    public static string CrearCadenaSinPool(string conexionValidada)
    {
        ProteccionBaseDePruebas.ExigirCadenaValida(conexionValidada);
        return new NpgsqlConnectionStringBuilder(conexionValidada) { Pooling = false }.ConnectionString;
    }

    /// <summary>Confirma en la propia sesión que el bloqueo sigue retenido, para usar antes de migrar o limpiar.</summary>
    public async Task VerificarRetenidoAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref liberado) == 1)
        {
            throw new BloqueoDePruebasPerdidoException();
        }

        await using var comando = conexion.CreateCommand();

        // PostgreSQL guarda una clave bigint en dos partes de 32 bits, con objsubid = 1.
        comando.CommandText = $"""
            SELECT EXISTS (
                SELECT 1 FROM pg_locks
                WHERE locktype = 'advisory'
                  AND pid = pg_backend_pid()
                  AND granted
                  AND classid = {(uint)(Clave >> 32)}
                  AND objid = {unchecked((uint)Clave)}
                  AND objsubid = 1)
            """;

        if (await comando.ExecuteScalarAsync(cancellationToken) is not true)
        {
            throw new BloqueoDePruebasPerdidoException();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref liberado, 1) == 1)
        {
            return;
        }

        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = $"SELECT pg_advisory_unlock({Clave})";
            await comando.ExecuteScalarAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // No es un error de la suite: cerrar la conexión también debe terminar la sesión.
        }
        finally
        {
            await conexion.DisposeAsync();
        }
    }

    private static async Task CerrarSinOcultarElErrorOriginalAsync(DbConnection conexion)
    {
        try
        {
            await conexion.DisposeAsync();
        }
        catch (Exception)
        {
            // Quien llama vuelve a lanzar el error de la inicialización, que es el que importa.
        }
    }
}
