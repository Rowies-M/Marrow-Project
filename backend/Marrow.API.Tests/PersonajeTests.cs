using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
    [InlineData(200, 100, 10)]
    [InlineData(-1, -1, -1)]
    public async Task Crear_IgnoraLasEstadisticasEnviadasPorElCliente(int vidaActual, int vidaMaxima, int ataque)
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());
        var request = DeserializarCrear($$"""
            {
              "nombre": "Bruma",
              "nivel": 99,
              "experiencia": 999,
              "vidaActual": {{vidaActual}},
              "vidaMaxima": {{vidaMaxima}},
              "ataque": {{ataque}},
              "defensa": 77,
              "clase": 99
            }
            """);

        Assert.Empty(ValidarAnotaciones(request));
        var respuesta = await servicio.CrearAsync(UsuarioId, request);

        Assert.Equal(1, respuesta.Nivel);
        Assert.Equal(0, respuesta.Experiencia);
        Assert.Equal(80, respuesta.VidaActual);
        Assert.Equal(80, respuesta.VidaMaxima);
        Assert.Equal(15, respuesta.Ataque);
        Assert.Equal(5, respuesta.Defensa);
        Assert.Equal(ClasePersonaje.Guerrero, respuesta.Clase);
        Assert.NotNull(repositorio.Guardado);
        Assert.Equal(1, repositorio.Guardado.Nivel);
        Assert.Equal(0, repositorio.Guardado.Experiencia);
        Assert.Equal(80, repositorio.Guardado.VidaActual);
        Assert.Equal(80, repositorio.Guardado.VidaMaxima);
        Assert.Equal(15, repositorio.Guardado.Ataque);
        Assert.Equal(5, repositorio.Guardado.Defensa);
    }

    [Fact]
    public async Task Crear_RecortaElNombreYAsignaElUsuario()
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        var respuesta = await servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest { Nombre = "  Bruma  " });

        Assert.Equal("Bruma", respuesta.Nombre);
        Assert.NotNull(repositorio.Guardado);
        Assert.Equal(UsuarioId, repositorio.Guardado.UsuarioId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_NombreInvalido_LanzaErrorSinGuardar(string nombre)
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(
            UsuarioId,
            new CrearPersonajeRequest { Nombre = nombre }));

        Assert.Null(repositorio.Guardado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void CrearRequest_ValidaLaLongitudDelNombre(int longitud)
    {
        var request = new CrearPersonajeRequest { Nombre = new string('a', longitud) };

        Assert.NotEmpty(ValidarAnotaciones(request));
    }

    [Theory]
    [InlineData(typeof(CrearPersonajeRequest))]
    [InlineData(typeof(ActualizarPersonajeRequest))]
    public void RequestDePersonaje_SoloAceptaElNombre(Type tipoRequest)
    {
        var propiedad = Assert.Single(tipoRequest.GetProperties());

        Assert.Equal(nameof(Personaje.Nombre), propiedad.Name);
    }

    [Fact]
    public async Task Crear_EmpiezaEnDificultadNormal()
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest { Nombre = "Bruma" });

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

        await servicio.ActualizarAsync(
            UsuarioId,
            PersonajeId,
            new ActualizarPersonajeRequest { Nombre = "Nuevo nombre" });

        Assert.NotNull(repositorio.Guardado);
        Assert.Equal("Nuevo nombre", repositorio.Guardado.Nombre);
        Assert.Equal(Dificultad.Dificil, repositorio.Guardado.DificultadDesbloqueada);
    }

    [Fact]
    public async Task Actualizar_SoloCambiaElNombreAunqueElClienteEnvieEstadisticas()
    {
        var repositorio = new PersonajeRepositoryFalso
        {
            Existente = new Personaje
            {
                Id = PersonajeId,
                Nombre = "Bruma",
                Nivel = 3,
                Experiencia = 40,
                VidaActual = 50,
                VidaMaxima = 80,
                Ataque = 15,
                Defensa = 5,
                Clase = ClasePersonaje.Guerrero,
                UsuarioId = UsuarioId
            }
        };
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());
        var request = JsonSerializer.Deserialize<ActualizarPersonajeRequest>(
            """
            {
              "nombre": "  Nuevo nombre  ",
              "nivel": 99,
              "experiencia": 999,
              "vidaActual": 999,
              "vidaMaxima": 999,
              "ataque": 999,
              "defensa": 999,
              "clase": 99
            }
            """,
            JsonSerializerOptions.Web)!;

        var respuesta = await servicio.ActualizarAsync(UsuarioId, PersonajeId, request);

        Assert.NotNull(respuesta);
        Assert.Equal("Nuevo nombre", respuesta.Nombre);
        Assert.Equal(3, respuesta.Nivel);
        Assert.Equal(40, respuesta.Experiencia);
        Assert.Equal(50, respuesta.VidaActual);
        Assert.Equal(80, respuesta.VidaMaxima);
        Assert.Equal(15, respuesta.Ataque);
        Assert.Equal(5, respuesta.Defensa);
        Assert.Equal(ClasePersonaje.Guerrero, respuesta.Clase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Actualizar_NombreInvalido_LanzaErrorSinGuardar(string nombre)
    {
        var repositorio = new PersonajeRepositoryFalso { Existente = CrearExistente() };
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ActualizarAsync(
            UsuarioId,
            PersonajeId,
            new ActualizarPersonajeRequest { Nombre = nombre }));

        Assert.Null(repositorio.Guardado);
    }

    [Fact]
    public async Task Actualizar_PersonajeInexistenteOAjeno_DevuelveNull()
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        var respuesta = await servicio.ActualizarAsync(
            UsuarioId,
            PersonajeId,
            new ActualizarPersonajeRequest { Nombre = "Bruma" });

        Assert.Null(respuesta);
        Assert.Null(repositorio.Guardado);
    }

    [Fact]
    public async Task RepositorioActualizar_SoloEscribeElNombre()
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
        Assert.Equal([nameof(Personaje.Nombre)], interceptor.PropiedadesModificadas);
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

    // Usa las mismas opciones JSON que ASP.NET Core aplica por defecto a los controladores.
    private static CrearPersonajeRequest DeserializarCrear(string json) =>
        JsonSerializer.Deserialize<CrearPersonajeRequest>(json, JsonSerializerOptions.Web)!;

    private static List<ValidationResult> ValidarAnotaciones(CrearPersonajeRequest request)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), resultados, validateAllProperties: true);
        return resultados;
    }

    private static Personaje CrearExistente() => new()
    {
        Id = PersonajeId,
        Nombre = "Bruma",
        Nivel = 1,
        UsuarioId = UsuarioId
    };

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
