using Marrow.API.Models;
using Marrow.API.Services;

namespace Marrow.API.Tests;

public class ReglasProgresionTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(7, 700)]
    public void ExperienciaParaSubir_EsCienPorElNivelActual(int nivel, int esperada)
    {
        Assert.Equal(esperada, ReglasProgresion.ExperienciaParaSubir(nivel));
    }

    [Fact]
    public void AplicarVictoria_SinAlcanzarElMinimo_SoloSumaExperiencia()
    {
        Assert.Equal(new ProgresoPersonaje(3, 150), ReglasProgresion.AplicarVictoria(3, 50));
    }

    [Fact]
    public void AplicarVictoria_AlcanzandoElMinimoExacto_SubeDeNivelSinExcedente()
    {
        Assert.Equal(new ProgresoPersonaje(2, 0), ReglasProgresion.AplicarVictoria(1, 0));
    }

    [Theory]
    [InlineData(1, 40, 2, 40)]
    [InlineData(2, 150, 3, 50)]
    public void AplicarVictoria_ConservaLaExperienciaSobrante(int nivel, int experiencia, int nivelEsperado, int experienciaEsperada)
    {
        Assert.Equal(new ProgresoPersonaje(nivelEsperado, experienciaEsperada), ReglasProgresion.AplicarVictoria(nivel, experiencia));
    }

    [Theory]
    [InlineData(1, 200, 3, 0)]
    [InlineData(1, 350, 3, 150)]
    [InlineData(2, 700, 4, 300)]
    public void AplicarVictoria_ProcesaMasDeUnaSubidaDeNivel(int nivel, int experiencia, int nivelEsperado, int experienciaEsperada)
    {
        Assert.Equal(new ProgresoPersonaje(nivelEsperado, experienciaEsperada), ReglasProgresion.AplicarVictoria(nivel, experiencia));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    public void AplicarVictoria_RechazaValoresInvalidos(int nivel, int experiencia)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasProgresion.AplicarVictoria(nivel, experiencia));
    }
}
