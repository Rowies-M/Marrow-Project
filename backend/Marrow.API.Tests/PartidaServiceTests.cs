using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marrow.API.Controllers;
using Marrow.API.Data;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Marrow.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

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
    public async Task Crear_UsaLaDificultadElegidaSinResultadoDeCombate(Dificultad dificultad)
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var personajes = new PersonajeRepositoryFalso(new Personaje
        {
            Id = 3,
            UsuarioId = UsuarioId,
            DificultadDesbloqueada = dificultad
        });
        var servicio = new PartidaService(repositorio, personajes, new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3, Dificultad = dificultad });

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
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });
        personaje.DificultadDesbloqueada = Dificultad.Dificil;

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(Dificultad.Normal, repositorio.Creada.Dificultad);
    }

    [Theory]
    [InlineData(Dificultad.Normal, 80, 30)]
    [InlineData(Dificultad.Normal, 500, 30)]
    [InlineData(Dificultad.Dificil, 80, 60)]
    [InlineData(Dificultad.Dificil, 500, 60)]
    public async Task Crear_InicializaLasVidasConElPersonajeYElCatalogo(
        Dificultad dificultad,
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
                VidaActual = 1,
                VidaMaxima = vidaMaximaPersonaje,
                Ataque = 10,
                DificultadDesbloqueada = dificultad
            }),
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3, Dificultad = dificultad });

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(vidaMaximaPersonaje, repositorio.Creada.VidaPersonajeActual);
        Assert.Equal(vidaEnemigoEsperada, repositorio.Creada.VidaEnemigoActual);
    }

    [Theory]
    [InlineData(1, 1, 30)]
    [InlineData(2, 2, 45)]
    [InlineData(3, 3, 80)]
    [InlineData(4, 3, 80)]
    public async Task Crear_AsignaElEnemigoSegunElNivelDelPersonaje(
        int nivelPersonaje,
        int enemigoEsperado,
        int vidaEnemigoEsperada)
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var servicio = new PartidaService(
            repositorio,
            new PersonajeRepositoryFalso(new Personaje
            {
                Id = 3,
                UsuarioId = UsuarioId,
                Nivel = nivelPersonaje,
                DificultadDesbloqueada = Dificultad.Normal
            }),
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(enemigoEsperado, repositorio.Creada.EnemigoId);
        Assert.Equal(vidaEnemigoEsperada, repositorio.Creada.VidaEnemigoActual);
    }

    [Fact]
    public void CrearPartidaRequest_NoPermiteElegirElEnemigo()
    {
        var propiedades = typeof(CrearPartidaRequest).GetProperties().Select(propiedad => propiedad.Name).Order();

        Assert.Equal(
            [nameof(CrearPartidaRequest.Dificultad), nameof(CrearPartidaRequest.PersonajeId)],
            propiedades);
    }

    [Fact]
    public void CrearPartidaRequest_LaDificultadPredeterminadaEsNormal()
    {
        Assert.Equal(Dificultad.Normal, new CrearPartidaRequest().Dificultad);
    }

    [Fact]
    public void CrearPartidaRequest_RechazaUnaDificultadSinDefinir()
    {
        var request = new CrearPartidaRequest { PersonajeId = 3, Dificultad = (Dificultad)99 };

        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), null, validateAllProperties: true));
    }

    [Fact]
    public async Task Crear_ConDificilDesbloqueadaYSinElegir_UsaNormal()
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var servicio = new PartidaService(
            repositorio,
            new PersonajeRepositoryFalso(new Personaje { Id = 3, UsuarioId = UsuarioId, DificultadDesbloqueada = Dificultad.Dificil }),
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

        await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 });

        Assert.NotNull(repositorio.Creada);
        Assert.Equal(Dificultad.Normal, repositorio.Creada.Dificultad);
    }

    [Fact]
    public async Task Crear_DificilSinDesbloquear_SeRechazaSinCrearLaPartida()
    {
        var repositorio = new PartidaRepositoryFalso(null);
        var servicio = new PartidaService(
            repositorio,
            new PersonajeRepositoryFalso(new Personaje { Id = 3, UsuarioId = UsuarioId, DificultadDesbloqueada = Dificultad.Normal }),
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

        var resultado = await servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3, Dificultad = Dificultad.Dificil });

        Assert.NotNull(resultado);
        Assert.True(resultado.DificultadBloqueada);
        Assert.Null(resultado.Partida);
        Assert.Null(repositorio.Creada);
    }

    [Fact]
    public async Task EndpointCrear_DificilSinDesbloquear_Devuelve409ConMensaje()
    {
        var servicio = new PartidaService(
            new PartidaRepositoryFalso(null),
            new PersonajeRepositoryFalso(new Personaje { Id = 3, UsuarioId = UsuarioId, DificultadDesbloqueada = Dificultad.Normal }),
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);
        var controlador = CrearControlador(servicio);

        var respuesta = await controlador.Crear(
            new CrearPartidaRequest { PersonajeId = 3, Dificultad = Dificultad.Dificil },
            CancellationToken.None);

        VerificarConflicto(respuesta, "Dificultad bloqueada.", "Ganá una partida en Normal para desbloquear Difícil.");
    }

    [Fact]
    public async Task Crear_SinEnemigoParaElNivel_LanzaError()
    {
        var servicio = new PartidaService(
            new PartidaRepositoryFalso(null),
            new PersonajeRepositoryFalso(new Personaje { Id = 3, UsuarioId = UsuarioId, Nivel = 2 }),
            new EnemigoRepositoryFalso(sinEnemigos: true), NullLogger<PartidaService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3 }));
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
            new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance);

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
    [InlineData(Dificultad.Normal, 500, 30)]
    [InlineData(Dificultad.Dificil, 500, 60)]
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
    [InlineData(Dificultad.Normal, 30)]
    [InlineData(Dificultad.Dificil, 60)]
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
        await new PartidaService(creacion, new PersonajeRepositoryFalso(personaje), new EnemigoRepositoryFalso(), NullLogger<PartidaService>.Instance)
            .CrearAsync(UsuarioId, new CrearPartidaRequest { PersonajeId = 3, Dificultad = dificultad });
        var creada = creacion.Creada!;
        creada.Id = PartidaId;
        creada.Personaje = personaje;
        creada.Enemigo = new Enemigo { Id = creada.EnemigoId, Nombre = "Goblin", VidaMaxima = 30 };

        var estado = await CrearServicio(new PartidaRepositoryFalso(creada)).ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(estado);
        Assert.Equal(dificultad, estado.Dificultad);
        Assert.Null(estado.ResultadoCombate);
        Assert.Equal(100, estado.VidaPersonajeActual);
        Assert.Equal(100, estado.VidaPersonajeMaxima);
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
            ["estado"] = JsonValueKind.String,
            ["personajeId"] = JsonValueKind.Number,
            ["personajeNombre"] = JsonValueKind.String,
            ["personajeNivel"] = JsonValueKind.Number,
            ["personajeExperiencia"] = JsonValueKind.Number,
            ["personajeNivelMaximo"] = JsonValueKind.Number,
            ["personajeExperienciaSiguienteNivel"] = JsonValueKind.Number,
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
            ["dificultad"] = JsonValueKind.String,
            ["resultadoCombate"] = JsonValueKind.Null,
            ["golpePoderosoDesbloqueado"] = JsonValueKind.False,
            ["golpePoderosoNivelRequerido"] = JsonValueKind.Number
        };
        var opciones = new JsonSerializerOptions(JsonSerializerOptions.Web);
        opciones.Converters.Add(new JsonStringEnumConverter());

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(estado, opciones));

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
        var vidaEnemigo = dificultad == Dificultad.Normal ? 40 : 80;
        var partida = CrearPartidaEnCombate(x, y, dificultad, 80, vidaEnemigo);
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
    public async Task Listar_PasaLaConsultaYElUsuarioAlRepositorio()
    {
        var repositorio = new PartidaRepositoryFalso(CrearPartida()) { TotalListado = 1 };

        await CrearServicio(repositorio).ObtenerPorUsuarioAsync(UsuarioId, new ConsultaPartidasRequest
        {
            Estado = EstadoPartida.Finalizada,
            Dificultad = Dificultad.Dificil,
            Orden = "asc",
            Page = 3,
            PageSize = 20
        });

        Assert.Equal((UsuarioId, EstadoPartida.Finalizada, Dificultad.Dificil, true, 3, 20), repositorio.UltimaConsulta);
    }

    [Fact]
    public async Task Listar_SinParametros_UsaLosValoresPredeterminados()
    {
        var repositorio = new PartidaRepositoryFalso(CrearPartida()) { TotalListado = 1 };

        var pagina = await CrearServicio(repositorio).ObtenerPorUsuarioAsync(UsuarioId, new ConsultaPartidasRequest());

        Assert.Equal((UsuarioId, (EstadoPartida?)null, (Dificultad?)null, false, 1, 10), repositorio.UltimaConsulta);
        Assert.Equal(1, pagina.Page);
        Assert.Equal(10, pagina.PageSize);
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public async Task Listar_CalculaElTotalDePaginas(int total, int pageSize, int paginasEsperadas)
    {
        var repositorio = new PartidaRepositoryFalso(CrearPartida()) { TotalListado = total };

        var pagina = await CrearServicio(repositorio).ObtenerPorUsuarioAsync(UsuarioId, new ConsultaPartidasRequest { PageSize = pageSize });

        Assert.Equal(total, pagina.TotalItems);
        Assert.Equal(paginasEsperadas, pagina.TotalPages);
        Assert.Equal(PartidaId, Assert.Single(pagina.Items).Id);
    }

    [Theory]
    [InlineData(1, 1, "desc", true)]
    [InlineData(1, 50, "asc", true)]
    [InlineData(1_000_000, 10, "desc", true)]
    [InlineData(0, 10, "desc", false)]
    [InlineData(1_000_001, 10, "desc", false)]
    [InlineData(1, 0, "desc", false)]
    [InlineData(1, 51, "desc", false)]
    [InlineData(1, 10, "abc", false)]
    [InlineData(1, 10, "ASC", false)]
    public void ConsultaPartidas_ValidaLimites(int page, int pageSize, string orden, bool esValida)
    {
        var consulta = new ConsultaPartidasRequest { Page = page, PageSize = pageSize, Orden = orden };

        var valida = Validator.TryValidateObject(consulta, new ValidationContext(consulta), null, validateAllProperties: true);

        Assert.Equal(esValida, valida);
    }

    [Fact]
    public void ConsultaPartidas_RechazaEnumsSinDefinir()
    {
        var consulta = new ConsultaPartidasRequest { Estado = (EstadoPartida)99, Dificultad = (Dificultad)99 };
        var errores = new List<ValidationResult>();

        Validator.TryValidateObject(consulta, new ValidationContext(consulta), errores, validateAllProperties: true);

        Assert.Equal(2, errores.Count);
    }

    [Theory]
    [InlineData(Dificultad.Normal)]
    [InlineData(Dificultad.Dificil)]
    public async Task Atacar_VictoriaEnNivelMaximo_AcumulaLaExperienciaDelEnemigoSinSubir(Dificultad dificultad)
    {
        var partida = CrearPartidaEnCombate(4, 3, dificultad, 60, 10);
        partida.Personaje.Nivel = 3;
        partida.Personaje.Experiencia = 20;
        partida.Enemigo.ExperienciaOtorgada = 35;

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(35, respuesta.ExperienciaGanada);
        Assert.Equal(3, partida.Personaje.Nivel);
        Assert.Equal(55, partida.Personaje.Experiencia);
        Assert.Equal(3, respuesta.Estado.PersonajeNivel);
        Assert.Equal(3, respuesta.Estado.PersonajeNivelMaximo);
        Assert.Equal(55, respuesta.Estado.PersonajeExperiencia);
        Assert.Null(respuesta.Estado.PersonajeExperienciaSiguienteNivel);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task ObtenerEstado_InformaSiGolpePoderosoEstaDesbloqueado(int nivel, bool esperado)
    {
        var partida = CrearPartida();
        partida.Personaje.Nivel = nivel;

        var estado = await CrearServicio(new PartidaRepositoryFalso(partida)).ObtenerEstadoAsync(UsuarioId, PartidaId);

        Assert.NotNull(estado);
        Assert.Equal(esperado, estado.GolpePoderosoDesbloqueado);
        Assert.Equal(2, estado.GolpePoderosoNivelRequerido);
    }

    [Fact]
    public async Task GolpePoderoso_EnNivelUno_SeRechazaSinEscribir()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).UsarGolpePoderosoAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.HabilidadBloqueada);
        VerificarSinEscrituras(repositorio, partida, 4, 3);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GolpePoderoso_DesdeNivelDos_DuplicaElAtaque(int nivel)
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        partida.Personaje.Nivel = nivel;

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).UsarGolpePoderosoAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(30, respuesta.DanioInfligido);
        Assert.Equal(10, respuesta.DanioRecibido);
        Assert.Equal(10, partida.VidaEnemigoActual);
        Assert.Equal(70, partida.VidaPersonajeActual);
    }

    [Fact]
    public async Task GolpePoderoso_FueraDeAlcance_SeRechazaSinEscribir()
    {
        var partida = CrearPartidaEnCombate(3, 3, Dificultad.Normal, 80, 40);
        partida.Personaje.Nivel = 2;
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).UsarGolpePoderosoAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.FueraDeAlcance);
        VerificarSinEscrituras(repositorio, partida, 3, 3);
    }

    [Fact]
    public async Task GolpePoderoso_Victoria_OtorgaLaExperienciaDelEnemigo()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 20);
        partida.Personaje.Nivel = 2;
        partida.Enemigo.ExperienciaOtorgada = 20;

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).UsarGolpePoderosoAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(ResultadoCombate.Victoria, respuesta.Estado.ResultadoCombate);
        Assert.Equal(20, respuesta.ExperienciaGanada);
        Assert.Equal(20, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task EndpointGolpePoderoso_Bloqueado_Devuelve409ConMensaje()
    {
        var controlador = CrearControlador(new PartidaRepositoryFalso(
            CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40)));

        var respuesta = await controlador.UsarGolpePoderoso(PartidaId, CancellationToken.None);

        VerificarConflicto(respuesta, "Golpe poderoso bloqueado.", "Golpe poderoso se desbloquea en el nivel 2.");
    }

    [Fact]
    public void EndpointGolpePoderoso_NoRecibeDatosDelCliente()
    {
        var metodo = typeof(PartidasController).GetMethod(nameof(PartidasController.UsarGolpePoderoso))!;

        var ruta = Assert.Single(metodo.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>());
        Assert.Equal("{id:int:min(1)}/golpe-poderoso", ruta.Template);
        Assert.Equal(
            [typeof(int), typeof(CancellationToken)],
            metodo.GetParameters().Select(parametro => parametro.ParameterType));
    }

    [Fact]
    public async Task Atacar_Victoria_SubeDeNivelConservaElSobranteYNoCambiaLasEstadisticas()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        partida.Personaje.Nivel = 1;
        partida.Personaje.Experiencia = 30;
        partida.Personaje.VidaMaxima = 80;
        partida.Personaje.Ataque = 15;
        partida.Personaje.Defensa = 5;

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(2, respuesta.Estado.PersonajeNivel);
        Assert.Equal(30, respuesta.Estado.PersonajeExperiencia);
        Assert.Equal(200, respuesta.Estado.PersonajeExperienciaSiguienteNivel);
        Assert.Equal(80, partida.Personaje.VidaMaxima);
        Assert.Equal(15, partida.Personaje.Ataque);
        Assert.Equal(5, partida.Personaje.Defensa);
    }

    [Fact]
    public async Task Atacar_Victoria_ProcesaMasDeUnaSubidaDeNivel()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        partida.Personaje.Nivel = 1;
        partida.Personaje.Experiencia = 350;

        var resultado = await CrearServicio(new PartidaRepositoryFalso(partida)).AtacarAsync(UsuarioId, PartidaId);

        VerificarAceptado(resultado);
        Assert.Equal(3, partida.Personaje.Nivel);
        Assert.Equal(150, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task Atacar_Derrota_NoOtorgaExperiencia()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 10, 40);
        partida.Personaje.Experiencia = 20;
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        var respuesta = VerificarAceptado(resultado);
        Assert.Equal(ResultadoCombate.Derrota, respuesta.Estado.ResultadoCombate);
        Assert.Equal(0, respuesta.ExperienciaGanada);
        Assert.Null(repositorio.UltimoProgresoSolicitado);
        Assert.Equal(1, partida.Personaje.Nivel);
        Assert.Equal(20, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task Atacar_SinTerminarElCombate_NoOtorgaExperiencia()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 80, 40);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        Assert.Equal(0, VerificarAceptado(resultado).ExperienciaGanada);
        Assert.Null(repositorio.UltimoProgresoSolicitado);
        Assert.Equal(0, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task Finalizar_Abandono_NoOtorgaExperiencia()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        partida.Personaje.Experiencia = 20;
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).FinalizarAsync(UsuarioId, PartidaId);

        Assert.NotNull(resultado);
        Assert.False(resultado.ConcurrenciaPerdida);
        Assert.Null(partida.ResultadoCombate);
        Assert.Equal(1, partida.Personaje.Nivel);
        Assert.Equal(20, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task Atacar_PartidaYaGanada_NoRecompensaDosVeces()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        var repositorio = new PartidaRepositoryFalso(partida);
        var servicio = CrearServicio(repositorio);

        VerificarAceptado(await servicio.AtacarAsync(UsuarioId, PartidaId));
        VerificarRechazo(await servicio.AtacarAsync(UsuarioId, PartidaId), RechazoAtaque.PartidaNoActiva);

        Assert.Equal(1, repositorio.LlamadasAplicarAtaque);
        Assert.Equal(2, partida.Personaje.Nivel);
        Assert.Equal(0, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task Atacar_DosPeticionesSimultaneas_SoloLaPrimeraRecompensa()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        var repositorio = new PartidaRepositoryFalso(partida);
        var servicio = CrearServicio(repositorio);
        var leida = await repositorio.ObtenerEstadoAsync(PartidaId, UsuarioId);
        Assert.NotNull(leida);

        VerificarAceptado(await servicio.AtacarAsync(UsuarioId, PartidaId));
        var filas = await repositorio.AplicarAtaqueAsync(leida, UsuarioId, 60, 0, ResultadoCombate.Victoria, false, new ProgresoPersonaje(2, 0));

        Assert.Equal(0, filas);
        Assert.Equal(2, partida.Personaje.Nivel);
        Assert.Equal(0, partida.Personaje.Experiencia);
    }

    [Fact]
    public async Task Atacar_OtraVictoriaCambioElPersonaje_SeRechazaSinFinalizarNiPerderExperiencia()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Normal, 60, 10);
        var repositorio = new PartidaRepositoryFalso(partida)
        {
            AntesDeAplicarAtaque = () => partida.Personaje.Experiencia = 50
        };

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarRechazo(resultado, RechazoAtaque.EstadoObsoleto);
        Assert.Equal(EstadoPartida.Activa, partida.Estado);
        Assert.Equal(10, partida.VidaEnemigoActual);
        Assert.Equal(1, partida.Personaje.Nivel);
        Assert.Equal(50, partida.Personaje.Experiencia);
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
    public async Task Atacar_VictoriaEnDificil_ConservaElDesbloqueo()
    {
        var partida = CrearPartidaEnCombate(4, 3, Dificultad.Dificil, 60, 10);
        var repositorio = new PartidaRepositoryFalso(partida);

        var resultado = await CrearServicio(repositorio).AtacarAsync(UsuarioId, PartidaId);

        VerificarAceptado(resultado);
        Assert.Equal(Dificultad.Dificil, partida.Personaje.DificultadDesbloqueada);
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

        var autorizacion = Assert.Single(typeof(PartidasController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(RolesUsuario.Jugador, autorizacion.Roles);
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
        new(repositorio, null!, null!, NullLogger<PartidaService>.Instance);

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

    // Valores simples para calcular a mano: sin defensas, cada ataque aplica todo su daño.
    // Las reglas con los valores reales del catálogo se prueban en ReglasCombateTests.
    private static Partida CrearPartida() => new()
    {
        Id = PartidaId,
        Estado = EstadoPartida.Activa,
        Personaje = new Personaje { Nombre = "Bruma", Nivel = 1, UsuarioId = UsuarioId, VidaMaxima = 80, Ataque = 15, Defensa = 0 },
        Enemigo = new Enemigo { Nombre = "Goblin", VidaMaxima = 40, Ataque = 10, Defensa = 0, ExperienciaOtorgada = 100 },
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
            0,
            3,
            100,
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
            null,
            false,
            2),
        15,
        10,
        0);

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

        public ProgresoPersonaje? UltimoProgresoSolicitado { get; private set; }

        public Task<int> AplicarAtaqueAsync(
            Partida leida,
            int usuarioId,
            int vidaPersonaje,
            int vidaEnemigo,
            ResultadoCombate? resultado,
            bool desbloquearDificil,
            ProgresoPersonaje? progreso = null,
            CancellationToken cancellationToken = default)
        {
            LlamadasAplicarAtaque++;
            UltimoDesbloqueoSolicitado = desbloquearDificil;
            UltimoProgresoSolicitado = progreso;
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

            // Equivale a la reversión de la transacción: si el personaje cambió, no se aplica nada.
            if (progreso is not null
                && (actual.Personaje.Nivel != leida.Personaje.Nivel
                    || actual.Personaje.Experiencia != leida.Personaje.Experiencia))
            {
                return Task.FromResult(0);
            }

            if (progreso is not null)
            {
                actual.Personaje.Nivel = progreso.Nivel;
                actual.Personaje.Experiencia = progreso.Experiencia;
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
            Personaje = new Personaje
            {
                Nombre = origen.Personaje.Nombre,
                Nivel = origen.Personaje.Nivel,
                Experiencia = origen.Personaje.Experiencia,
                VidaMaxima = origen.Personaje.VidaMaxima,
                Ataque = origen.Personaje.Ataque,
                Defensa = origen.Personaje.Defensa,
                UsuarioId = origen.Personaje.UsuarioId,
                DificultadDesbloqueada = origen.Personaje.DificultadDesbloqueada
            },
            EnemigoId = origen.EnemigoId,
            Enemigo = origen.Enemigo,
            VidaPersonajeActual = origen.VidaPersonajeActual,
            VidaEnemigoActual = origen.VidaEnemigoActual,
            PersonajeX = origen.PersonajeX,
            PersonajeY = origen.PersonajeY,
            EnemigoX = origen.EnemigoX,
            EnemigoY = origen.EnemigoY
        };

        public (int UsuarioId, EstadoPartida? Estado, Dificultad? Dificultad, bool Ascendente, int Page, int PageSize)? UltimaConsulta { get; private set; }

        public int TotalListado { get; init; }

        public Task<(IReadOnlyList<Partida> Partidas, int Total)> ObtenerPorUsuarioAsync(
            int usuarioId,
            EstadoPartida? estado,
            Dificultad? dificultad,
            bool ascendente,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            UltimaConsulta = (usuarioId, estado, dificultad, ascendente, page, pageSize);
            IReadOnlyList<Partida> partidas = partida is null ? [] : [partida];
            return Task.FromResult((partidas, TotalListado));
        }

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

        public Task<AtaquePartidaResult?> UsarGolpePoderosoAsync(
            int usuarioId,
            int partidaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AtaquePartidaResult?>(resultado);

        public Task<CrearPartidaResult?> CrearAsync(
            int usuarioId,
            CrearPartidaRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PaginaPartidasResponse> ObtenerPorUsuarioAsync(
            int usuarioId,
            ConsultaPartidasRequest consulta,
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

        public Task<int> EliminarSinPartidasAsync(int id, int usuarioId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class EnemigoRepositoryFalso(bool sinEnemigos = false) : IEnemigoRepository
    {
        public Task<Enemigo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Enemigo?> ObtenerPorNivelAsync(int nivel, CancellationToken cancellationToken = default) =>
            Task.FromResult(sinEnemigos ? null : DatosIniciales.Enemigos().FirstOrDefault(enemigo => enemigo.Nivel == nivel));

        public Task<IReadOnlyList<Enemigo>> ObtenerTodosAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Enemigo> CrearAsync(Enemigo enemigo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
