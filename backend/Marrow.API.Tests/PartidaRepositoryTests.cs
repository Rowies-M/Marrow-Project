using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Marrow.API.Data;
using Marrow.API.Models;
using Marrow.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Marrow.API.Tests;

public class PartidaRepositoryTests
{
    // Nunca se abre esta conexión: los interceptores suprimen la apertura y la ejecución del comando.
    private const string ConexionInexistente = "Host=127.0.0.1;Port=1;Database=sin_conexion";

    [Fact]
    public async Task MoverAsync_GeneraUnUpdateQueExcluyeLaCeldaDelEnemigo()
    {
        var sql = await CapturarSqlDeMoverAsync();

        var where = sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
        Assert.Contains(
            "(p.\"PersonajeX\" + @deltaX <> p.\"EnemigoX\" OR p.\"PersonajeY\" + @deltaY <> p.\"EnemigoY\")",
            where);
    }

    [Fact]
    public async Task MoverAsync_ConservaLosFiltrosDePartidaPropietarioYEstado()
    {
        var sql = await CapturarSqlDeMoverAsync();

        var where = sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
        Assert.Contains("p.\"Id\" = @id", where);
        Assert.Contains("\"UsuarioId\" = @usuarioId", where);
        Assert.Contains("p.\"Estado\" = 0", where);
    }

    [Theory]
    [InlineData("WHEN p.\"PersonajeX\" + @deltaX < 0 THEN 0")]
    [InlineData("WHEN p.\"PersonajeY\" + @deltaY < 0 THEN 0")]
    public async Task MoverAsync_ConservaLosLimitesInferioresDelTablero(string condicion)
    {
        var sql = await CapturarSqlDeMoverAsync();

        var inicioSet = sql.IndexOf("SET ", StringComparison.Ordinal);
        var finSet = sql.IndexOf("FROM", inicioSet, StringComparison.Ordinal);
        Assert.Contains(condicion, sql[inicioSet..finSet]);
    }

    [Fact]
    public async Task MoverAsync_SoloActualizaLasCoordenadasDelPersonajeDentroDeLosLimites()
    {
        var sql = await CapturarSqlDeMoverAsync();

        var inicioSet = sql.IndexOf("SET ", StringComparison.Ordinal);
        var finSet = sql.IndexOf("FROM", inicioSet, StringComparison.Ordinal);
        var set = sql[inicioSet..finSet];
        Assert.Contains("\"PersonajeX\" = CASE", set);
        Assert.Contains("\"PersonajeY\" = CASE", set);
        Assert.Contains("> 10 THEN 10", set);
        Assert.Contains("> 6 THEN 6", set);
        Assert.DoesNotContain("Enemigo", set);
        Assert.DoesNotContain("Vida", set);
        Assert.DoesNotContain("Estado", set);
    }

    [Fact]
    public async Task AplicarAtaque_CompruebaElEstadoLeidoEnElUpdateDeLaPartida()
    {
        var (comandos, transaccion) = await EjecutarAtaqueAsync();

        var where = comandos.Sql[0][comandos.Sql[0].IndexOf("WHERE", StringComparison.Ordinal)..];
        foreach (var condicion in new[]
        {
            "p.\"Id\" = @",
            "p0.\"UsuarioId\" = @",
            "p.\"Estado\" = 0",
            "p.\"Dificultad\" = @",
            "p.\"VidaPersonajeActual\" = @",
            "p.\"VidaEnemigoActual\" = @",
            "p.\"PersonajeX\" = @",
            "p.\"PersonajeY\" = @",
            "p.\"EnemigoX\" = @",
            "p.\"EnemigoY\" = @"
        })
        {
            Assert.Contains(condicion, where);
        }

        Assert.Equal(1, transaccion.Inicios);
        Assert.Equal(1, transaccion.Commits);
    }

    [Fact]
    public async Task AplicarAtaque_SoloEscribeVidasEstadoResultadoYFecha()
    {
        var (comandos, _) = await EjecutarAtaqueAsync();

        var sql = comandos.Sql[0];
        var inicioSet = sql.IndexOf("SET ", StringComparison.Ordinal);
        var finSet = sql.IndexOf("FROM", inicioSet, StringComparison.Ordinal);
        var set = sql[inicioSet..finSet];
        Assert.Contains("\"VidaPersonajeActual\" = @", set);
        Assert.Contains("\"VidaEnemigoActual\" = @", set);
        Assert.Contains("\"Estado\" = @", set);
        Assert.Contains("\"ResultadoCombate\" = @", set);
        Assert.Contains("\"FechaUltimaActualizacion\" = now()", set);
        Assert.DoesNotContain("PersonajeX", set);
        Assert.DoesNotContain("PersonajeY", set);
        Assert.DoesNotContain("EnemigoX", set);
        Assert.DoesNotContain("EnemigoY", set);
        Assert.DoesNotContain("Dificultad", set);
    }

