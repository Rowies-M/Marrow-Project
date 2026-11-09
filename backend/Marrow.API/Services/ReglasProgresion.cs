using Marrow.API.Models;

namespace Marrow.API.Services;

public static class ReglasProgresion
{
    public const int NivelMinimo = 1;
    public const int NivelMaximo = 3;

    private const int ExperienciaPorNivel = 100;

    public static int ExperienciaParaSubir(int nivel) => checked(ExperienciaPorNivel * nivel);

    /// <summary>Nulo en el nivel máximo, porque ya no se puede subir.</summary>
    public static int? ExperienciaSiguienteNivel(int nivel) =>
        nivel >= NivelMaximo ? null : ExperienciaParaSubir(nivel);

    /// <summary>El nivel del enemigo de una partida coincide con el del personaje, dentro de los niveles del juego.</summary>
    public static int NivelDeEnemigo(int nivelPersonaje) => Math.Clamp(nivelPersonaje, NivelMinimo, NivelMaximo);

    /// <summary>
    /// Suma la recompensa y sube los niveles que alcancen, conservando el excedente.
    /// En el nivel máximo la experiencia sigue acumulándose, pero el nivel no cambia.
    /// </summary>
    public static ProgresoPersonaje AplicarVictoria(int nivel, int experiencia, int experienciaGanada)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nivel);
        ArgumentOutOfRangeException.ThrowIfNegative(experiencia);
        ArgumentOutOfRangeException.ThrowIfNegative(experienciaGanada);

        var restante = checked(experiencia + experienciaGanada);
        while (nivel < NivelMaximo && restante >= ExperienciaParaSubir(nivel))
        {
            restante -= ExperienciaParaSubir(nivel);
            nivel++;
        }

        return new ProgresoPersonaje(nivel, restante);
    }
}
