using System.Security.Claims;
using Marrow.API.Controllers;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Marrow.API.Services;
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

    // MoverAsync y FinalizarAsync no usan los repositorios de personajes ni de enemigos.
    private static PartidaService CrearServicio(IPartidaRepository repositorio) =>
        new(repositorio, null!, null!);

    private static PartidasController CrearControlador(IPartidaRepository repositorio)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, UsuarioId.ToString()) };
        return new PartidasController(CrearServicio(repositorio))
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

    /// <summary>Reproduce en memoria las condiciones y los resultados de los UPDATE del repositorio real.</summary>
    private sealed class PartidaRepositoryFalso(Partida? partida) : IPartidaRepository
    {
        public Action? DespuesDeMover { get; init; }

        public bool LecturaDevuelveNull { get; init; }

        public Partida? Creada { get; private set; }

        public Task<Partida?> ObtenerPorIdAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LecturaDevuelveNull ? null : Buscar(id, usuarioId));

        public Task<Partida?> ObtenerEstadoAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LecturaDevuelveNull ? null : Buscar(id, usuarioId));

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
            if (activa?.Estado != EstadoPartida.Activa)
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

    private sealed class EnemigoRepositoryFalso : IEnemigoRepository
    {
        public Task<Enemigo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Enemigo?>(new Enemigo { Id = id, Nombre = "Goblin", VidaMaxima = 30 });

        public Task<IReadOnlyList<Enemigo>> ObtenerTodosAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Enemigo> CrearAsync(Enemigo enemigo, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
