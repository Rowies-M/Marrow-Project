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
              "nivel": 1,
              "experiencia": 0,
              "vidaActual": {{vidaActual}},
              "vidaMaxima": {{vidaMaxima}},
              "ataque": {{ataque}},
              "defensa": 5,
              "clase": 0
            }
            """);

        Assert.Empty(ValidarAnotaciones(request));
        var respuesta = await servicio.CrearAsync(UsuarioId, request);

        Assert.Equal(80, respuesta.VidaActual);
        Assert.Equal(80, respuesta.VidaMaxima);
        Assert.Equal(15, respuesta.Ataque);
        Assert.NotNull(repositorio.Guardado);
        Assert.Equal(80, repositorio.Guardado.VidaActual);
        Assert.Equal(80, repositorio.Guardado.VidaMaxima);
        Assert.Equal(15, repositorio.Guardado.Ataque);
    }

    [Fact]
    public async Task Crear_SinEnviarEstadisticas_AsignaLosValoresDelBackend()
    {
        var servicio = new PersonajeService(new PersonajeRepositoryFalso(), new UsuarioRepositoryFalso());

        var respuesta = await servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest
        {
            Nombre = "Bruma",
            Nivel = 1,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.Equal(80, respuesta.VidaActual);
        Assert.Equal(80, respuesta.VidaMaxima);
        Assert.Equal(15, respuesta.Ataque);
    }

    [Fact]
    public async Task Crear_ConservaLosDemasCampos()
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        var respuesta = await servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest
        {
            Nombre = "  Bruma  ",
            Nivel = 3,
            Experiencia = 40,
            Defensa = 5,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.Equal("Bruma", respuesta.Nombre);
        Assert.Equal(3, respuesta.Nivel);
        Assert.Equal(40, respuesta.Experiencia);
        Assert.Equal(5, respuesta.Defensa);
        Assert.Equal(ClasePersonaje.Guerrero, respuesta.Clase);
        Assert.NotNull(repositorio.Guardado);
        Assert.Equal(UsuarioId, repositorio.Guardado.UsuarioId);
    }

    [Theory]
    [InlineData("   ", 1, 0, 5, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 0, 0, 5, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 1, -1, 5, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 1, 0, -1, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 1, 0, 5, (ClasePersonaje)99)]
    public async Task Crear_DatosInvalidos_LanzaErrorSinGuardar(
        string nombre,
        int nivel,
        int experiencia,
        int defensa,
        ClasePersonaje clase)
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearAsync(UsuarioId, new CrearPersonajeRequest
        {
            Nombre = nombre,
            Nivel = nivel,
            Experiencia = experiencia,
            Defensa = defensa,
            Clase = clase
        }));

        Assert.Null(repositorio.Guardado);
    }

    [Theory]
    [InlineData("""{ "nombre": "", "nivel": 1, "clase": 0 }""")]
    [InlineData("""{ "nombre": "Bruma", "nivel": 0, "clase": 0 }""")]
    [InlineData("""{ "nombre": "Bruma", "nivel": 1, "experiencia": -1, "clase": 0 }""")]
    [InlineData("""{ "nombre": "Bruma", "nivel": 1, "defensa": -1, "clase": 0 }""")]
    [InlineData("""{ "nombre": "Bruma", "nivel": 1, "clase": 99 }""")]
    public void CrearRequest_MantieneLasAnotacionesDeLosDemasCampos(string json)
    {
        Assert.NotEmpty(ValidarAnotaciones(DeserializarCrear(json)));
    }

    [Theory]
    [InlineData(nameof(ActualizarPersonajeRequest.VidaActual))]
    [InlineData(nameof(ActualizarPersonajeRequest.VidaMaxima))]
    [InlineData(nameof(ActualizarPersonajeRequest.Ataque))]
    public void Requests_SoloElDeActualizacionAceptaEstadisticas(string propiedad)
    {
        Assert.Null(typeof(CrearPersonajeRequest).GetProperty(propiedad));
        Assert.NotNull(typeof(ActualizarPersonajeRequest).GetProperty(propiedad));
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
    public async Task Actualizar_GuardaLosValoresEnviados()
    {
        var repositorio = new PersonajeRepositoryFalso { Existente = CrearExistente() };
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        var respuesta = await servicio.ActualizarAsync(UsuarioId, PersonajeId, new ActualizarPersonajeRequest
        {
            Nombre = "  Nuevo nombre  ",
            Nivel = 2,
            Experiencia = 30,
            VidaActual = 90,
            VidaMaxima = 110,
            Ataque = 12,
            Defensa = 7,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.NotNull(respuesta);
        Assert.Equal("Nuevo nombre", respuesta.Nombre);
        Assert.Equal(2, respuesta.Nivel);
        Assert.Equal(30, respuesta.Experiencia);
        Assert.Equal(90, respuesta.VidaActual);
        Assert.Equal(110, respuesta.VidaMaxima);
        Assert.Equal(12, respuesta.Ataque);
        Assert.Equal(7, respuesta.Defensa);
    }

    [Theory]
    [InlineData(-1, 100, 10)]
    [InlineData(50, -1, 10)]
    [InlineData(50, 100, -1)]
    [InlineData(101, 100, 10)]
    public async Task Actualizar_EstadisticasInvalidas_LanzaErrorSinGuardar(int vidaActual, int vidaMaxima, int ataque)
    {
        var repositorio = new PersonajeRepositoryFalso { Existente = CrearExistente() };
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ActualizarAsync(
            UsuarioId,
            PersonajeId,
            new ActualizarPersonajeRequest
            {
                Nombre = "Bruma",
                Nivel = 1,
                VidaActual = vidaActual,
                VidaMaxima = vidaMaxima,
                Ataque = ataque,
                Clase = ClasePersonaje.Guerrero
            }));

        Assert.Null(repositorio.Guardado);
    }

    [Theory]
    [InlineData("   ", 1, 0, 5, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 0, 0, 5, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 1, -1, 5, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 1, 0, -1, ClasePersonaje.Guerrero)]
    [InlineData("Bruma", 1, 0, 5, (ClasePersonaje)99)]
    public async Task Actualizar_DatosComunesInvalidos_LanzaErrorSinGuardar(
        string nombre,
        int nivel,
        int experiencia,
        int defensa,
        ClasePersonaje clase)
    {
        var repositorio = new PersonajeRepositoryFalso { Existente = CrearExistente() };
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ActualizarAsync(
            UsuarioId,
            PersonajeId,
            new ActualizarPersonajeRequest
            {
                Nombre = nombre,
                Nivel = nivel,
                Experiencia = experiencia,
                Defensa = defensa,
                Clase = clase
            }));

        Assert.Null(repositorio.Guardado);
    }

    [Fact]
    public async Task Actualizar_PersonajeInexistenteOAjeno_DevuelveNull()
    {
        var repositorio = new PersonajeRepositoryFalso();
        var servicio = new PersonajeService(repositorio, new UsuarioRepositoryFalso());

        var respuesta = await servicio.ActualizarAsync(UsuarioId, PersonajeId, new ActualizarPersonajeRequest
        {
            Nombre = "Bruma",
            Nivel = 1,
            Clase = ClasePersonaje.Guerrero
        });

        Assert.Null(respuesta);
        Assert.Null(repositorio.Guardado);
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
