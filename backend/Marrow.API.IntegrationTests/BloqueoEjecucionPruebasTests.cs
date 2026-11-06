using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Marrow.API.IntegrationTests.Infraestructura;
using Npgsql;

namespace Marrow.API.IntegrationTests;

// Solo con dobles: comprueban la lógica del ciclo de vida, no el comportamiento real de PostgreSQL.
// Ninguna prueba usa la sobrecarga real, así que una regresión en la validación tampoco abriría una conexión.
public class BloqueoEjecucionPruebasTests
{
    private const string ClaveFicticia = "CLAVE-FICTICIA-NO-MOSTRAR";
    private const string CadenaValida =
        $"Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}";

    private static readonly string ConsultaBloqueo = $"SELECT pg_try_advisory_lock({BloqueoEjecucionPruebas.Clave})";
    private static readonly string ConsultaDesbloqueo = $"SELECT pg_advisory_unlock({BloqueoEjecucionPruebas.Clave})";

    [Fact]
    public void Clave_EsFijaYCorrespondeAlTextoDocumentado()
    {
        var bytes = BitConverter.GetBytes(BloqueoEjecucionPruebas.Clave).Reverse().ToArray();

        Assert.Equal("MARROWTE", System.Text.Encoding.ASCII.GetString(bytes));
    }

    [Fact]
    public async Task Adquirir_Libre_AbreVerificaIdentidadYBloqueaEnEseOrden()
    {
        var servidor = new ServidorFalso();

        await using var bloqueo = await Adquirir(servidor);

        Assert.Equal(["abrir", "identidad", ConsultaBloqueo], servidor.Eventos);
        Assert.Equal(1, servidor.ConexionesAbiertas);
        Assert.True(servidor.Ocupado);
    }

    [Fact]
    public async Task Adquirir_RecibeUnaCadenaSinPoolHaciaElMismoDestino()
    {
        var servidor = new ServidorFalso();

        await using var bloqueo = await Adquirir(servidor);

        var cadena = new NpgsqlConnectionStringBuilder(Assert.Single(servidor.CadenasRecibidas));
        Assert.False(cadena.Pooling);
        Assert.Equal("marrow_test", cadena.Database);
        Assert.Equal("marrow_test", cadena.Username);
    }

    [Fact]
    public async Task Adquirir_Ocupado_SeRechazaYCierraLaConexion()
    {
        var servidor = new ServidorFalso { Ocupado = true };

        await Assert.ThrowsAsync<BloqueoDePruebasOcupadoException>(() => Adquirir(servidor));

        Assert.Equal(["abrir", "identidad", ConsultaBloqueo, "cerrar"], servidor.Eventos);
        Assert.Equal(0, servidor.ConexionesAbiertas);
        Assert.True(servidor.Ocupado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData("true")]
    public async Task Adquirir_ResultadoInesperado_CuentaComoOcupado(object? resultado)
    {
        var servidor = new ServidorFalso { ResultadoDeBloqueoForzado = () => resultado };

        await Assert.ThrowsAsync<BloqueoDePruebasOcupadoException>(() => Adquirir(servidor));

        Assert.Equal(0, servidor.ConexionesAbiertas);
    }

    [Fact]
    public async Task Adquirir_MientrasOtroLoRetiene_SeRechazaYDespuesDeLiberarFunciona()
    {
        var servidor = new ServidorFalso();

        var primero = await Adquirir(servidor);
        await Assert.ThrowsAsync<BloqueoDePruebasOcupadoException>(() => Adquirir(servidor));
        Assert.Equal(1, servidor.ConexionesAbiertas);

        await primero.DisposeAsync();
        await using var tercero = await Adquirir(servidor);

        Assert.True(servidor.Ocupado);
    }

    [Fact]
    public async Task Liberar_DesbloqueaYCierraLaConexion()
    {
        var servidor = new ServidorFalso();
        var bloqueo = await Adquirir(servidor);
        servidor.Eventos.Clear();

        await bloqueo.DisposeAsync();

        Assert.Equal([ConsultaDesbloqueo, "cerrar"], servidor.Eventos);
        Assert.False(servidor.Ocupado);
        Assert.Equal(0, servidor.ConexionesAbiertas);
    }

    [Fact]
    public async Task Liberar_DosVeces_SoloActuaUnaVez()
    {
        var servidor = new ServidorFalso();
        var bloqueo = await Adquirir(servidor);
        servidor.Eventos.Clear();

        await bloqueo.DisposeAsync();
        await bloqueo.DisposeAsync();

        Assert.Equal([ConsultaDesbloqueo, "cerrar"], servidor.Eventos);
    }

    [Fact]
    public async Task Liberar_SiFallaElDesbloqueo_NoLanzaYCierraLaConexion()
    {
        var servidor = new ServidorFalso();
        var bloqueo = await Adquirir(servidor);
        servidor.FallarDesbloqueo = true;

        await bloqueo.DisposeAsync();

        Assert.Equal(0, servidor.ConexionesAbiertas);
        Assert.False(servidor.Ocupado);
    }

    [Fact]
    public async Task CorteDeConexion_SinDesbloquear_LiberaElBloqueoDeSesion()
    {
        var servidor = new ServidorFalso();
        await Adquirir(servidor);

        servidor.SimularCorteDeSesion();
        await using var siguiente = await Adquirir(servidor);

        Assert.True(servidor.Ocupado);
    }

    [Fact]
    public async Task Inicializacion_SiFallaLaApertura_PropagaElErrorSinBloquear()
    {
        var servidor = new ServidorFalso { FallarApertura = true };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Adquirir(servidor));

        Assert.False(servidor.Ocupado);
        Assert.DoesNotContain(ConsultaBloqueo, servidor.Eventos);
        Assert.Equal(0, servidor.ConexionesAbiertas);
    }

