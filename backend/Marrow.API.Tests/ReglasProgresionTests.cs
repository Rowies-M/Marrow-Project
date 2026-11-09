using Marrow.API.Models;
using Marrow.API.Services;

namespace Marrow.API.Tests;

public class ReglasProgresionTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    public void ExperienciaParaSubir_EsCienPorElNivelActual(int nivel, int esperada)
    {
        Assert.Equal(esperada, ReglasProgresion.ExperienciaParaSubir(nivel));
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, null)]
    [InlineData(4, null)]
    public void ExperienciaSiguienteNivel_EsNulaEnElNivelMaximo(int nivel, int? esperada)
    {
        Assert.Equal(esperada, ReglasProgresion.ExperienciaSiguienteNivel(nivel));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 3)]
    public void NivelDeEnemigo_SeLimitaALosNivelesDelJuego(int nivelPersonaje, int esperado)
    {
        Assert.Equal(esperado, ReglasProgresion.NivelDeEnemigo(nivelPersonaje));
    }

    [Fact]
    public void AplicarVictoria_SinAlcanzarElMinimo_SoloSumaExperiencia()
    {
        Assert.Equal(new ProgresoPersonaje(2, 100), ReglasProgresion.AplicarVictoria(2, 0, 100));
    }

    [Theory]
    [InlineData(1, 0, 2, 0)]
    [InlineData(2, 100, 3, 0)]
    public void AplicarVictoria_AlcanzandoElMinimoExacto_SubeDeNivelSinExcedente(
        int nivel,
        int experiencia,
        int nivelEsperado,
        int experienciaEsperada)
    {
        Assert.Equal(new ProgresoPersonaje(nivelEsperado, experienciaEsperada), ReglasProgresion.AplicarVictoria(nivel, experiencia, 100));
    }

    [Theory]
    [InlineData(1, 40, 100, 2, 40)]
    [InlineData(2, 150, 100, 3, 50)]
    public void AplicarVictoria_ConservaLaExperienciaSobrante(
        int nivel,
        int experiencia,
        int ganada,
        int nivelEsperado,
        int experienciaEsperada)
    {
        Assert.Equal(new ProgresoPersonaje(nivelEsperado, experienciaEsperada), ReglasProgresion.AplicarVictoria(nivel, experiencia, ganada));
    }

    [Fact]
    public void AplicarVictoria_ProcesaMasDeUnaSubidaDeNivelHastaElMaximo()
    {
        Assert.Equal(new ProgresoPersonaje(3, 150), ReglasProgresion.AplicarVictoria(1, 440, 10));
    }

    [Theory]
    [InlineData(3, 0, 100, 3, 100)]
    [InlineData(3, 290, 100, 3, 390)]
    [InlineData(3, 5000, 100, 3, 5100)]
    public void AplicarVictoria_EnElNivelMaximo_AcumulaExperienciaSinSubir(
        int nivel,
        int experiencia,
        int ganada,
        int nivelEsperado,
        int experienciaEsperada)
    {
        Assert.Equal(new ProgresoPersonaje(nivelEsperado, experienciaEsperada), ReglasProgresion.AplicarVictoria(nivel, experiencia, ganada));
    }

    [Fact]
    public void AplicarVictoria_PersonajeSobreElMaximo_ConservaSuNivel()
    {
        Assert.Equal(new ProgresoPersonaje(4, 45), ReglasProgresion.AplicarVictoria(4, 10, 35));
    }

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(-1, 0, 10)]
    [InlineData(1, -1, 10)]
    [InlineData(1, 0, -1)]
    public void AplicarVictoria_RechazaValoresInvalidos(int nivel, int experiencia, int ganada)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasProgresion.AplicarVictoria(nivel, experiencia, ganada));
    }
}
