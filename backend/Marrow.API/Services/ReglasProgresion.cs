using Marrow.API.Models;

namespace Marrow.API.Services;

public static class ReglasProgresion
{
    public const int ExperienciaPorVictoria = 100;

    private const int ExperienciaPorNivel = 100;

    public static int ExperienciaParaSubir(int nivel) => checked(ExperienciaPorNivel * nivel);

    /// <summary>Suma la recompensa y sube los niveles que alcancen, conservando el excedente.</summary>
    public static ProgresoPersonaje AplicarVictoria(int nivel, int experiencia)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nivel);
        ArgumentOutOfRangeException.ThrowIfNegative(experiencia);

        var restante = checked(experiencia + ExperienciaPorVictoria);
        while (restante >= ExperienciaParaSubir(nivel))
        {
            restante -= ExperienciaParaSubir(nivel);
            nivel++;
        }

        return new ProgresoPersonaje(nivel, restante);
    }
}
