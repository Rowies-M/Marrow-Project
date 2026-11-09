using Marrow.API.Data;
using Marrow.API.Models;
using Marrow.API.Services;

namespace Marrow.API.Tests;

public class ReglasCombateTests
{
    private const int LimiteAtaques = 30;

    private static Personaje CrearGuerrero() => new() { VidaMaxima = 80, Ataque = 15, Defensa = 5 };

    private static Enemigo ObtenerEnemigo(int nivel) => DatosIniciales.Enemigos().Single(enemigo => enemigo.Nivel == nivel);

    private static EstadisticasCombate ObtenerEstadisticas(Dificultad dificultad, int nivelEnemigo) =>
        ReglasCombate.ObtenerEstadisticas(dificultad, CrearGuerrero(), ObtenerEnemigo(nivelEnemigo));

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
    [InlineData(1, "Goblin", 30, 5, 2, 100)]
    [InlineData(2, "Esqueleto", 45, 8, 3, 100)]
    [InlineData(3, "Orco", 80, 12, 5, 100)]
    public void Catalogo_TieneUnEnemigoPorCadaNivel(
        int nivel,
        string nombre,
        int vida,
        int ataque,
        int defensa,
        int experiencia)
    {
        var enemigo = ObtenerEnemigo(nivel);

        Assert.Equal(nombre, enemigo.Nombre);
        Assert.Equal(vida, enemigo.VidaMaxima);
        Assert.Equal(ataque, enemigo.Ataque);
        Assert.Equal(defensa, enemigo.Defensa);
        Assert.Equal(experiencia, enemigo.ExperienciaOtorgada);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 1, 30, 5, 2)]
    [InlineData(Dificultad.Normal, 2, 45, 8, 3)]
    [InlineData(Dificultad.Normal, 3, 80, 12, 5)]
    [InlineData(Dificultad.Dificil, 1, 60, 7, 2)]
    [InlineData(Dificultad.Dificil, 2, 90, 12, 3)]
    [InlineData(Dificultad.Dificil, 3, 160, 18, 5)]
    public void ObtenerEstadisticas_AplicaLaDificultadSobreElCatalogo(
        Dificultad dificultad,
        int nivelEnemigo,
        int vidaEnemigo,
        int ataqueEnemigo,
        int defensaEnemigo)
    {
        var estadisticas = ObtenerEstadisticas(dificultad, nivelEnemigo);

        Assert.Equal(vidaEnemigo, estadisticas.VidaEnemigo);
        Assert.Equal(ataqueEnemigo, estadisticas.AtaqueEnemigo);
        Assert.Equal(defensaEnemigo, estadisticas.DefensaEnemigo);
    }

    [Fact]
    public void ObtenerEstadisticas_UsaLosValoresDelPersonaje()
    {
        var personaje = new Personaje { VidaMaxima = 100, Ataque = 20, Defensa = 7 };

        var estadisticas = ReglasCombate.ObtenerEstadisticas(Dificultad.Normal, personaje, ObtenerEnemigo(1));

        Assert.Equal(100, estadisticas.VidaJugador);
        Assert.Equal(20, estadisticas.AtaqueJugador);
        Assert.Equal(7, estadisticas.DefensaJugador);
    }

    [Fact]
    public void ObtenerEstadisticas_DificultadInvalida_LanzaError()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReglasCombate.ObtenerEstadisticas((Dificultad)99, CrearGuerrero(), ObtenerEnemigo(1)));
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
    [InlineData(1, 79, 17, 13, 1)]
    [InlineData(2, 77, 33, 12, 3)]
    [InlineData(3, 73, 70, 10, 7)]
    public void ResolverAtaque_EnemigoSobrevive_RestaLaDefensaAlDanioYAlContraataque(
        int nivelEnemigo,
        int vidaJugadorEsperada,
        int vidaEnemigoEsperada,
        int danioInfligidoEsperado,
        int danioRecibidoEsperado)
    {
        var estadisticas = ObtenerEstadisticas(Dificultad.Normal, nivelEnemigo);

        var resolucion = ReglasCombate.ResolverAtaque(estadisticas, estadisticas.VidaJugador, estadisticas.VidaEnemigo);

        Assert.Equal(vidaJugadorEsperada, resolucion.VidaJugador);
        Assert.Equal(vidaEnemigoEsperada, resolucion.VidaEnemigo);
        Assert.Equal(danioInfligidoEsperado, resolucion.DanioInfligido);
        Assert.Equal(danioRecibidoEsperado, resolucion.DanioRecibido);
        Assert.Null(resolucion.Resultado);
    }

    [Fact]
    public void ResolverAtaque_LaDefensaSuperaAlAtaque_AplicaElDanioMinimo()
    {
        var estadisticas = new EstadisticasCombate(80, 1, 50, 40, 1, 50);

        var resolucion = ReglasCombate.ResolverAtaque(estadisticas, 80, 40);

        Assert.Equal(1, resolucion.DanioInfligido);
        Assert.Equal(1, resolucion.DanioRecibido);
    }

    [Theory]
    [InlineData(1, 28)]
    [InlineData(2, 27)]
    [InlineData(3, 25)]
    public void ResolverAtaque_GolpePoderoso_DuplicaElAtaqueAntesDeRestarLaDefensa(int nivelEnemigo, int danioEsperado)
    {
        var estadisticas = ObtenerEstadisticas(Dificultad.Dificil, nivelEnemigo);

        var resolucion = ReglasCombate.ResolverAtaque(
            estadisticas,
            estadisticas.VidaJugador,
            estadisticas.VidaEnemigo,
            golpePoderoso: true);

        Assert.Equal(danioEsperado, resolucion.DanioInfligido);
    }

    [Fact]
    public void ResolverAtaque_GolpePoderoso_NoAumentaElContraataque()
    {
        var estadisticas = ObtenerEstadisticas(Dificultad.Dificil, 3);

        var basico = ReglasCombate.ResolverAtaque(estadisticas, 80, 160);
        var poderoso = ReglasCombate.ResolverAtaque(estadisticas, 80, 160, golpePoderoso: true);

        Assert.Equal(13, basico.DanioRecibido);
        Assert.Equal(basico.DanioRecibido, poderoso.DanioRecibido);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void GolpePoderosoDesbloqueado_DesdeElNivelDos(int nivel, bool esperado)
    {
        Assert.Equal(esperado, ReglasCombate.GolpePoderosoDesbloqueado(nivel));
    }

    [Theory]
    [InlineData(13, 13)]
    [InlineData(5, 5)]
    [InlineData(1, 1)]
    public void ResolverAtaque_EnemigoMuere_EsVictoriaSinContraataqueNiVidaNegativa(
        int vidaEnemigo,
        int danioInfligidoEsperado)
    {
        var resolucion = ReglasCombate.ResolverAtaque(ObtenerEstadisticas(Dificultad.Normal, 1), 60, vidaEnemigo);

        Assert.Equal(0, resolucion.VidaEnemigo);
        Assert.Equal(60, resolucion.VidaJugador);
        Assert.Equal(danioInfligidoEsperado, resolucion.DanioInfligido);
        Assert.Equal(0, resolucion.DanioRecibido);
        Assert.Equal(ResultadoCombate.Victoria, resolucion.Resultado);
    }

    [Theory]
    [InlineData(13, 13)]
    [InlineData(5, 5)]
    [InlineData(1, 1)]
    public void ResolverAtaque_ContraataqueMataAlJugador_EsDerrotaSinVidaNegativa(
        int vidaJugador,
        int danioRecibidoEsperado)
    {
        var resolucion = ReglasCombate.ResolverAtaque(ObtenerEstadisticas(Dificultad.Dificil, 3), vidaJugador, 160);

        Assert.Equal(0, resolucion.VidaJugador);
        Assert.Equal(150, resolucion.VidaEnemigo);
        Assert.Equal(10, resolucion.DanioInfligido);
        Assert.Equal(danioRecibidoEsperado, resolucion.DanioRecibido);
        Assert.Equal(ResultadoCombate.Derrota, resolucion.Resultado);
    }

    [Fact]
    public void ResolverAtaque_JugadorSobreviveConUnPuntoDeVida_NoTerminaElCombate()
    {
        var resolucion = ReglasCombate.ResolverAtaque(ObtenerEstadisticas(Dificultad.Dificil, 3), 14, 160);

        Assert.Equal(1, resolucion.VidaJugador);
        Assert.Null(resolucion.Resultado);
    }

    [Fact]
    public void ResolverAtaque_AmbosConPocaVida_GanaElJugadorPorqueAtacaPrimero()
    {
        var resolucion = ReglasCombate.ResolverAtaque(ObtenerEstadisticas(Dificultad.Dificil, 3), 1, 10);

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
            () => ReglasCombate.ResolverAtaque(ObtenerEstadisticas(Dificultad.Normal, 1), vidaJugador, vidaEnemigo));
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
    [InlineData(Dificultad.Normal, Dificultad.Normal, true)]
    [InlineData(Dificultad.Normal, Dificultad.Dificil, true)]
    [InlineData(Dificultad.Dificil, Dificultad.Normal, false)]
    [InlineData(Dificultad.Dificil, Dificultad.Dificil, true)]
    [InlineData((Dificultad)99, Dificultad.Dificil, false)]
    public void DificultadDisponible_DificilRequiereElDesbloqueo(
        Dificultad solicitada,
        Dificultad desbloqueada,
        bool esperado)
    {
        Assert.Equal(esperado, ReglasCombate.DificultadDisponible(solicitada, desbloqueada));
    }

    [Theory]
    [InlineData(Dificultad.Normal, 1, false, 3, 78, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Normal, 2, false, 4, 71, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Normal, 3, false, 8, 31, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Dificil, 1, false, 5, 72, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Dificil, 2, false, 8, 31, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Dificil, 3, false, 7, 0, ResultadoCombate.Derrota)]
    [InlineData(Dificultad.Dificil, 2, true, 4, 59, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Dificil, 3, true, 7, 2, ResultadoCombate.Victoria)]
    public void CombateCompleto_ConElCatalogo_TerminaConLosValoresEsperados(
        Dificultad dificultad,
        int nivelEnemigo,
        bool golpePoderoso,
        int ataquesEsperados,
        int vidaJugadorFinal,
        ResultadoCombate resultadoEsperado)
    {
        var estadisticas = ObtenerEstadisticas(dificultad, nivelEnemigo);
        var vidaJugador = estadisticas.VidaJugador;
        var vidaEnemigo = estadisticas.VidaEnemigo;
        var ataques = 0;
        ResultadoCombate? resultado = null;

        while (resultado is null)
        {
            Assert.True(ataques < LimiteAtaques, $"El combate no terminó en {LimiteAtaques} ataques.");

            var resolucion = ReglasCombate.ResolverAtaque(estadisticas, vidaJugador, vidaEnemigo, golpePoderoso);
            vidaJugador = resolucion.VidaJugador;
            vidaEnemigo = resolucion.VidaEnemigo;
            resultado = resolucion.Resultado;
            ataques++;
        }

        Assert.Equal(ataquesEsperados, ataques);
        Assert.Equal(vidaJugadorFinal, vidaJugador);
        Assert.Equal(resultadoEsperado, resultado);
    }
}
