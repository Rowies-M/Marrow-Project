using Marrow.API.Data;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Marrow.API.Repositories;
using Marrow.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marrow.API.Tests;

public class PersonajeTests
{
    private const int UsuarioId = 42;
    private const int PersonajeId = 7;

    // Nunca se abre esta conexión: el interceptor frena el guardado antes de llegar a la base.
    private const string ConexionInexistente = "Host=127.0.0.1;Port=1;Database=sin_conexion";

    [Theory]
    [InlineData(100, 100, 10)]
    [InlineData(999, 999, 999)]
    [InlineData(1, 1, 0)]
    public async Task Crear_IgnoraLasEstadisticasEnviadasPorElCliente(int vidaActual, int vidaMaxima, int ataque)
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        var respuesta = await servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest
        {
            Nombre = "Bruma",
            Nivel = 1,
            VidaActual = vidaActual,
            VidaMaxima = vidaMaxima,
            Ataque = ataque,
            Defensa = 5,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.Equal(80, respuesta.VidaActual);
        Assert.Equal(80, respuesta.VidaMaxima);
        Assert.Equal(15, respuesta.Ataque);
        Assert.NotNull(repositorio.Guardado);
        Assert.Equal(80, repositorio.Guardado.VidaActual);
        Assert.Equal(80, repositorio.Guardado.VidaMaxima);
        Assert.Equal(15, repositorio.Guardado.Ataque);
    }

    [Fact]
    public async Task Crear_EmpiezaEnDificultadNormal()
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest
        {
            Nombre = "Bruma",
            Nivel = 1,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.NotNull(repositorio.Guardado);
        Assert.Equal(Dificultad.Normal, repositorio.Guardado.DificultadDesbloqueada);
    }

    [Theory]
    [InlineData(typeof(ActualizarPersonajeRequest))]
    [InlineData(typeof(CrearPersonajeRequest))]
    public void RequestDePersonaje_NoAceptaDificultad(Type tipoRequest)
    {
        Assert.DoesNotContain(
            tipoRequest.GetProperties(),
            propiedad => propiedad.Name.Contains("Dificultad", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Actualizar_ConservaLaDificultadDesbloqueada()
    {
        var repositorio = new PersonajeRepositoryFalso
        {
            Existente = new Personaje
            {
                Id = PersonajeId,
                Nombre = "Bruma",
                Nivel = 1,
                UsuarioId = UsuarioId,
                DificultadDesbloqueada = Dificultad.Dificil
            }
        };
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await servicio.ActualizarAsync(UsuarioId, PersonajeId, new ActualizarPersonajeRequest
        {
            Nombre = "Nuevo nombre",
            Nivel = 2,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.NotNull(repositorio.Guardado);
        Assert.Equal("Nuevo nombre", repositorio.Guardado.Nombre);
        Assert.Equal(Dificultad.Dificil, repositorio.Guardado.DificultadDesbloqueada);
    }

    [Fact]
    public async Task RepositorioActualizar_NoMarcaLaDificultadComoModificada()
    {
        var interceptor = new GuardadoInterceptado();
        await using var context = CrearContexto(interceptor);
        var repositorio = new PersonajeRepository(context);

        await repositorio.ActualizarAsync(new Personaje
        {
            Id = PersonajeId,
            Nombre = "Bruma",
            UsuarioId = UsuarioId,
            DificultadDesbloqueada = Dificultad.Normal
        });

        Assert.True(interceptor.Guardo);
        Assert.Contains(nameof(Personaje.Nombre), interceptor.PropiedadesModificadas);
        Assert.Contains(nameof(Personaje.VidaActual), interceptor.PropiedadesModificadas);
        Assert.DoesNotContain(nameof(Personaje.DificultadDesbloqueada), interceptor.PropiedadesModificadas);
    }

    [Theory]
    [InlineData(typeof(Personaje), nameof(Personaje.DificultadDesbloqueada), false)]
    [InlineData(typeof(Partida), nameof(Partida.Dificultad), false)]
    [InlineData(typeof(Partida), nameof(Partida.ResultadoCombate), true)]
    public async Task ModeloEf_GuardaLosEnumsComoEnterosConLaNulabilidadEsperada(
        Type entidad,
        string propiedad,
        bool nullableEsperado)
    {
        await using var context = CrearContexto(new GuardadoInterceptado());
        var modelo = context.GetService<IDesignTimeModel>().Model;

        var columna = modelo.FindEntityType(entidad)?.FindProperty(propiedad);

        Assert.NotNull(columna);
        Assert.Equal("integer", columna.GetColumnType());
        Assert.Equal(nullableEsperado, columna.IsNullable);
    }

    private static MarrowDbContext CrearContexto(GuardadoInterceptado interceptor) =>
        new(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(ConexionInexistente)
            .AddInterceptors(interceptor)
            .Options);

    private sealed class GuardadoInterceptado : SaveChangesInterceptor
    {
        public bool Guardo { get; private set; }

        public List<string> PropiedadesModificadas { get; } = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Guardo = true;
            PropiedadesModificadas.AddRange(eventData.Context!.ChangeTracker.Entries<Personaje>()
                .SelectMany(entrada => entrada.Properties)
                .Where(propiedad => propiedad.IsModified)
                .Select(propiedad => propiedad.Metadata.Name));

            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }

    private sealed class PersonajeRepositoryFalso : IPersonajeRepository
    {
        public Personaje? Existente { get; init; }

        public Personaje? Guardado { get; private set; }

        public Task<Personaje?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Personaje?> ObtenerPorIdAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Existente);

        public Task<IReadOnlyList<Personaje>> ObtenerPorUsuarioAsync(
            int usuarioId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Personaje> CrearAsync(Personaje personaje, CancellationToken cancellationToken = default)
        {
            Guardado = personaje;
            return Task.FromResult(personaje);
        }

        public Task<Personaje> ActualizarAsync(Personaje personaje, CancellationToken cancellationToken = default)
        {
            Guardado = personaje;
            return Task.FromResult(personaje);
        }
    }

    private sealed class UsuarioRepositoryFalso : IUsuarioRepository
    {
        public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Usuario?>(new Usuario { Id = id });

        public Task<Usuario?> ObtenerPorNombreUsuarioAsync(
            string nombreUsuario,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