    [Fact]
    public async Task AplicarAtaque_ComparaContraLosValoresLeidos()
    {
        var (comandos, _) = await EjecutarAtaqueAsync();

        Assert.Equal(17, ValorEnWhere(comandos, 0, "Id"));
        Assert.Equal(42, ValorEnWhere(comandos, 0, "UsuarioId"));
        Assert.Equal(0, ValorEnWhere(comandos, 0, "Dificultad"));
        Assert.Equal(60, ValorEnWhere(comandos, 0, "VidaPersonajeActual"));
        Assert.Equal(10, ValorEnWhere(comandos, 0, "VidaEnemigoActual"));
        Assert.Equal(4, ValorEnWhere(comandos, 0, "PersonajeX"));
        Assert.Equal(3, ValorEnWhere(comandos, 0, "PersonajeY"));
        Assert.Equal(5, ValorEnWhere(comandos, 0, "EnemigoX"));
        Assert.Equal(3, ValorEnWhere(comandos, 0, "EnemigoY"));
    }

    [Fact]
    public async Task AplicarAtaque_ConResultado_FinalizaYGuardaLosValoresCalculados()
    {
        var (comandos, _) = await EjecutarAtaqueAsync(vidaPersonaje: 60, vidaEnemigo: 0);

        Assert.Equal(60, ValorEnSet(comandos, 0, "VidaPersonajeActual"));
        Assert.Equal(0, ValorEnSet(comandos, 0, "VidaEnemigoActual"));
        Assert.Equal(1, ValorEnSet(comandos, 0, "Estado"));
        Assert.Equal(1, ValorEnSet(comandos, 0, "ResultadoCombate"));
    }

    [Fact]
    public async Task AplicarAtaque_SinResultado_SigueActivaConResultadoNulo()
    {
        var (comandos, _) = await EjecutarAtaqueAsync(vidaPersonaje: 50, vidaEnemigo: 5, resultado: null);

        Assert.Equal(50, ValorEnSet(comandos, 0, "VidaPersonajeActual"));
        Assert.Equal(5, ValorEnSet(comandos, 0, "VidaEnemigoActual"));
        Assert.Equal(0, ValorEnSet(comandos, 0, "Estado"));
        Assert.Contains("\"ResultadoCombate\" = NULL", comandos.Sql[0]);
    }

    [Fact]
    public async Task AplicarAtaque_ConDesbloqueo_SubeLaDificultadDentroDeLaTransaccion()
    {
        var (comandos, transaccion) = await EjecutarAtaqueAsync(desbloquearDificil: true);

        Assert.Equal(2, comandos.Sql.Count);
        Assert.Contains("UPDATE \"Partidas\"", comandos.Sql[0]);
        var desbloqueo = comandos.Sql[1];
        Assert.Contains("UPDATE \"Personajes\"", desbloqueo);
        Assert.Equal(1, ValorEnSet(comandos, 1, "DificultadDesbloqueada"));
        var where = desbloqueo[desbloqueo.IndexOf("WHERE", StringComparison.Ordinal)..];
        Assert.Contains("p.\"DificultadDesbloqueada\" = 0", where);
        Assert.Equal(3, ValorEnWhere(comandos, 1, "Id"));
        Assert.Equal(42, ValorEnWhere(comandos, 1, "UsuarioId"));
        Assert.Equal(1, transaccion.Inicios);
        Assert.Equal(1, transaccion.Commits);
        Assert.Equal([true, true], comandos.EnTransaccionAbierta);
        Assert.Equal([0, 0], comandos.CommitsAlEjecutar);
    }

    [Fact]
    public async Task AplicarAtaque_SinDesbloqueo_NoEscribeElPersonaje()
    {
        var (comandos, transaccion) = await EjecutarAtaqueAsync(desbloquearDificil: false);

        Assert.Single(comandos.Sql);
        Assert.Equal(1, transaccion.Commits);
    }

    [Fact]
    public async Task AplicarAtaque_SinFilasEnLaPartida_NoDesbloquea()
    {
        var (comandos, _) = await EjecutarAtaqueAsync(desbloquearDificil: true, filasPartida: 0, filasEsperadas: 0);

        Assert.Contains("UPDATE \"Partidas\"", Assert.Single(comandos.Sql));
    }