    public static TheoryData<IdentidadDePrueba, MotivoRechazoConexion> IdentidadesIncorrectas() => new()
    {
        { new IdentidadDePrueba("marrow", "marrow_test"), MotivoRechazoConexion.BaseRealIncorrecta },
        { new IdentidadDePrueba("Marrow", "marrow_test"), MotivoRechazoConexion.BaseRealIncorrecta },
        { new IdentidadDePrueba("marrow_test", "postgres"), MotivoRechazoConexion.SessionUserIncorrecto }
    };

    [Theory]
    [MemberData(nameof(IdentidadesIncorrectas))]
    public async Task Inicializacion_SiLaIdentidadEsIncorrecta_NoIntentaBloquear(
        IdentidadDePrueba identidad,
        MotivoRechazoConexion motivo)
    {
        var servidor = new ServidorFalso { Identidad = identidad };

        var error = await Assert.ThrowsAsync<ConexionDePruebaRechazadaException>(() => Adquirir(servidor));

        Assert.Contains(motivo, error.Motivos);
        Assert.Equal(["abrir", "identidad", "cerrar"], servidor.Eventos);
        Assert.False(servidor.Ocupado);
    }

    [Fact]
    public async Task Inicializacion_SiFallaLaConsultaDeBloqueo_CierraYPropaga()
    {
        var servidor = new ServidorFalso { FallarBloqueo = true };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Adquirir(servidor));

