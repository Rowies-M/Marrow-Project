using System.Security.Claims;
using System.Text.Json;
using Marrow.API.Controllers;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Marrow.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Tests;

public class PartidaServiceTests
{
    private const int PartidaId = 17;
    private const int UsuarioId = 42;
    private const int OtroUsuarioId = 99;

    private static readonly MoverPartidaRequest MoverDerecha = new() { Direccion = "derecha" };

    [Fact]
    public async Task Finalizar_PrimeraVez_NoInformaConflicto()
    {
        var repositorio = new PartidaRepositoryFalso(CrearPartida());
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.FinalizarAsync(UsuarioId, PartidaId);

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Equal(EstadoPartida.Finalizada, resultado.Partida.Estado);
    }

    [Fact]
    public async Task Finalizar_Repetida_InformaConflicto()
    {
        var repositorio = new PartidaRepositoryFalso(CrearPartida());
        var servicio = CrearServicio(repositorio);
        await servicio.FinalizarAsync(UsuarioId, PartidaId);

        var resultado = await servicio.FinalizarAsync(UsuarioId, PartidaId);

        Assert.NotNull(resultado);
        Assert.True(resultado.ConcurrenciaPerdida);
    }

    [Fact]
    public async Task Finalizar_PartidaInexistente_DevuelveNull()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(null));

        Assert.Null(await servicio.FinalizarAsync(UsuarioId, PartidaId));
    }

    [Fact]
    public async Task Finalizar_PartidaAjena_DevuelveNullYNoLaModifica()
    {
        var partida = CrearPartida();
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        Assert.Null(await servicio.FinalizarAsync(OtroUsuarioId, PartidaId));
        Assert.Equal(EstadoPartida.Activa, partida.Estado);
    }

    [Fact]
    public async Task Mover_PartidaActiva_ActualizaLaPosicion()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(CrearPartida()));

        var resultado = await servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha);

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Equal(2, resultado.Estado.PersonajeX);
        Assert.Equal(1, resultado.Estado.PersonajeY);
    }

    [Theory]
    [InlineData("arriba", 1, 0)]
    [InlineData("abajo", 1, 2)]
    [InlineData("izquierda", 0, 1)]
    [InlineData("derecha", 2, 1)]
    public async Task Mover_CadaDireccion_CambiaSoloLaPosicionDelPersonaje(
        string direccion,
        int xEsperada,
        int yEsperada)
    {
        var partida = CrearPartida();
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var resultado = await servicio.MoverAsync(
            UsuarioId,
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion });

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Equal(xEsperada, resultado.Estado.PersonajeX);
        Assert.Equal(yEsperada, resultado.Estado.PersonajeY);
        Assert.Equal(EstadoPartida.Activa, resultado.Estado.Estado);
        Assert.Equal(84, resultado.Estado.VidaPersonajeActual);
        Assert.Equal(30, resultado.Estado.VidaEnemigoActual);
        Assert.Equal(5, resultado.Estado.EnemigoX);
        Assert.Equal(3, resultado.Estado.EnemigoY);
    }

    [Fact]
    public async Task Mover_ExitosoSinLecturaPosterior_LanzaErrorDeConsistencia()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(CrearPartida()) { LecturaDevuelveNull = true });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha));
    }

    [Fact]
    public async Task Finalizar_ExitosoSinLecturaPosterior_LanzaErrorDeConsistencia()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(CrearPartida()) { LecturaDevuelveNull = true });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.FinalizarAsync(UsuarioId, PartidaId));
    }

    [Fact]
    public async Task Mover_PartidaFinalizada_InformaConflictoYNoCambiaLaPosicion()
    {
        var partida = CrearPartida();
        partida.Estado = EstadoPartida.Finalizada;
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var resultado = await servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha);

        Assert.NotNull(resultado);
        Assert.True(resultado.ConcurrenciaPerdida);
        Assert.Equal(1, partida.PersonajeX);
    }

    [Fact]
    public async Task Mover_GanaLaCarreraContraFinalizar_NoInformaConflicto()
    {
        var partida = CrearPartida();
        var repositorio = new PartidaRepositoryFalso(partida)
        {
            DespuesDeMover = () => partida.Estado = EstadoPartida.Finalizada
        };
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha);

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Equal(EstadoPartida.Finalizada, resultado.Estado.Estado);
        Assert.Equal(2, resultado.Estado.PersonajeX);
    }

    [Fact]
    public async Task Mover_PartidaInexistente_DevuelveNull()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(null));

        Assert.Null(await servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha));
    }

    [Fact]
    public async Task Mover_PartidaAjena_DevuelveNullYNoLaModifica()
    {
        var partida = CrearPartida();
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        Assert.Null(await servicio.MoverAsync(OtroUsuarioId, PartidaId, MoverDerecha));
        Assert.Equal(1, partida.PersonajeX);
    }

    [Fact]
    public async Task EndpointFinalizar_TraduceCadaCasoAlCodigoHttp()
    {
        var controlador = CrearControlador(new PartidaRepositoryFalso(CrearPartida()));

        Assert.IsType<OkObjectResult>(await controlador.Finalizar(PartidaId, CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controlador.Finalizar(PartidaId, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controlador.Finalizar(PartidaId + 1, CancellationToken.None));
    }

    [Fact]
    public async Task EndpointMover_PartidaFinalizada_Devuelve409()
    {
        var partida = CrearPartida();
        partida.Estado = EstadoPartida.Finalizada;
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(PartidaId, MoverDerecha, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(respuesta);
    }

    [Fact]
    public async Task EndpointMover_GanaLaCarreraContraFinalizar_Devuelve200()
    {
        var partida = CrearPartida();
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida)
        {
            DespuesDeMover = () => partida.Estado = EstadoPartida.Finalizada
        });

        var respuesta = await controlador.Mover(PartidaId, MoverDerecha, CancellationToken.None);

        Assert.IsType<OkObjectResult>(respuesta);
    }

    [Fact]
    public async Task EndpointMover_PartidaInexistente_Devuelve404()
    {
        var controlador = CrearControlador(new PartidaRepositoryFalso(null));

        var respuesta = await controlador.Mover(PartidaId, MoverDerecha, CancellationToken.None);

        Assert.IsType<NotFoundResult>(respuesta);
    }

    [Theory]
    [InlineData(Dificultad.Normal)]
    [InlineData(Dificultad.Dificil)]
    public async Task Crear_CopiaLaDificultadDelPersonajeSinResultadoDeCombate(Dificultad dificultad)
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var personajes = new PersonajeRepositoryFalso(new Personaje
        {
            Id = 3,
            UsuarioId = UsuarioId,
            DificultadDesbloqueada = dificultad
        });
        var servicio = new PartidaService(repositorio, personajes, new EnemigoRepositoryFalso());

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(dificultad, repositorio.Creada.Dificultad);
        Assert.Null(repositorio.Creada.ResultadoCombate);
        Assert.Equal(EstadoPartida.Activa, repositorio.Creada.Estado);
    }

    [Fact]
    public async Task Crear_DificultadDeLaPartidaNoCambiaSiElPersonajeDesbloqueaOtra()
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var personaje = new Personaje
        {
            Id = 3,
            UsuarioId = UsuarioId,
            DificultadDesbloqueada = Dificultad.Normal
        };
        var servicio = new PartidaService(
            repositorio,
            new PersonajeRepositoryFalso(personaje),
            new EnemigoRepositoryFalso());

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });
        personaje.DificultadDesbloqueada = Dificultad.Dificil;

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(Dificultad.Normal, repositorio.Creada.Dificultad);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 100, 100, 40)]
    [InlineData(Dificultad.Normal, 1, 500, 40)]
    [InlineData(Dificultad.Dificil, 100, 100, 80)]
    [InlineData(Dificultad.Dificil, 1, 500, 80)]
    public async Task Crear_InicializaLasVidasSinUsarLasEstadisticasDelPersonaje(
        Dificultad dificultad,
        int vidaActualPersonaje,
        int vidaMaximaPersonaje,
        int vidaEnemigoEsperada)
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var servicio = new PartidaService(
            repositorio,
            new PersonajeRepositoryFalso(new Personaje
            {
                Id = 3,
                UsuarioId = UsuarioId,
                VidaActual = vidaActualPersonaje,
                VidaMaxima = vidaMaximaPersonaje,
                Ataque = 10,
                DificultadDesbloqueada = dificultad
            }),
            new EnemigoRepositoryFalso());

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(80, repositorio.Creada.VidaPersonajeActual);
        Assert.Equal(vidaEnemigoEsperada, repositorio.Creada.VidaEnemigoActual);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 30, 40)]
    [InlineData(Dificultad.Normal, 999, 40)]
    [InlineData(Dificultad.Dificil, 30, 80)]
    [InlineData(Dificultad.Dificil, 999, 80)]
    public async Task Crear_NoUsaLasEstadisticasDelGoblinPeroConservaSuIdentidad(
        Dificultad dificultad,
        int vidaGoblin,
        int vidaEnemigoEsperada)
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var servicio = new PartidaService(
            repositorio,
            new PersonajeRepositoryFalso(new Personaje
            {
                Id = 3,
                UsuarioId = UsuarioId,
                DificultadDesbloqueada = dificultad
            }),
            new EnemigoRepositoryFalso(vidaGoblin));

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(vidaEnemigoEsperada, repositorio.Creada.VidaEnemigoActual);
        Assert.Equal(1, repositorio.Creada.EnemigoId);
    }

    [Fact]
    public async Task Crear_NoModificaAlPersonaje()
    {
        var personaje = new Personaje
        {
            Id = 3,
            UsuarioId = UsuarioId,
            Nivel = 2,
            Experiencia = 30,
            VidaActual = 100,
            VidaMaxima = 110,
            Ataque = 10,
            Defensa = 5,
            DificultadDesbloqueada = Dificultad.Dificil
        };
        var servicio = new PartidaService(
            new PartidaRepositoryFalso(null),
            new PersonajeRepositoryFalso(personaje),
            new EnemigoRepositoryFalso());

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });

        Assert.Equal(2, personaje.Nivel);
        Assert.Equal(30, personaje.Experiencia);
        Assert.Equal(100, personaje.VidaActual);
        Assert.Equal(110, personaje.VidaMaxima);
        Assert.Equal(10, personaje.Ataque);
        Assert.Equal(5, personaje.Defensa);
        Assert.Equal(Dificultad.Dificil, personaje.DificultadDesbloqueada);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 80, 40)]
    [InlineData(Dificultad.Dificil, 80, 80)]
    public async Task ObtenerEstado_InformaLosMaximosSegunLaDificultadDeLaPartida(
        Dificultad dificultad,
        int vidaMaximaJugador,
        int vidaMaximaEnemigo)
    {
        var partida = CrearPartida();
        partida.Dificultad = dificultad;
        partida.Personaje.VidaMaxima = 500;
        partida.Enemigo.VidaMaxima = 30;
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var estado = await servicio.ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(estado);
        Assert.Equal(vidaMaximaJugador, estado.VidaPersonajeMaxima);
        Assert.Equal(vidaMaximaEnemigo, estado.VidaEnemigoMaxima);
    }

    [Fact]
    public async Task ObtenerEstado_UsaLaDificultadDeLaPartidaAunqueElPersonajeHayaDesbloqueadoOtra()
    {
        var partida = CrearPartida();
        partida.Dificultad = Dificultad.Normal;
        partida.Personaje.DificultadDesbloqueada = Dificultad.Dificil;
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var estado = await servicio.ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(estado);
        Assert.Equal(Dificultad.Normal, estado.Dificultad);
        Assert.Equal(40, estado.VidaEnemigoMaxima);
    }

    [Theory]
    [InlineData(Dificultad.Normal, null)]
    [InlineData(Dificultad.Dificil, ResultadoCombate.Victoria)]
    [InlineData(Dificultad.Normal, ResultadoCombate.Derrota)]
    public async Task ObtenerEstado_IncluyeDificultadYResultadoDeCombate(
        Dificultad dificultad,
        ResultadoCombate? resultado)
    {
        var partida = CrearPartida();
        partida.Dificultad = dificultad;
        partida.ResultadoCombate = resultado;
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var estado = await servicio.ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(estado);
        Assert.Equal(dificultad, estado.Dificultad);
        Assert.Equal(resultado, estado.ResultadoCombate);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 40)]
    [InlineData(Dificultad.Dificil, 80)]
    public async Task PartidaNueva_InformaVidasCompletasDificultadYResultadoNulo(
        Dificultad dificultad,
        int vidaEnemigoEsperada)
    {
        var personaje = new Personaje
        {
            Id = 3,
            Nombre = "Bruma",
            UsuarioId = UsuarioId,
            VidaActual = 100,
            VidaMaxima = 100,
            DificultadDesbloqueada = dificultad
        };
        var creacion = new PartidaRepositoryFalso(null);
        await new PartidaService(creacion, new PersonajeRepositoryFalso(personaje), new EnemigoRepositoryFalso())
            .CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });
        var creada = creacion.Creada!;
        creada.Id = PartidaId;
        creada.Personaje = personaje;
        creada.Enemigo = new Enemigo { Id = creada.EnemigoId, Nombre = "Goblin", VidaMaxima = 30 };

        var estado = await CrearServicio(new PartidaRepositoryFalso(creada)).ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(estado);
        Assert.Equal(dificultad, estado.Dificultad);
        Assert.Null(estado.ResultadoCombate);
        Assert.Equal(80, estado.VidaPersonajeActual);
        Assert.Equal(80, estado.VidaPersonajeMaxima);
        Assert.Equal(vidaEnemigoEsperada, estado.VidaEnemigoActual);
        Assert.Equal(vidaEnemigoEsperada, estado.VidaEnemigoMaxima);
    }

    [Fact]
    public async Task FinalizacionManual_MantieneElResultadoNulo()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(CrearPartida()));

        var finalizacion = await servicio.FinalizarAsync(UsuarioId, PartidaId);
        var estado = await servicio.ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(finalizacion);
        Assert.False(finalizacion.ConcurrenciaPerdida);
        Assert.NotNull(estado);
        Assert.Equal(EstadoPartida.Finalizada, estado.Estado);
        Assert.Null(estado.ResultadoCombate);
    }

    [Fact]
    public async Task EstadoSerializado_ConservaLosCamposExistentesYAgregaLosNuevos()
    {
        var servicio = CrearServicio(new PartidaRepositoryFalso(CrearPartida()));
        var estado = await servicio.ObtenerEstadoAsync(UsuarioId, PartidaId);
        var esperados = new Dictionary<string, JsonValueKind>
        {
            ["partidaId"] = JsonValueKind.Number,
            ["fechaCreacion"] = JsonValueKind.String,
            ["fechaUltimaActualizacion"] = JsonValueKind.String,
            ["estado"] = JsonValueKind.Number,
            ["personajeId"] = JsonValueKind.Number,
            ["personajeNombre"] = JsonValueKind.String,
            ["personajeNivel"] = JsonValueKind.Number,
            ["vidaPersonajeActual"] = JsonValueKind.Number,
            ["vidaPersonajeMaxima"] = JsonValueKind.Number,
            ["personajeX"] = JsonValueKind.Number,
            ["personajeY"] = JsonValueKind.Number,
            ["enemigoId"] = JsonValueKind.Number,
            ["enemigoNombre"] = JsonValueKind.String,
            ["enemigoNivel"] = JsonValueKind.Number,
            ["vidaEnemigoActual"] = JsonValueKind.Number,
            ["vidaEnemigoMaxima"] = JsonValueKind.Number,
            ["enemigoX"] = JsonValueKind.Number,
            ["enemigoY"] = JsonValueKind.Number,
            ["dificultad"] = JsonValueKind.Number,
            ["resultadoCombate"] = JsonValueKind.Null
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(estado, JsonSerializerOptions.Web));

        var obtenidos = json.RootElement.EnumerateObject()
            .ToDictionary(propiedad => propiedad.Name, propiedad => propiedad.Value.ValueKind);
        Assert.Equal(esperados, obtenidos);
    }

    [Theory]
    [InlineData(5, 2, "abajo")]
    [InlineData(5, 4, "arriba")]
    [InlineData(4, 3, "derecha")]
    [InlineData(6, 3, "izquierda")]
    public async Task Mover_HaciaLaCeldaDelEnemigo_InformaConflictoSinCambiarCoordenadas(
        int x,
        int y,
        string direccion)
    {
        var partida = CrearPartidaEn(x, y);
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var resultado = await servicio.MoverAsync(
            UsuarioId,
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion });

        Assert.NotNull(resultado);
        Assert.True(resultado.ConcurrenciaPerdida);
        Assert.Equal(x, resultado.Estado.PersonajeX);
        Assert.Equal(y, resultado.Estado.PersonajeY);
        Assert.Equal(x, partida.PersonajeX);
        Assert.Equal(y, partida.PersonajeY);
        Assert.Equal(5, partida.EnemigoX);
        Assert.Equal(3, partida.EnemigoY);
        Assert.Equal(EstadoPartida.Activa, partida.Estado);
    }

    [Theory]
    [InlineData(5, 2, "abajo")]
    [InlineData(5, 4, "arriba")]
    [InlineData(4, 3, "derecha")]
    [InlineData(6, 3, "izquierda")]
    public async Task EndpointMover_HaciaLaCeldaDelEnemigo_Devuelve409SinCambiarCoordenadas(
        int x,
        int y,
        string direccion)
    {
        var partida = CrearPartidaEn(x, y);
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion },
            CancellationToken.None);

        var conflicto = Assert.IsType<ConflictObjectResult>(respuesta);
        Assert.Equal(StatusCodes.Status409Conflict, conflicto.StatusCode);
        Assert.Equal(x, partida.PersonajeX);
        Assert.Equal(y, partida.PersonajeY);
    }

    [Fact]
    public async Task Mover_IntentosRepetidosContraElEnemigo_NuncaCambianLasCoordenadas()
    {
        var partida = CrearPartidaEn(4, 3);
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        for (var intento = 0; intento < 3; intento++)
        {
            await servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha);

            Assert.Equal(4, partida.PersonajeX);
            Assert.Equal(3, partida.PersonajeY);
        }
    }

    [Theory]
    [InlineData(5, 2, "arriba", 5, 1)]
    [InlineData(5, 4, "abajo", 5, 5)]
    [InlineData(4, 3, "izquierda", 3, 3)]
    [InlineData(6, 3, "derecha", 7, 3)]
    public async Task EndpointMover_AlejandoseDelEnemigo_Devuelve200YCambiaCoordenadas(
        int x,
        int y,
        string direccion,
        int xEsperada,
        int yEsperada)
    {
        var partida = CrearPartidaEn(x, y);
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(respuesta);
        Assert.Equal(xEsperada, partida.PersonajeX);
        Assert.Equal(yEsperada, partida.PersonajeY);
    }

    [Theory]
    [InlineData(4, 2, "derecha", 5, 2)]
    [InlineData(6, 2, "abajo", 6, 3)]
    [InlineData(4, 4, "arriba", 4, 3)]
    [InlineData(6, 4, "izquierda", 5, 4)]
    public async Task Mover_DesdeUnaDiagonalDelEnemigo_NoEsColision(
        int x,
        int y,
        string direccion,
        int xEsperada,
        int yEsperada)
    {
        var partida = CrearPartidaEn(x, y);
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var resultado = await servicio.MoverAsync(
            UsuarioId,
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion });

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Equal(xEsperada, partida.PersonajeX);
        Assert.Equal(yEsperada, partida.PersonajeY);
    }

    [Theory]
    [InlineData(0, 1, "izquierda")]
    [InlineData(10, 1, "derecha")]
    [InlineData(1, 0, "arriba")]
    [InlineData(1, 6, "abajo")]
    public async Task EndpointMover_ContraUnLimiteDelTablero_Devuelve200SinCambiarCoordenadas(
        int x,
        int y,
        string direccion)
    {
        var partida = CrearPartidaEn(x, y);
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(respuesta);
        Assert.Equal(x, partida.PersonajeX);
        Assert.Equal(y, partida.PersonajeY);
    }

    [Fact]
    public async Task EndpointMover_PartidaAjenaJuntoAlEnemigo_Devuelve404SinCambiarCoordenadas()
    {
        var partida = CrearPartidaEn(4, 3);
        partida.Personaje.UsuarioId = OtroUsuarioId;
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(PartidaId, MoverDerecha, CancellationToken.None);

        Assert.IsType<NotFoundResult>(respuesta);
        Assert.Equal(4, partida.PersonajeX);
        Assert.Equal(3, partida.PersonajeY);
    }

    [Fact]
    public async Task EndpointMover_PartidaFinalizadaJuntoAlEnemigo_Devuelve409SinCambiarCoordenadas()
    {
        var partida = CrearPartidaEn(4, 3);
        partida.Estado = EstadoPartida.Finalizada;
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(PartidaId, MoverDerecha, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(respuesta);
        Assert.Equal(4, partida.PersonajeX);
        Assert.Equal(3, partida.PersonajeY);
    }

    [Theory]
    [InlineData(5, 2, "abajo")]
    [InlineData(5, 4, "arriba")]
    [InlineData(4, 3, "derecha")]
    [InlineData(6, 3, "izquierda")]
    public async Task EndpointMover_Colision_MuestraElMensajeDeCasillaOcupada(int x, int y, string direccion)
    {
        var controlador = CrearControlador(new PartidaRepositoryFalso(CrearPartidaEn(x, y)));

        var respuesta = await controlador.Mover(
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion },
            CancellationToken.None);

        VerificarConflicto(respuesta, "Movimiento no permitido.", "La casilla está ocupada por el enemigo.");
    }

    [Fact]
    public async Task EndpointMover_PartidaFinalizada_MuestraElMensajeDePartidaNoActiva()
    {
        var partida = CrearPartidaEn(1, 1);
        partida.Estado = EstadoPartida.Finalizada;
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(PartidaId, MoverDerecha, CancellationToken.None);

        VerificarConflicto(
            respuesta,
            "La partida no está activa.",
            "No se puede mover el personaje porque la partida ya fue finalizada.");
    }

    [Theory]
    [InlineData(5, 2, "abajo")]
    [InlineData(5, 4, "arriba")]
    [InlineData(4, 3, "derecha")]
    [InlineData(6, 3, "izquierda")]
    public async Task EndpointMover_PartidaFinalizadaJuntoAlEnemigo_MuestraElMensajeDePartidaNoActiva(
        int x,
        int y,
        string direccion)
    {
        var partida = CrearPartidaEn(x, y);
        partida.Estado = EstadoPartida.Finalizada;
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Mover(
            PartidaId,
            new MoverPartidaRequest { Direccion = direccion },
            CancellationToken.None);

        VerificarConflicto(
            respuesta,
            "La partida no está activa.",
            "No se puede mover el personaje porque la partida ya fue finalizada.");
    }

    [Theory]
    [InlineData(Dificultad.Normal, 5, 2, 70, 25, 10)]
    [InlineData(Dificultad.Normal, 5, 4, 70, 25, 10)]
    [InlineData(Dificultad.Normal, 4, 3, 70, 25, 10)]
    [InlineData(Dificultad.Normal, 6, 3, 70, 25, 10)]
    [InlineData(Dificultad.Dificil, 5, 2, 65, 65, 15)]
    [InlineData(Dificultad.Dificil, 5, 4, 65, 65, 15)]
    [InlineData(Dificultad.Dificil, 4, 3, 65, 65, 15)]
    [InlineData(Dificultad.Dificil, 6, 3, 65, 65, 15)]
    public async Task Atacar_DesdeUnaCasillaAdyacente_AplicaDanioYContraataque(
        Dificultad dificultad,
        int x,
        int y,
        int vidaJugadorEsperada,
        int vidaEnemigoEsperada,
        int danioRecibidoEsperado)
    {
        var estadisticas = ReglasCombate.ObtenerEstadisticas(dificultad);
        var partida = CrearPartidaEnCombate(x, y, dificultad, estadisticas.VidaJugador, estadisticas.VidaEnemigo);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(15, respuesta.DanioInfligido);
        Assert.Equal(danioRecibidoEsperado, respuesta.DanioRecibido);
        Assert.Equal(vidaJugadorEsperada, respuesta.Estado.VidaPersonajeActual);
        Assert.Equal(vidaEnemigoEsperada, respuesta.Estado.VidaEnemigoActual);
        Assert.Equal(EstadoPartida.Activa, respuesta.Estado.Estado);
        Assert.Null(respuesta.Estado.ResultadoCombate);
        Assert.Equal(vidaJugadorEsperada, partida.VidaPersonajeActual);
        Assert.Equal(vidaEnemigoEsperada, partida.VidaEnemigoActual);
        Assert.Equal(x, partida.PersonajeX);
        Assert.Equal(y, partida.PersonajeY);
        Assert.Equal(5, partida.EnemigoX);
        Assert.Equal(3, partida.EnemigoY);
    }

    [Fact]
    public async Task Atacar_EnemigoSobreviveConUnPunto_Contraataca()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 16);

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(1, respuesta.Estado.VidaEnemigoActual);
        Assert.Equal(70, respuesta.Estado.VidaPersonajeActual);
        Assert.Equal(10, respuesta.DanioRecibido);
        Assert.Null(respuesta.Estado.ResultadoCombate);
    }

    [Theory]
    [InlineData(15, 15)]
    [InlineData(10, 10)]
    public async Task Atacar_EnemigoMuere_EsVictoriaFinalizadaSinContraataque(int vidaEnemigo, int danioEsperado)
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, vidaEnemigo);

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(danioEsperado, respuesta.DanioInfligido);
        Assert.Equal(0, respuesta.DanioRecibido);
        Assert.Equal(0, respuesta.Estado.VidaEnemigoActual);
        Assert.Equal(60, respuesta.Estado.VidaPersonajeActual);
        Assert.Equal(ResultadoCombate.Victoria, respuesta.Estado.ResultadoCombate);
        Assert.Equal(EstadoPartida.Finalizada, respuesta.Estado.Estado);
        Assert.Equal(ResultadoCombate.Victoria, partida.ResultadoCombate);
        Assert.Equal(EstadoPartida.Finalizada, partida.Estado);
        Assert.NotEqual(default, partida.FechaUltimaActualizacion);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(5, 5)]
    public async Task Atacar_ContraataqueMataAlJugador_EsDerrotaFinalizadaSinDesbloqueo(
        int vidaJugador,
        int danioEsperado)
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, vidaJugador, 40);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(danioEsperado, respuesta.DanioRecibido);
        Assert.Equal(0, respuesta.Estado.VidaPersonajeActual);
        Assert.Equal(25, respuesta.Estado.VidaEnemigoActual);
        Assert.Equal(ResultadoCombate.Derrota, respuesta.Estado.ResultadoCombate);
        Assert.Equal(EstadoPartida.Finalizada, respuesta.Estado.Estado);
        Assert.NotEqual(default, partida.FechaUltimaActualizacion);
        Assert.False(repositorio.UltimoDesbloqueoSolicitado);
        Assert.Equal(Dificultad.Normal, partida.Personaje.DificultadDesbloqueada);
    }

    [Fact]
    public async Task Atacar_VictoriaEnNormal_DesbloqueaDificil()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarAceptado(resultado);
        Assert.True(repositorio.UltimoDesbloqueoSolicitado);
        Assert.Equal(Dificultad.Dificil, partida.Personaje.DificultadDesbloqueada);
    }

    [Fact]
    public async Task Atacar_VictoriaEnDificil_NoSolicitaDesbloqueoAunqueElPersonajeEsteEnNormal()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Dificil, 60, 10);
        partida.Personaje.DificultadDesbloqueada = Dificultad.Normal;
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarAceptado(resultado);
        Assert.False(repositorio.UltimoDesbloqueoSolicitado);
        Assert.Equal(Dificultad.Normal, partida.Personaje.DificultadDesbloqueada);
    }

    [Fact]
    public async Task Atacar_SinResultado_NoSolicitaDesbloqueo()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida);

        await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        Assert.False(repositorio.UltimoDesbloqueoSolicitado);
        Assert.Equal(Dificultad.Normal, partida.Personaje.DificultadDesbloqueada);
    }

    [Fact]
    public async Task Atacar_VictoriaEnNormalConDificilYaDesbloqueado_ConservaElDesbloqueo()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        partida.Personaje.DificultadDesbloqueada = Dificultad.Dificil;

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).AtacarAsync(UsuarioId, PartidaId);

        VerificarAceptado(resultado);
        Assert.Equal(Dificultad.Dificil, partida.Personaje.DificultadDesbloqueada);
    }

    [Fact]
    public async Task FinalizacionManual_NoDesbloqueaDificil()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);

        await CrearServicio(new PartidaRepositoryFalso(partida)).FinalizarAsync(UsuarioId, PartidaId);

        Assert.Equal(EstadoPartida.Finalizada, partida.Estado);
        Assert.Null(partida.ResultadoCombate);
        Assert.Equal(Dificultad.Normal, partida.Personaje.DificultadDesbloqueada);
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(6, 2)]
    [InlineData(4, 4)]
    [InlineData(6, 4)]
    [InlineData(5, 3)]
    [InlineData(5, 1)]
    [InlineData(3, 3)]
    [InlineData(0, 0)]
    public async Task Atacar_SinAdyacenciaOrtogonal_RechazaSinEscribir(int x, int y)
    {
        var partida = CrearPartidaEnCombate(x, y, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.FueraDeAlcance);
        VerificarSinEscrituras(repositorio, partida, x, y);
    }

    [Fact]
    public async Task Atacar_PartidaFinalizada_RechazaSinEscribir()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        partida.Estado = EstadoPartida.Finalizada;
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.PartidaNoActiva);
        Assert.Equal(0, repositorio.LlamadasAplicarAtaque);
        Assert.Equal(80, partida.VidaPersonajeActual);
        Assert.Equal(40, partida.VidaEnemigoActual);
    }

    [Fact]
    public async Task Atacar_PartidaInexistente_DevuelveNull()
    {
        var repositorio = new PartidaRepositoryFalso(null);

        Assert.Null(await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId));
        Assert.Equal(0, repositorio.LlamadasAplicarAtaque);
    }

    [Fact]
    public async Task Atacar_PartidaAjena_DevuelveNullSinEscribir()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida);

        Assert.Null(await CrearServicio(repositorio).AtacarAsync(OtroUsuarioId, PartidaId));
        VerificarSinEscrituras(repositorio, partida, 4, 3);
    }

    [Fact]
    public async Task Atacar_OtroAtaqueGanaLaCarrera_InformaEstadoObsoletoSinDanioDoble()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida)
        {
            AntesDeAplicarAtaque = () =>
            {
                partida.VidaPersonajeActual = 70;
                partida.VidaEnemigoActual = 25;
            }
        };

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.EstadoObsoleto);
        Assert.Equal(70, partida.VidaPersonajeActual);
        Assert.Equal(25, partida.VidaEnemigoActual);
    }

    [Fact]
    public async Task Atacar_OtraOperacionFinalizaEnLaCarrera_InformaPartidaNoActivaSinDesbloqueo()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        var repositorio = new PartidaRepositoryFalso(partida)
        {
            AntesDeAplicarAtaque = () => partida.Estado = EstadoPartida.Finalizada
        };

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.PartidaNoActiva);
        Assert.Equal(10, partida.VidaEnemigoActual);
        Assert.Null(partida.ResultadoCombate);
        Assert.Equal(Dificultad.Normal, partida.Personaje.DificultadDesbloqueada);
    }

    [Fact]
    public async Task Atacar_ElJugadorSeAlejaEnLaCarrera_InformaFueraDeAlcance()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida)
        {
            AntesDeAplicarAtaque = () => partida.PersonajeX = 3
        };

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.FueraDeAlcance);
        Assert.Equal(80, partida.VidaPersonajeActual);
        Assert.Equal(40, partida.VidaEnemigoActual);
    }

    [Fact]
    public async Task Atacar_ExitosoSinLecturaPosterior_LanzaErrorDeConsistencia()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida) { LecturaNullTrasAtaque = true });

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AtacarAsync(UsuarioId, PartidaId));
    }

    [Fact]
    public async Task EndpointAtacar_Aceptado_Devuelve200ConEstadoYDanios()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(respuesta);
        var cuerpo = Assert.IsType<AtaquePartidaResponse>(ok.Value);
        Assert.Equal(15, cuerpo.DanioInfligido);
        Assert.Equal(10, cuerpo.DanioRecibido);
        Assert.Equal(70, cuerpo.Estado.VidaPersonajeActual);
        Assert.Equal(25, cuerpo.Estado.VidaEnemigoActual);
    }

    [Theory]
    [InlineData(60, 10, ResultadoCombate.Victoria)]
    [InlineData(10, 40, ResultadoCombate.Derrota)]
    public async Task EndpointAtacar_FinDelCombate_Devuelve200(
        int vidaJugador,
        int vidaEnemigo,
        ResultadoCombate esperado)
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, vidaJugador, vidaEnemigo);
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(respuesta);
        var cuerpo = Assert.IsType<AtaquePartidaResponse>(ok.Value);
        Assert.Equal(esperado, cuerpo.Estado.ResultadoCombate);
        Assert.Equal(EstadoPartida.Finalizada, cuerpo.Estado.Estado);
    }

    [Fact]
    public async Task EndpointAtacar_FueraDeAlcance_Devuelve409ConMensaje()
    {
        var controlador = CrearControlador(new PartidaRepositoryFalso(
            CrearPartidaEnCombate(4, 2, Dificultad.Normal, 80, 40)));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        VerificarConflicto(respuesta, "Ataque no permitido.", "El enemigo está fuera de alcance.");
    }

    [Fact]
    public async Task EndpointAtacar_PartidaFinalizada_Devuelve409ConMensaje()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        partida.Estado = EstadoPartida.Finalizada;
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        VerificarConflicto(
            respuesta,
            "La partida no está activa.",
            "No se puede atacar porque la partida ya fue finalizada.");
    }

    [Fact]
    public async Task EndpointAtacar_EstadoObsoleto_Devuelve409ConMensaje()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var controlador = CrearControlador(new PartidaRepositoryFalso(partida)
        {
            AntesDeAplicarAtaque = () => partida.VidaEnemigoActual = 25
        });

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        VerificarConflicto(
            respuesta,
            "La partida cambió.",
            "La partida cambió mientras se procesaba el ataque. Volvé a intentarlo.");
    }

    [Fact]
    public async Task EndpointAtacar_PartidaInexistenteOAjena_Devuelve404()
    {
        var ajena = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        ajena.Personaje.UsuarioId = OtroUsuarioId;

        var inexistente = await CrearControlador(new PartidaRepositoryFalso(null))
            .Atacar(PartidaId, CancellationToken.None);
        var deOtroUsuario = await CrearControlador(new PartidaRepositoryFalso(ajena))
            .Atacar(PartidaId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(inexistente);
        Assert.IsType<NotFoundResult>(deOtroUsuario);
    }

    [Fact]
    public async Task EndpointAtacar_SinIdentificadorDeUsuario_Devuelve401()
    {
        var controlador = new PartidasController(CrearServicio(new PartidaRepositoryFalso(null)))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(respuesta);
    }

    [Fact]
    public async Task EndpointAtacar_RespuestaValida_Devuelve200ConLaRespuesta()
    {
        var esperada = CrearRespuestaDeAtaque();
        var controlador = CrearControlador(new ServicioDeAtaqueFijo(new AtaquePartidaResult(esperada, null)));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(respuesta);
        Assert.Same(esperada, ok.Value);
    }

    [Fact]
    public async Task EndpointAtacar_ResultadoConAmbosNulos_Devuelve500()
    {
        var controlador = CrearControlador(new ServicioDeAtaqueFijo(new AtaquePartidaResult(null, null)));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        VerificarErrorInternoSinContenido(respuesta);
    }

    [Theory]
    [InlineData(RechazoAtaque.PartidaNoActiva)]
    [InlineData(RechazoAtaque.FueraDeAlcance)]
    [InlineData(RechazoAtaque.EstadoObsoleto)]
    public async Task EndpointAtacar_ResultadoConAmbosInformados_Devuelve500(RechazoAtaque rechazo)
    {
        var controlador = CrearControlador(new ServicioDeAtaqueFijo(
            new AtaquePartidaResult(CrearRespuestaDeAtaque(), rechazo)));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        VerificarErrorInternoSinContenido(respuesta);
    }

    [Theory]
    [InlineData((RechazoAtaque)99)]
    [InlineData((RechazoAtaque)(-1))]
    public async Task EndpointAtacar_RechazoNoDefinido_Devuelve500(RechazoAtaque rechazo)
    {
        var controlador = CrearControlador(new ServicioDeAtaqueFijo(new AtaquePartidaResult(null, rechazo)));

        var respuesta = await controlador.Atacar(PartidaId, CancellationToken.None);

        VerificarErrorInternoSinContenido(respuesta);
    }

    [Fact]
    public void EndpointAtacar_ExigeAutorizacionYNoRecibeDatosDelCliente()
    {
        var metodo = typeof(PartidasController).GetMethod(nameof(PartidasController.Atacar))!;

        Assert.NotEmpty(typeof(PartidasController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(metodo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        var ruta = Assert.Single(metodo.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>());
        Assert.Equal("{id:int:min(1)}/ataque", ruta.Template);
        Assert.Equal(
            [typeof(int), typeof(CancellationToken)],
            metodo.GetParameters().Select(parametro => parametro.ParameterType));
    }

    [Fact]
    public async Task Mover_DesdeLaCeldaDelEnemigo_PermiteSalir()
    {
        var partida = CrearPartidaEn(5, 3);
        var servicio = CrearServicio(new PartidaRepositoryFalso(partida));

        var resultado = await servicio.MoverAsync(UsuarioId, PartidaId, MoverDerecha);

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Equal(6, partida.PersonajeX);
        Assert.Equal(3, partida.PersonajeY);
    }

    // MoverAsync y FinalizarAsync no usan los repositorios de personajes ni de enemigos.
    private static PartidaService CrearServicio(IPartidaRepository repositorio) =>
        new(repositorio, null!, null!);

    private static PartidasController CrearControlador(IPartidaRepository repositorio) =>
        CrearControlador(CrearServicio(repositorio));

    private static PartidasController CrearControlador(IPartidaService servicio)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, UsuarioId.ToString()) };
        return new PartidasController(servicio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
            }
        };
    }

    private static Partida CrearPartida() => new()
    {
        Id = PartidaId,
        Estado = EstadoPartida.Activa,
        Personaje = new Personaje { Nombre = "Bruma", UsuarioId = UsuarioId },
        Enemigo = new Enemigo { Nombre = "Goblin" },
        VidaPersonajeActual = 84,
        VidaEnemigoActual = 30,
        PersonajeX = 1,
        PersonajeY = 1,
        EnemigoX = 5,
        EnemigoY = 3
    };

    private static void VerificarConflicto(IActionResult respuesta, string titulo, string detalle)
    {
        var conflicto = Assert.IsType<ConflictObjectResult>(respuesta);
        Assert.Equal(StatusCodes.Status409Conflict, conflicto.StatusCode);
        var problema = Assert.IsType<ProblemDetails>(conflicto.Value);
        Assert.Equal(titulo, problema.Title);
        Assert.Equal(detalle, problema.Detail);
    }

    private static void VerificarErrorInternoSinContenido(IActionResult respuesta)
    {
        var error = Assert.IsType<StatusCodeResult>(respuesta);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
    }

    private static AtaquePartidaResponse CrearRespuestaDeAtaque() => new(
        new EstadoPartidaResponse(
            PartidaId,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            EstadoPartida.Activa,
            3,
            "Bruma",
            1,
            70,
            80,
            4,
            3,
            1,
            "Goblin",
            1,
            25,
            40,
            5,
            3,
            Dificultad.Normal,
            null),
        15,
        10);

    private static AtaquePartidaResponse VerificarAceptado(AtaquePartidaResult? resultado)
    {
        Assert.NotNull(resultado);
        Assert.Null(resultado.Rechazo);
        Assert.NotNull(resultado.Respuesta);
        return resultado.Respuesta;
    }

    private static void VerificarRechazo(AtaquePartidaResult? resultado, RechazoAtaque esperado)
    {
        Assert.NotNull(resultado);
        Assert.Null(resultado.Respuesta);
        Assert.Equal(esperado, resultado.Rechazo);
    }

    private static void VerificarSinEscrituras(PartidaRepositoryFalso repositorio, Partida partida, int x, int y)
    {
        Assert.Equal(0, repositorio.LlamadasAplicarAtaque);
        Assert.Equal(80, partida.VidaPersonajeActual);
        Assert.Equal(40, partida.VidaEnemigoActual);
        Assert.Equal(x, partida.PersonajeX);
        Assert.Equal(y, partida.PersonajeY);
        Assert.Equal(EstadoPartida.Activa, partida.Estado);
        Assert.Null(partida.ResultadoCombate);
        Assert.Equal(Dificultad.Normal, partida.Personaje.DificultadDesbloqueada);
    }

    private static Partida CrearPartidaEnCombate(
        int x,
        int y,
        Dificultad dificultad,
        int vidaJugador,
        int vidaEnemigo)
    {
        var partida = CrearPartidaEn(x, y);
        partida.Dificultad = dificultad;
        partida.VidaPersonajeActual = vidaJugador;
        partida.VidaEnemigoActual = vidaEnemigo;
        partida.Personaje.DificultadDesbloqueada = dificultad;
        return partida;
    }

    private static Partida CrearPartidaEn(int x, int y)
    {
        var partida = CrearPartida();
        partida.PersonajeX = x;
        partida.PersonajeY = y;
        return partida;
    }

    /// <summary>Reproduce en memoria las condiciones y los resultados de los UPDATE del repositorio real.</summary>
    private sealed class PartidaRepositoryFalso(Partida? partida) : IPartidaRepository
    {
        public Action? DespuesDeMover { get; init; }

        public bool LecturaDevuelveNull { get; init; }

        public Partida? Creada { get; private set; }

        public Action? AntesDeAplicarAtaque { get; init; }

        public bool LecturaNullTrasAtaque { get; init; }

        public int LlamadasAplicarAtaque { get; private set; }

        public bool? UltimoDesbloqueoSolicitado { get; private set; }

        public Task<int> AplicarAtaqueAsync(
            Partida leida,
            int usuarioId,
            int vidaPersonaje,
            int vidaEnemigo,
            ResultadoCombate? resultado,
            bool desbloquearDificil,
            CancellationToken cancellationToken = default)
        {
            LlamadasAplicarAtaque++;
            UltimoDesbloqueoSolicitado = desbloquearDificil;
            AntesDeAplicarAtaque?.Invoke();

            var actual = Buscar(leida.Id, usuarioId);
            if (actual is null
                || actual.Estado != EstadoPartida.Activa
                || actual.Dificultad != leida.Dificultad
                || actual.VidaPersonajeActual != leida.VidaPersonajeActual
                || actual.VidaEnemigoActual != leida.VidaEnemigoActual
                || actual.PersonajeX != leida.PersonajeX
                || actual.PersonajeY != leida.PersonajeY
                || actual.EnemigoX != leida.EnemigoX
                || actual.EnemigoY != leida.EnemigoY)
            {
                return Task.FromResult(0);
            }

            actual.VidaPersonajeActual = vidaPersonaje;
            actual.VidaEnemigoActual = vidaEnemigo;
            actual.ResultadoCombate = resultado;
            actual.Estado = resultado is null ? EstadoPartida.Activa : EstadoPartida.Finalizada;
            actual.FechaUltimaActualizacion = DateTime.UtcNow;
            if (desbloquearDificil && actual.Personaje.DificultadDesbloqueada == Dificultad.Normal)
            {
                actual.Personaje.DificultadDesbloqueada = Dificultad.Dificil;
            }

            return Task.FromResult(1);
        }

        public Task<Partida?> ObtenerPorIdAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LecturaDevuelveNull ? null : Buscar(id, usuarioId));

        // Devuelve una copia, como la lectura AsNoTracking real: los cambios posteriores no alteran lo ya leído.
        public Task<Partida?> ObtenerEstadoAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            if (LecturaDevuelveNull || (LecturaNullTrasAtaque && LlamadasAplicarAtaque > 0))
            {
                return Task.FromResult<Partida?>(null);
            }

            var encontrada = Buscar(id, usuarioId);
            return Task.FromResult(encontrada is null ? null : CopiarPartida(encontrada));
        }

        private static Partida CopiarPartida(Partida origen) => new()
        {
            Id = origen.Id,
            FechaCreacion = origen.FechaCreacion,
            FechaUltimaActualizacion = origen.FechaUltimaActualizacion,
            Estado = origen.Estado,
            Dificultad = origen.Dificultad,
            ResultadoCombate = origen.ResultadoCombate,
            PersonajeId = origen.PersonajeId,
            Personaje = origen.Personaje,
            EnemigoId = origen.EnemigoId,
            Enemigo = origen.Enemigo,
            VidaPersonajeActual = origen.VidaPersonajeActual,
            VidaEnemigoActual = origen.VidaEnemigoActual,
            PersonajeX = origen.PersonajeX,
            PersonajeY = origen.PersonajeY,
            EnemigoX = origen.EnemigoX,
            EnemigoY = origen.EnemigoY
        };

        public Task<IReadOnlyList<Partida>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Partida> CrearAsync(Partida nuevaPartida, CancellationToken cancellationToken = default)
        {
            Creada = nuevaPartida;
            return Task.FromResult(nuevaPartida);
        }

        public Task<int> MoverAsync(
            int id,
            int usuarioId,
            int deltaX,
            int deltaY,
            CancellationToken cancellationToken = default)
        {
            var activa = Buscar(id, usuarioId);
            if (activa?.Estado != EstadoPartida.Activa
                || (activa.PersonajeX + deltaX == activa.EnemigoX && activa.PersonajeY + deltaY == activa.EnemigoY))
            {
                return Task.FromResult(0);
            }

            activa.PersonajeX = Math.Clamp(activa.PersonajeX + deltaX, 0, 10);
            activa.PersonajeY = Math.Clamp(activa.PersonajeY + deltaY, 0, 6);
            DespuesDeMover?.Invoke();
            return Task.FromResult(1);
        }

        public Task<int> FinalizarAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var activa = Buscar(id, usuarioId);
            if (activa?.Estado != EstadoPartida.Activa)
            {
                return Task.FromResult(0);
            }

            activa.Estado = EstadoPartida.Finalizada;
            return Task.FromResult(1);
        }

        private Partida? Buscar(int id, int usuarioId) =>
            partida?.Id == id && partida.Personaje.UsuarioId == usuarioId ? partida : null;
    }

    private sealed class ServicioDeAtaqueFijo(AtaquePartidaResult resultado) : IPartidaService
    {
        public Task<AtaquePartidaResult?> AtacarAsync(
            int usuarioId,
            int partidaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AtaquePartidaResult?>(resultado);

        public Task<PartidaResponse?> CrearAsync(
            int usuarioId,
            CrearPartidaRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PartidaResponse>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PartidaResponse?> ObtenerPorIdAsync(
            int usuarioId,
            int partidaId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<EstadoPartidaResponse?> ObtenerEstadoAsync(
            int usuarioId,
            int partidaId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MovimientoPartidaResult?> MoverAsync(
            int usuarioId,
            int partidaId,
            MoverPartidaRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FinalizacionPartidaResult?> FinalizarAsync(
            int usuarioId,
            int partidaId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class PersonajeRepositoryFalso(Personaje personaje) : IPersonajeRepository
    {
        public Task<Personaje?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Personaje?> ObtenerPorIdAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Personaje?>(personaje);

        public Task<IReadOnlyList<Personaje>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Personaje> CrearAsync(Personaje nuevo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Personaje> ActualizarAsync(Personaje existente, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class EnemigoRepositoryFalso(int vidaMaxima = 30) : IEnemigoRepository
    {
        public Task<Enemigo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Enemigo?>(new Enemigo { Id = id, Nombre = "Goblin", VidaMaxima = vidaMaxima });

        public Task<IReadOnlyList<Enemigo>> ObtenerTodosAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Enemigo> CrearAsync(Enemigo enemigo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
