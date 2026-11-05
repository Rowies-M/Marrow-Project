using Marrow.API.Models;

namespace Marrow.API.Services;

/// <summary>Estadísticas de combate de una dificultad.</summary>
public sealed record EstadisticasCombate(
    int VidaJugador,
    int DanioJugador,
    int VidaEnemigo,
    int DanioEnemigo);

/// <summary>Consecuencias de un ataque. El daño informado es el que realmente se aplicó.</summary>
public sealed record ResolucionAtaque(
    int VidaJugador,
    int VidaEnemigo,
    int DanioInfligido,
    int DanioRecibido,
    ResultadoCombate? Resultado);

public static class ReglasCombate
{
    private static readonly EstadisticasCombate EstadisticasNormal = new(80, 15, 40, 10);
    private static readonly EstadisticasCombate EstadisticasDificil = new(80, 15, 80, 15);

    public static EstadisticasCombate ObtenerEstadisticas(Dificultad dificultad) => dificultad switch
    {
        Dificultad.Normal => EstadisticasNormal,
        Dificultad.Dificil => EstadisticasDificil,
        _ => throw new ArgumentOutOfRangeException(nameof(dificultad), dificultad, "La dificultad no es válida.")
    };

    public static bool EsAdyacente(int jugadorX, int jugadorY, int enemigoX, int enemigoY) =>
        Math.Abs(jugadorX - enemigoX) + Math.Abs(jugadorY - enemigoY) == 1;

    public static bool DesbloqueaDificil(Dificultad dificultad, ResultadoCombate resultado) =>
        dificultad == Dificultad.Normal && resultado == ResultadoCombate.Victoria;

    public static ResolucionAtaque ResolverAtaque(Dificultad dificultad, int vidaJugador, int vidaEnemigo)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaJugador);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaEnemigo);

        var estadisticas = ObtenerEstadisticas(dificultad);
        var danioInfligido = Math.Min(estadisticas.DanioJugador, vidaEnemigo);
        var vidaEnemigoRestante = vidaEnemigo - danioInfligido;

        if (vidaEnemigoRestante == 0)
        {
            return new ResolucionAtaque(vidaJugador, 0, danioInfligido, 0, ResultadoCombate.Victoria);
        }

        var danioRecibido = Math.Min(estadisticas.DanioEnemigo, vidaJugador);
        var vidaJugadorRestante = vidaJugador - danioRecibido;

        return new ResolucionAtaque(
            vidaJugadorRestante,
            vidaEnemigoRestante,
            danioInfligido,
            danioRecibido,
            vidaJugadorRestante == 0 ? ResultadoCombate.Derrota : null);
    }
}