        Assert.False(servidor.Ocupado);
        Assert.Equal(0, servidor.ConexionesAbiertas);
    }

    [Fact]
    public async Task Inicializacion_SiTambienFallaElCierre_ConservaElErrorOriginal()
    {
        var servidor = new ServidorFalso { FallarBloqueo = true, FallarCierre = true };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Adquirir(servidor));

        Assert.Equal("Falla simulada del bloqueo.", error.Message);
    }

    [Fact]
    public async Task Inicializacion_Ocupada_ConFallaDeCierre_ConservaElRechazo()
    {
        var servidor = new ServidorFalso { Ocupado = true, FallarCierre = true };

        await Assert.ThrowsAsync<BloqueoDePruebasOcupadoException>(() => Adquirir(servidor));
    }

    [Fact]
    public async Task Inicializacion_Cancelada_NoBloqueaYNoDejaConexiones()
    {
        var servidor = new ServidorFalso();
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Adquirir(servidor, cancelacion.Token));

        Assert.False(servidor.Ocupado);
        Assert.Equal(0, servidor.ConexionesAbiertas);
    }

    [Fact]
    public async Task VerificarRetenido_MientrasLoRetiene_NoLanza()
    {
        var servidor = new ServidorFalso();
        await using var bloqueo = await Adquirir(servidor);

        await bloqueo.VerificarRetenidoAsync();
    }

    [Fact]
    public async Task VerificarRetenido_ConsultaLaClaveEnSuPropiaSesion()
    {
        var servidor = new ServidorFalso();
        await using var bloqueo = await Adquirir(servidor);

        await bloqueo.VerificarRetenidoAsync();

        var consulta = servidor.Eventos[^1];
        Assert.Contains("pg_backend_pid()", consulta);
        Assert.Contains($"classid = {(uint)(BloqueoEjecucionPruebas.Clave >> 32)}", consulta);
        Assert.Contains($"objid = {unchecked((uint)BloqueoEjecucionPruebas.Clave)}", consulta);
        Assert.Contains("objsubid = 1", consulta);
    }

    [Fact]
    public async Task VerificarRetenido_SiElServidorNoLoConfirma_Lanza()
    {
        var servidor = new ServidorFalso();
        await using var bloqueo = await Adquirir(servidor);
        servidor.RetencionConfirmada = false;

        await Assert.ThrowsAsync<BloqueoDePruebasPerdidoException>(() => bloqueo.VerificarRetenidoAsync());
    }

    [Fact]
    public async Task VerificarRetenido_DespuesDeLiberar_LanzaSinConsultar()
    {
        var servidor = new ServidorFalso();
        var bloqueo = await Adquirir(servidor);
        await bloqueo.DisposeAsync();
        servidor.Eventos.Clear();

        await Assert.ThrowsAsync<BloqueoDePruebasPerdidoException>(() => bloqueo.VerificarRetenidoAsync());

        Assert.Empty(servidor.Eventos);
    }

    [Fact]
    public void CadenaSinPool_DesactivaElPoolYConservaElDestino()
    {
        var cadena = new NpgsqlConnectionStringBuilder(BloqueoEjecucionPruebas.CrearCadenaSinPool(CadenaValida));

        Assert.False(cadena.Pooling);
        Assert.False(cadena.Multiplexing);
        Assert.Equal("localhost", cadena.Host);
        Assert.Equal(5432, cadena.Port);
        Assert.Equal("marrow_test", cadena.Database);
        Assert.Equal("marrow_test", cadena.Username);
        Assert.Equal(ClaveFicticia, cadena.Password);
    }

    [Theory]
    [InlineData("Database=marrow", MotivoRechazoConexion.BaseIncorrecta)]
    [InlineData("Database=Marrow", MotivoRechazoConexion.BaseIncorrecta)]
    [InlineData("Username=postgres", MotivoRechazoConexion.UsuarioIncorrecto)]
    [InlineData("Host=otro.invalid", MotivoRechazoConexion.HostIncorrecto)]
    [InlineData("Port=5433", MotivoRechazoConexion.PuertoIncorrecto)]
    public async Task Adquirir_ConCadenaIncorrecta_SeRechazaSinCrearConexion(
        string reemplazo,
        MotivoRechazoConexion motivo)
    {
        var parametros = CadenaValida.Split(';')
            .Select(parametro => parametro.StartsWith(reemplazo.Split('=')[0] + "=", StringComparison.Ordinal)
                ? reemplazo
                : parametro);
        var servidor = new ServidorFalso();

        var error = await Assert.ThrowsAsync<ConexionDePruebaRechazadaException>(
            () => BloqueoEjecucionPruebas.AdquirirConFabricaDeConexionAsync(
                string.Join(';', parametros),
                servidor.CrearConexion));

        Assert.Equal([motivo], error.Motivos);
        Assert.Empty(servidor.CadenasRecibidas);
        Assert.DoesNotContain(ClaveFicticia, error.ToString());
    }

    [Fact]
    public async Task Rechazos_NoRevelanCredenciales()
    {
        var servidor = new ServidorFalso { Ocupado = true };

        var error = await Assert.ThrowsAsync<BloqueoDePruebasOcupadoException>(() => Adquirir(servidor));

        Assert.DoesNotContain("Password", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static Task<BloqueoEjecucionPruebas> Adquirir(
        ServidorFalso servidor,
        CancellationToken cancellationToken = default) =>
        BloqueoEjecucionPruebas.AdquirirConFabricaDeConexionAsync(CadenaValida, servidor.CrearConexion, cancellationToken);

    public sealed record IdentidadDePrueba(string Base, string Usuario);

    // Simula el bloqueo de sesión: pertenece a una conexión y se libera al cerrarla.
    private sealed class ServidorFalso
    {
        private ConexionFalsa? duenio;

        public List<string> Eventos { get; } = [];

        public List<string> CadenasRecibidas { get; } = [];

        public IdentidadDePrueba Identidad { get; init; } = new("marrow_test", "marrow_test");

        public bool Ocupado { get; set; }

        public int ConexionesAbiertas { get; set; }

        public bool FallarApertura { get; init; }

        public bool FallarBloqueo { get; init; }

        public bool FallarCierre { get; init; }

        public bool FallarDesbloqueo { get; set; }

        public bool RetencionConfirmada { get; set; } = true;

        public Func<object?>? ResultadoDeBloqueoForzado { get; init; }

        public DbConnection CrearConexion(string cadena)
        {
            CadenasRecibidas.Add(cadena);
            return new ConexionFalsa(this);
        }

        public void SimularCorteDeSesion()
        {
            duenio?.Dispose();
        }

        public DataTableReader LeerIdentidad()
        {
            Eventos.Add("identidad");

            var tabla = new DataTable();
            foreach (var columna in new[] { "base", "session", "current", "direccion" })
            {
                tabla.Columns.Add(columna, typeof(string));
            }

            tabla.Columns.Add("puerto", typeof(int));
            tabla.Columns.Add("propietario", typeof(string));
            foreach (var columna in new[] { "super", "bases", "roles", "replica", "rls" })
            {
                tabla.Columns.Add(columna, typeof(bool));
            }

            foreach (var columna in new[] { "membresias", "encontradas", "connect" })
            {
                tabla.Columns.Add(columna, typeof(long));
            }

            tabla.Rows.Add(
                Identidad.Base,
                Identidad.Usuario,
                Identidad.Usuario,
                "::1",
                5432,
                "marrow_test",
                false,
                false,
                false,
                false,
                false,
                0L,
                2L,
                0L);
            return tabla.CreateDataReader();
        }

        public object? Ejecutar(ConexionFalsa conexion, string sql)
        {
            Eventos.Add(sql);
            if (sql.Contains("pg_try_advisory_lock", StringComparison.Ordinal))
            {
                if (FallarBloqueo)
                {
                    throw new InvalidOperationException("Falla simulada del bloqueo.");
                }

                if (ResultadoDeBloqueoForzado is not null)
                {
                    return ResultadoDeBloqueoForzado();
                }

                if (Ocupado)
                {
                    return false;
                }

                Ocupado = true;
                duenio = conexion;
                return true;
            }

            if (sql.Contains("pg_locks", StringComparison.Ordinal))
            {
                return RetencionConfirmada && ReferenceEquals(duenio, conexion);
            }

            if (FallarDesbloqueo)
            {
                throw new InvalidOperationException("Falla simulada del desbloqueo.");
            }

            Ocupado = false;
            duenio = null;
            return true;
        }

        public void AlCerrar(ConexionFalsa conexion)
        {
            Eventos.Add("cerrar");
            ConexionesAbiertas--;
            if (ReferenceEquals(duenio, conexion))
            {
                Ocupado = false;
                duenio = null;
            }

            if (FallarCierre)
            {
                throw new InvalidOperationException("Falla simulada del cierre.");
            }
        }
    }

    private sealed class ConexionFalsa(ServidorFalso servidor) : DbConnection
    {
        private ConnectionState estado = ConnectionState.Closed;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "sin_conexion";

        public override string DataSource => "sin_conexion";

        public override string ServerVersion => "18.0";

        public override ConnectionState State => estado;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close()
        {
            if (estado == ConnectionState.Open)
            {
                estado = ConnectionState.Closed;
                servidor.AlCerrar(this);
            }
        }

        public override void Open()
        {
            if (servidor.FallarApertura)
            {
                throw new InvalidOperationException("Falla simulada de la apertura.");
            }

            servidor.Eventos.Add("abrir");
            servidor.ConexionesAbiertas++;
            estado = ConnectionState.Open;
        }

        // Como NpgsqlConnection: liberar la conexión cierra la sesión.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }

            base.Dispose(disposing);
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => new ComandoFalso(servidor, this);
    }

    private sealed class ComandoFalso(ServidorFalso servidor, ConexionFalsa conexion) : DbCommand
    {
        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => throw new NotSupportedException();

        public override object? ExecuteScalar() => servidor.Ejecutar(conexion, CommandText);

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => servidor.LeerIdentidad();
    }
}
