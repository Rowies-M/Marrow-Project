using Marrow.API.Models;

namespace Marrow.API.Services;

/// <summary>Valores de una partida: los del personaje y los del enemigo, ya ajustados por la dificultad.</summary>
public sealed record EstadisticasCombate(
    int VidaJugador,
    int AtaqueJugador,
    int DefensaJugador,
    int VidaEnemigo,
    int AtaqueEnemigo,
    int DefensaEnemigo);

/// <summary>Consecuencias de un ataque. El daño informado es el que realmente se aplicó.</summary>
public sealed record ResolucionAtaque(
    int VidaJugador,
    int VidaEnemigo,
    int DanioInfligido,
    int DanioRecibido,
    ResultadoCombate? Resultado);

public static class ReglasCombate
{
    public const int NivelGolpePoderoso = 2;

    private const int DanioMinimo = 1;
    private const int MultiplicadorGolpePoderoso = 2;

    public static EstadisticasCombate ObtenerEstadisticas(Dificultad dificultad, Personaje personaje, Enemigo enemigo)
    {
        ArgumentNullException.ThrowIfNull(personaje);
        ArgumentNullException.ThrowIfNull(enemigo);

        // Normal usa el catálogo tal cual. Difícil duplica la vida y aumenta el ataque un 50 %, redondeando hacia abajo.
        var (vidaEnemigo, ataqueEnemigo) = dificultad switch
        {
            Dificultad.Normal => (enemigo.VidaMaxima, enemigo.Ataque),
            Dificultad.Dificil => (enemigo.VidaMaxima * 2, enemigo.Ataque * 3 / 2),
            _ => throw new ArgumentOutOfRangeException(nameof(dificultad), dificultad, "La dificultad no es válida.")
        };

        return new EstadisticasCombate(
            personaje.VidaMaxima,
            personaje.Ataque,
            personaje.Defensa,
            vidaEnemigo,
            ataqueEnemigo,
            enemigo.Defensa);
    }

    public static bool EsAdyacente(int jugadorX, int jugadorY, int enemigoX, int enemigoY) =>
        Math.Abs(jugadorX - enemigoX) + Math.Abs(jugadorY - enemigoY) == 1;

    public static bool DesbloqueaDificil(Dificultad dificultad, ResultadoCombate resultado) =>
        dificultad == Dificultad.Normal && resultado == ResultadoCombate.Victoria;

    public static bool DificultadDisponible(Dificultad solicitada, Dificultad desbloqueada) =>
        solicitada == Dificultad.Normal
        || (solicitada == Dificultad.Dificil && desbloqueada == Dificultad.Dificil);

    public static bool GolpePoderosoDesbloqueado(int nivel) => nivel >= NivelGolpePoderoso;

    public static ResolucionAtaque ResolverAtaque(
        EstadisticasCombate estadisticas,
        int vidaJugador,
        int vidaEnemigo,
        bool golpePoderoso = false)
    {
        ArgumentNullException.ThrowIfNull(estadisticas);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaJugador);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vidaEnemigo);

        var ataque = golpePoderoso
            ? estadisticas.AtaqueJugador * MultiplicadorGolpePoderoso
            : estadisticas.AtaqueJugador;
        var danioInfligido = Math.Min(CalcularDanio(ataque, estadisticas.DefensaEnemigo), vidaEnemigo);
        var vidaEnemigoRestante = vidaEnemigo - danioInfligido;

        if (vidaEnemigoRestante == 0)
        {
            return new ResolucionAtaque(vidaJugador, 0, danioInfligido, 0, ResultadoCombate.Victoria);
        }

        var danioRecibido = Math.Min(
            CalcularDanio(estadisticas.AtaqueEnemigo, estadisticas.DefensaJugador),
            vidaJugador);
        var vidaJugadorRestante = vidaJugador - danioRecibido;

        return new ResolucionAtaque(
            vidaJugadorRestante,
            vidaEnemigoRestante,
            danioInfligido,
            danioRecibido,
            vidaJugadorRestante == 0 ? ResultadoCombate.Derrota : null);
    }

    private static int CalcularDanio(int ataque, int defensa) => Math.Max(DanioMinimo, ataque - defensa);
}