    [Fact]
    public async Task AplicarAtaque_PersonajeYaDesbloqueado_NoEsError()
    {
        var (comandos, transaccion) = await EjecutarAtaqueAsync(desbloquearDificil: true, filasPersonaje: 0);

        Assert.Equal(2, comandos.Sql.Count);
        Assert.Equal(1, transaccion.Commits);
    }

    [Fact]
    public async Task AplicarAtaque_SiFallaElDesbloqueo_NoConfirmaLaTransaccion()
    {
        var comandos = new ComandosDeAtaque { FallarPersonaje = true };
        var transaccion = new ConexionFalsa();
        await using var context = CrearContextoDeAtaque(comandos, transaccion);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new PartidaRepository(context).AplicarAtaqueAsync(
            CrearPartidaLeida(),
            42,
            60,
            0,
            ResultadoCombate.Victoria,
            desbloquearDificil: true));

        Assert.Equal(2, comandos.Sql.Count);
        Assert.Equal(1, transaccion.Inicios);
        Assert.Equal(0, transaccion.Commits);
        Assert.True(transaccion.Liberada);
    }

    [Fact]
    public async Task AplicarAtaque_ConProgreso_LoGuardaDentroDeLaTransaccionYComparaLoLeido()
    {
        var (comandos, transaccion) = await EjecutarAtaqueAsync(progreso: new ProgresoPersonaje(3, 40));

        Assert.Equal(2, comandos.Sql.Count);
        var progreso = comandos.Sql[1];
        Assert.Contains("UPDATE \"Personajes\"", progreso);
        Assert.Equal(3, ValorEnSet(comandos, 1, "Nivel"));
        Assert.Equal(40, ValorEnSet(comandos, 1, "Experiencia"));
        Assert.Equal(3, ValorEnWhere(comandos, 1, "Id"));
        Assert.Equal(42, ValorEnWhere(comandos, 1, "UsuarioId"));
        Assert.Equal(2, ValorEnWhere(comandos, 1, "Nivel"));
        Assert.Equal(50, ValorEnWhere(comandos, 1, "Experiencia"));
        Assert.Equal([true, true], comandos.EnTransaccionAbierta);
        Assert.Equal([0, 0], comandos.CommitsAlEjecutar);
        Assert.Equal(1, transaccion.Commits);
    }

    [Fact]
    public async Task AplicarAtaque_SinFilasEnLaPartida_NoOtorgaProgreso()
    {
        var (comandos, _) = await EjecutarAtaqueAsync(
            progreso: new ProgresoPersonaje(3, 40),
            filasPartida: 0,
            filasEsperadas: 0);

        Assert.Contains("UPDATE \"Partidas\"", Assert.Single(comandos.Sql));
    }

    [Fact]
    public async Task AplicarAtaque_SiElPersonajeCambio_NoConfirmaYDevuelveCero()
    {
        var (comandos, transaccion) = await EjecutarAtaqueAsync(
            progreso: new ProgresoPersonaje(3, 40),
            filasPersonaje: 0,
            filasEsperadas: 0);

        Assert.Equal(2, comandos.Sql.Count);
        Assert.Equal(0, transaccion.Commits);
        Assert.True(transaccion.Liberada);
    }

    [Fact]
    public async Task AplicarAtaque_SinProgreso_NoEscribeNivelNiExperiencia()
    {
        var (comandos, _) = await EjecutarAtaqueAsync();

        Assert.All(comandos.Sql, sql =>
        {
            Assert.DoesNotContain("\"Nivel\"", sql);
            Assert.DoesNotContain("\"Experiencia\"", sql);
        });
    }

    private static async Task<(ComandosDeAtaque Comandos, ConexionFalsa Transaccion)> EjecutarAtaqueAsync(
        bool desbloquearDificil = false,
        ProgresoPersonaje? progreso = null,
        int filasPartida = 1,
        int filasPersonaje = 1,
        int filasEsperadas = 1,
        int vidaPersonaje = 60,
        int vidaEnemigo = 0,
        ResultadoCombate? resultado = ResultadoCombate.Victoria)
    {
        var comandos = new ComandosDeAtaque { FilasPartida = filasPartida, FilasPersonaje = filasPersonaje };
        var transaccion = new ConexionFalsa();
        await using var context = CrearContextoDeAtaque(comandos, transaccion);

        var filas = await new PartidaRepository(context).AplicarAtaqueAsync(
            CrearPartidaLeida(),
            42,
            vidaPersonaje,
            vidaEnemigo,
            resultado,
            desbloquearDificil,
            progreso);

        Assert.Equal(filasEsperadas, filas);
        return (comandos, transaccion);
    }

    private static int? ValorEnSet(ComandosDeAtaque comandos, int indice, string columna)
    {
        var sql = comandos.Sql[indice];
        var inicio = sql.IndexOf("SET ", StringComparison.Ordinal);
        var fin = sql.IndexOf("WHERE", inicio, StringComparison.Ordinal);
        return ValorDeParametro(comandos, indice, sql[inicio..fin], $"\"{columna}\" = @(\\w+)");
    }

    private static int? ValorEnWhere(ComandosDeAtaque comandos, int indice, string columna)
    {
        var sql = comandos.Sql[indice];
        var where = sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
        return ValorDeParametro(comandos, indice, where, $"\\.\"{columna}\" = @(\\w+)");
    }

    private static int? ValorDeParametro(ComandosDeAtaque comandos, int indice, string seccion, string patron)
    {
        var coincidencia = Regex.Match(seccion, patron);
        Assert.True(coincidencia.Success, $"No se encontró {patron} en {seccion}");
        var valor = comandos.Parametros[indice][coincidencia.Groups[1].Value];
        return valor is null or DBNull ? null : Convert.ToInt32(valor);
    }

    private static MarrowDbContext CrearContextoDeAtaque(ComandosDeAtaque comandos, ConexionFalsa conexion)
    {
        comandos.Conexion = conexion;
        return new MarrowDbContext(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(conexion)
            .AddInterceptors(comandos)
            .Options);
    }

    private static Partida CrearPartidaLeida() => new()
    {
        Id = 17,
        PersonajeId = 3,
        Personaje = new Personaje { Id = 3, Nivel = 2, Experiencia = 50 },
        Estado = EstadoPartida.Activa,
        Dificultad = Dificultad.Normal,
        VidaPersonajeActual = 60,
        VidaEnemigoActual = 10,
        PersonajeX = 4,
        PersonajeY = 3,
        EnemigoX = 5,
        EnemigoY = 3
    };

    private sealed class ComandosDeAtaque : DbCommandInterceptor
    {
        public int FilasPartida { get; init; } = 1;

        public int FilasPersonaje { get; init; } = 1;

        public bool FallarPersonaje { get; init; }

        public List<string> Sql { get; } = [];

        public List<Dictionary<string, object?>> Parametros { get; } = [];

        public List<bool> EnTransaccionAbierta { get; } = [];

        public List<int> CommitsAlEjecutar { get; } = [];

        public ConexionFalsa? Conexion { get; set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            Parametros.Add(command.Parameters.Cast<DbParameter>()
                .ToDictionary(parametro => parametro.ParameterName.TrimStart('@'), parametro => parametro.Value));
            EnTransaccionAbierta.Add(command.Transaction is TransaccionFalsa { Liberada: false });
            CommitsAlEjecutar.Add(Conexion?.Commits ?? 0);

            var esPersonaje = command.CommandText.Contains("UPDATE \"Personajes\"");
            if (esPersonaje && FallarPersonaje)
            {
                throw new InvalidOperationException("Falla simulada del personaje.");
            }

            return ValueTask.FromResult(
                InterceptionResult<int>.SuppressWithResult(esPersonaje ? FilasPersonaje : FilasPartida));
        }
    }

    // Conexión en memoria y siempre abierta: EF descartaría la transacción si la viera cerrada.
    private sealed class ConexionFalsa : DbConnection
    {
        private TransaccionFalsa? transaccion;

        public int Inicios { get; private set; }

        public int Commits => transaccion?.Commits ?? 0;

        public bool Liberada => transaccion?.Liberada ?? false;

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "sin_conexion";

        public override string DataSource => "sin_conexion";

        public override string ServerVersion => "18.0";

        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close()
        {
        }

        public override void Open()
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            Inicios++;
            transaccion = new TransaccionFalsa(this);
            return transaccion;
        }

        protected override DbCommand CreateDbCommand() =>
            new ComandoQueAceptaTransaccionFalsa(new NpgsqlCommand()) { Connection = this };
    }

    private sealed class TransaccionFalsa(DbConnection conexion) : DbTransaction
    {
        public int Commits { get; private set; }

        public bool Liberada { get; private set; }

        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        protected override DbConnection DbConnection => conexion;

        public override void Commit() => Commits++;

        public override void Rollback()
        {
        }

        protected override void Dispose(bool disposing)
        {
            Liberada = true;
            base.Dispose(disposing);
        }
    }

    // Usa los parámetros de NpgsqlCommand, pero acepta una conexión y una transacción falsas.
    private sealed class ComandoQueAceptaTransaccionFalsa(DbCommand interno) : DbCommand
    {
        [AllowNull]
        public override string CommandText
        {
            get => interno.CommandText;
            set => interno.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => interno.CommandTimeout;
            set => interno.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => interno.CommandType;
            set => interno.CommandType = value;
        }

        public override bool DesignTimeVisible
        {
            get => interno.DesignTimeVisible;
            set => interno.DesignTimeVisible = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => interno.UpdatedRowSource;
            set => interno.UpdatedRowSource = value;
        }

        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection => interno.Parameters;

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() => interno.Cancel();

        public override int ExecuteNonQuery() => throw new NotSupportedException();

        public override object? ExecuteScalar() => throw new NotSupportedException();

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => interno.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task Listar_SiempreFiltraPorUsuarioYNoCargaRelaciones()
    {
        var (consulta, conteo, _) = await EjecutarListadoAsync();

        foreach (var sql in new[] { consulta.Texto, conteo.Texto })
        {
            Assert.Contains("\"UsuarioId\" = @usuarioId", sql);
        }

        Assert.Equal(42, consulta.Parametros["usuarioId"]);
        Assert.Equal(42, conteo.Parametros["usuarioId"]);
        var columnas = consulta.Texto[..consulta.Texto.IndexOf("FROM", StringComparison.Ordinal)];
        Assert.DoesNotContain("\"Nombre\"", columnas);
        Assert.DoesNotContain("Enemigos", consulta.Texto);
    }

    [Fact]
    public async Task Listar_SinFiltros_NoRestringeEstadoNiDificultad()
    {
        var (consulta, conteo, _) = await EjecutarListadoAsync();

        foreach (var sql in new[] { consulta.Texto, conteo.Texto })
        {
            var where = sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
            Assert.DoesNotContain("\"Estado\" =", where);
            Assert.DoesNotContain("\"Dificultad\" =", where);
        }
    }

    [Fact]
    public async Task Listar_ConFiltros_LosAplicaALosResultadosYAlTotal()
    {
        var (consulta, conteo, _) = await EjecutarListadoAsync(EstadoPartida.Finalizada, Dificultad.Dificil);

        foreach (var sql in new[] { consulta.Texto, conteo.Texto })
        {
            var where = sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..];
            Assert.Contains("p.\"Estado\" = @estado", where);
            Assert.Contains("p.\"Dificultad\" = @dificultad", where);
        }

        Assert.Equal(1, Convert.ToInt32(consulta.Parametros["estado"]));
        Assert.Equal(1, Convert.ToInt32(consulta.Parametros["dificultad"]));
    }

    [Theory]
    [InlineData(false, "ORDER BY p.\"FechaCreacion\" DESC, p.\"Id\" DESC")]
    [InlineData(true, "ORDER BY p.\"FechaCreacion\", p.\"Id\"")]
    public async Task Listar_OrdenaPorFechaDeCreacionConDesempate(bool ascendente, string orden)
    {
        var (consulta, _, _) = await EjecutarListadoAsync(ascendente: ascendente);

        Assert.Contains(orden, consulta.Texto);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(3, 10, 20)]
    [InlineData(2, 50, 50)]
    public async Task Listar_PaginaConLimitYOffset(int page, int pageSize, int saltoEsperado)
    {
        var (consulta, _, _) = await EjecutarListadoAsync(page: page, pageSize: pageSize);

        var limite = Regex.Match(consulta.Texto, "LIMIT @(\\w+) OFFSET @(\\w+)");
        Assert.True(limite.Success, consulta.Texto);
        Assert.Equal(pageSize, consulta.Parametros[limite.Groups[1].Value]);
        Assert.Equal(saltoEsperado, consulta.Parametros[limite.Groups[2].Value]);
    }

    [Fact]
    public async Task Listar_DevuelveElTotalDeLaConsultaDeConteo()
    {
        var (_, _, total) = await EjecutarListadoAsync();

        Assert.Equal(37, total);
    }

    private static async Task<(ComandoDeLectura Consulta, ComandoDeLectura Conteo, int Total)> EjecutarListadoAsync(
        EstadoPartida? estado = null,
        Dificultad? dificultad = null,
        bool ascendente = false,
        int page = 1,
        int pageSize = 10)
    {
        var lecturas = new LecturasInterceptadas();
        await using var context = new MarrowDbContext(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(ConexionInexistente)
            .AddInterceptors(new AperturaSuprimida(), lecturas)
            .Options);

        var (_, total) = await new PartidaRepository(context).ObtenerPorUsuarioAsync(42, estado, dificultad, ascendente, page, pageSize);

        return (Assert.Single(lecturas.Comandos, comando => !EsConteo(comando.Texto)),
            Assert.Single(lecturas.Comandos, comando => EsConteo(comando.Texto)),
            total);
    }

    private static bool EsConteo(string sql) => sql.Contains("COUNT(", StringComparison.OrdinalIgnoreCase);

    private sealed record ComandoDeLectura(string Texto, Dictionary<string, object?> Parametros);

    // Responde sin ir a la base: el conteo devuelve 37 y la consulta, ninguna fila.
    private sealed class LecturasInterceptadas : DbCommandInterceptor
    {
        public List<ComandoDeLectura> Comandos { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Comandos.Add(new ComandoDeLectura(
                command.CommandText,
                command.Parameters.Cast<DbParameter>().ToDictionary(parametro => parametro.ParameterName.TrimStart('@'), parametro => parametro.Value)));

            var tabla = new DataTable();
            if (EsConteo(command.CommandText))
            {
                tabla.Columns.Add("total", typeof(int));
                tabla.Rows.Add(37);
            }

            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(tabla.CreateDataReader()));
        }
    }

    [Fact]
    public async Task EliminarPersonaje_ExigeUsuarioYQueNoTengaPartidasEnLaMismaSentencia()
    {
        var comando = new ComandoInterceptado();
        await using var context = new MarrowDbContext(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(ConexionInexistente)
            .AddInterceptors(new AperturaSuprimida(), comando)
            .Options);

        var filas = await new PersonajeRepository(context).EliminarSinPartidasAsync(7, 42);

        Assert.Equal(1, filas);
        Assert.NotNull(comando.Sql);
        Assert.Contains("DELETE FROM \"Personajes\"", comando.Sql);
        Assert.Contains("\"UsuarioId\" = @usuarioId", comando.Sql);
        Assert.Contains("NOT", comando.Sql);
        Assert.Contains("EXISTS", comando.Sql);
        Assert.Contains("\"Partidas\"", comando.Sql);
    }

    [Theory]
    [InlineData("p.\"Nivel\" > 3", "\"Nivel\"")]
    [InlineData("p.\"Nivel\" < 1", "\"Nivel\"")]
    [InlineData("p.\"Experiencia\" < 0", "\"Experiencia\"")]
    [InlineData("p.\"VidaMaxima\" <= 0", "\"VidaMaxima\"")]
    [InlineData("p.\"Ataque\" <= 0", "\"Ataque\"")]
    [InlineData("p.\"Defensa\" < 0", "\"Defensa\"")]
    public async Task NormalizarPersonajes_SoloCorrigeLosValoresInvalidos(string condicion, string columna)
    {
        var comando = new ComandoInterceptado();
        await using var context = new MarrowDbContext(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(ConexionInexistente)
            .AddInterceptors(new AperturaSuprimida(), comando)
            .Options);

        await InicializadorDatos.NormalizarPersonajesAsync(context, NullLogger.Instance);

        Assert.Equal(6, comando.Todos.Count);
        Assert.All(comando.Todos, sql => Assert.StartsWith("UPDATE \"Personajes\"", sql));
        var sentencia = Assert.Single(comando.Todos, sql => sql.Contains(condicion));
        Assert.Contains($"{columna} =", sentencia);
    }

    private static async Task<string> CapturarSqlDeMoverAsync()
    {
        var comando = new ComandoInterceptado();
        await using var context = new MarrowDbContext(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(ConexionInexistente)
            .AddInterceptors(new AperturaSuprimida(), comando)
            .Options);

        var filas = await new PartidaRepository(context).MoverAsync(17, 42, 1, 0);

        Assert.Equal(1, filas);
        Assert.NotNull(comando.Sql);
        return comando.Sql;
    }

    private sealed class AperturaSuprimida : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());

        public override ValueTask<InterceptionResult> ConnectionClosingAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class ComandoInterceptado : DbCommandInterceptor
    {
        public string? Sql { get; private set; }

        public List<string> Todos { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            Todos.Add(command.CommandText);
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }
}
