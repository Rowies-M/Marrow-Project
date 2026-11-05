using System.Data.Common;
using Marrow.API.Data;
using Marrow.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }
}
