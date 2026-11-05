using Marrow.API.Models;
using Marrow.API.Services;

namespace Marrow.API.Tests;

public class ReglasCombateTests
{
    private const int LimiteAtaques = 20;

    [Theory]
    [InlineData(Dificultad.Normal, 0)]
    [InlineData(Dificultad.Dificil, 1)]
    public void Dificultad_ConservaSuValorNumerico(Dificultad dificultad, int valorEsperado)
    {
        Assert.Equal(valorEsperado, (int)dificultad);
    }

    [Theory]
    [InlineData(ResultadoCombate.Victoria, 1)]
    [InlineData(ResultadoCombate.Derrota, 2)]
    public void ResultadoCombate_ConservaSuValorNumerico(ResultadoCombate resultado, int valorEsperado)
    {
        Assert.Equal(valorEsperado, (int)resultado);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 80, 15, 40, 10)]
    [InlineData(Dificultad.Dificil, 80, 15, 80, 15)]
    public void ObtenerEstadisticas_DevuelveLosValoresAcordados(
        Dificultad dificultad,
        int vidaJugador,
        int danioJugador,
        int vidaEnemigo,
        int danioEnemigo)
    {
        var estadisticas = ReglasCombate.ObtenerEstadisticas(dificultad);

        Assert.Equal(vidaJugador, estadisticas.VidaJugador);
        Assert.Equal(danioJugador, estadisticas.DanioJugador);
        Assert.Equal(vidaEnemigo, estadisticas.VidaEnemigo);
        Assert.Equal(danioEnemigo, estadisticas.DanioEnemigo);
    }

    [Fact]
    public void ObtenerEstadisticas_DificultadInvalida_LanzaError()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasCombate.ObtenerEstadisticas((Dificultad)99));
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(5, 4)]
    [InlineData(4, 3)]
    [InlineData(6, 3)]
    public void EsAdyacente_CasillaOrtogonal_EsVerdadero(int jugadorX, int jugadorY)
    {
        Assert.True(ReglasCombate.EsAdyacente(jugadorX, jugadorY, 5, 3));
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(6, 2)]
    [InlineData(4, 4)]
    [InlineData(6, 4)]
    public void EsAdyacente_Diagonal_EsFalso(int jugadorX, int jugadorY)
    {
        Assert.False(ReglasCombate.EsAdyacente(jugadorX, jugadorY, 5, 3));
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(5, 1)]
    [InlineData(5, 5)]
    [InlineData(3, 3)]
    [InlineData(7, 3)]
    [InlineData(0, 0)]
    [InlineData(10, 6)]
    public void EsAdyacente_MismaCasillaOFueraDeAlcance_EsFalso(int jugadorX, int jugadorY)
    {
        Assert.False(ReglasCombate.EsAdyacente(jugadorX, jugadorY, 5, 3));
    }

    [Theory]
    [InlineData(Dificultad.Normal, 70, 25, 10)]
    [InlineData(Dificultad.Dificil, 65, 65, 15)]
    public void ResolverAtaque_EnemigoSobrevive_AplicaDanioYContraataque(
        Dificultad dificultad,
        int vidaJugadorEsperada,
        int vidaEnemigoEsperada,
        int danioRecibidoEsperado)
    {
        var estadisticas = ReglasCombate.ObtenerEstadisticas(dificultad);

        var resolucion = ReglasCombate.ResolverAtaque(dificultad, estadisticas.VidaJugador, estadisticas.VidaEnemigo);

        Assert.Equal(vidaJugadorEsperada, resolucion.VidaJugador);
        Assert.Equal(vidaEnemigoEsperada, resolucion.VidaEnemigo);
        Assert.Equal(15, resolucion.DanioInfligido);
        Assert.Equal(danioRecibidoEsperado, resolucion.DanioRecibido);
        Assert.Null(resolucion.Resultado);
    }

    [Fact]
    public void ResolverAtaque_EnemigoSobreviveConUnPuntoDeVida_ContraatacaSinTerminarElCombate()
    {
        var resolucion = ReglasCombate.ResolverAtaque(Dificultad.Normal, 80, 16);

        Assert.Equal(1, resolucion.VidaEnemigo);
        Assert.Equal(70, resolucion.VidaJugador);
        Assert.Equal(15, resolucion.DanioInfligido);
        Assert.Equal(10, resolucion.DanioRecibido);
        Assert.Null(resolucion.Resultado);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 15, 15)]
    [InlineData(Dificultad.Dificil, 15, 15)]
    [InlineData(Dificultad.Normal, 10, 10)]
    [InlineData(Dificultad.Dificil, 5, 5)]
    [InlineData(Dificultad.Normal, 1, 1)]
    public void ResolverAtaque_EnemigoMuere_EsVictoriaSinContraataqueNiVidaNegativa(
        Dificultad dificultad,
        int vidaEnemigo,
        int danioInfligidoEsperado)
    {
        var resolucion = ReglasCombate.ResolverAtaque(dificultad, 60, vidaEnemigo);

        Assert.Equal(0, resolucion.VidaEnemigo);
        Assert.Equal(60, resolucion.VidaJugador);
        Assert.Equal(danioInfligidoEsperado, resolucion.DanioInfligido);
        Assert.Equal(0, resolucion.DanioRecibido);
        Assert.Equal(ResultadoCombate.Victoria, resolucion.Resultado);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 10, 10)]
    [InlineData(Dificultad.Dificil, 15, 15)]
    [InlineData(Dificultad.Normal, 5, 5)]
    [InlineData(Dificultad.Dificil, 1, 1)]
    public void ResolverAtaque_ContraataqueMataAlJugador_EsDerrotaSinVidaNegativa(
        Dificultad dificultad,
        int vidaJugador,
        int danioRecibidoEsperado)
    {
        var resolucion = ReglasCombate.ResolverAtaque(dificultad, vidaJugador, 40);

        Assert.Equal(0, resolucion.VidaJugador);
        Assert.Equal(25, resolucion.VidaEnemigo);
        Assert.Equal(15, resolucion.DanioInfligido);
        Assert.Equal(danioRecibidoEsperado, resolucion.DanioRecibido);
        Assert.Equal(ResultadoCombate.Derrota, resolucion.Resultado);
    }

    [Fact]
    public void ResolverAtaque_JugadorSobreviveConUnPuntoDeVida_NoTerminaElCombate()
    {
        var resolucion = ReglasCombate.ResolverAtaque(Dificultad.Normal, 11, 40);

        Assert.Equal(1, resolucion.VidaJugador);
        Assert.Null(resolucion.Resultado);
    }

    [Fact]
    public void ResolverAtaque_AmbosConPocaVida_GanaElJugadorPorqueAtacaPrimero()
    {
        var resolucion = ReglasCombate.ResolverAtaque(Dificultad.Dificil, 1, 15);

        Assert.Equal(1, resolucion.VidaJugador);
        Assert.Equal(0, resolucion.VidaEnemigo);
        Assert.Equal(ResultadoCombate.Victoria, resolucion.Resultado);
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(-1, 40)]
    [InlineData(80, 0)]
    [InlineData(80, -1)]
    public void ResolverAtaque_ConVidaAgotada_LanzaError(int vidaJugador, int vidaEnemigo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReglasCombate.ResolverAtaque(Dificultad.Normal, vidaJugador, vidaEnemigo));
    }

    [Fact]
    public void ResolverAtaque_DificultadInvalida_LanzaError()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReglasCombate.ResolverAtaque((Dificultad)99, 80, 40));
    }

    [Theory]
    [InlineData(Dificultad.Normal, ResultadoCombate.Victoria, true)]
    [InlineData(Dificultad.Normal, ResultadoCombate.Derrota, false)]
    [InlineData(Dificultad.Dificil, ResultadoCombate.Victoria, false)]
    [InlineData(Dificultad.Dificil, ResultadoCombate.Derrota, false)]
    [InlineData((Dificultad)99, ResultadoCombate.Victoria, false)]
    [InlineData(Dificultad.Normal, (ResultadoCombate)99, false)]
    [InlineData(Dificultad.Normal, (ResultadoCombate)0, false)]
    public void DesbloqueaDificil_SoloConVictoriaEnNormal(
        Dificultad dificultad,
        ResultadoCombate resultado,
        bool esperado)
    {
        Assert.Equal(esperado, ReglasCombate.DesbloqueaDificil(dificultad, resultado));
    }

    [Theory]
    [InlineData(Dificultad.Normal, 3, 60)]
    [InlineData(Dificultad.Dificil, 6, 5)]
    public void CombateCompleto_DesdeLaVidaInicial_TerminaEnVictoriaConLosValoresEsperados(
        Dificultad dificultad,
        int ataquesEsperados,
        int vidaJugadorFinal)
    {
        var estadisticas = ReglasCombate.ObtenerEstadisticas(dificultad);
        var vidaJugador = estadisticas.VidaJugador;
        var vidaEnemigo = estadisticas.VidaEnemigo;
        var ataques = 0;
        ResultadoCombate? resultado = null;

        while (resultado is null)
        {
            Assert.True(ataques < LimiteAtaques, $"El combate no terminó en {LimiteAtaques} ataques.");

            var resolucion = ReglasCombate.ResolverAtaque(dificultad, vidaJugador, vidaEnemigo);
            vidaJugador = resolucion.VidaJugador;
            vidaEnemigo = resolucion.VidaEnemigo;
            resultado = resolucion.Resultado;
            ataques++;
        }

        Assert.Equal(ataquesEsperados, ataques);
        Assert.Equal(vidaJugadorFinal, vidaJugador);
        Assert.Equal(0, vidaEnemigo);
        Assert.Equal(ResultadoCombate.Victoria, resultado);
    }
}
