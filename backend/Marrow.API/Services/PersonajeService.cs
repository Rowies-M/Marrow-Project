using Marrow.API.Data;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public sealed class PersonajeService(
    IPersonajeRepository personajeRepository,
    IUsuarioRepository usuarioRepository) : IPersonajeService
{
    private const int ExperienciaInicial = 0;

    public async Task<PersonajeResponse> CrearAsync(
        int usuarioId,
        CrearPersonajeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ArgumentNullException.ThrowIfNull(request);
        ValidarNombre(request.Nombre);
        await ValidarUsuarioAsync(usuarioId, cancellationToken);

        var personaje = new Personaje
        {
            Nombre = request.Nombre.Trim(),
            Nivel = ReglasProgresion.NivelMinimo,
            Experiencia = ExperienciaInicial,
            VidaActual = DatosIniciales.VidaInicialPersonaje,
            VidaMaxima = DatosIniciales.VidaInicialPersonaje,
            Ataque = DatosIniciales.AtaqueInicialPersonaje,
            Defensa = DatosIniciales.DefensaInicialPersonaje,
            Clase = ClasePersonaje.Guerrero,
            DificultadDesbloqueada = Dificultad.Normal,
            UsuarioId = usuarioId
        };

        var personajeCreado = await personajeRepository.CrearAsync(personaje, cancellationToken);
        return MapearRespuesta(personajeCreado);
    }

    public async Task<IReadOnlyList<PersonajeResponse>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        var personajes = await personajeRepository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
        return personajes.Select(MapearRespuesta).ToList();
    }

    public async Task<PersonajeResponse?> ObtenerPorIdAsync(
        int usuarioId,
        int personajeId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(personajeId, nameof(personajeId));

        var personaje = await personajeRepository.ObtenerPorIdAsync(personajeId, usuarioId, cancellationToken);
        return personaje is null ? null : MapearRespuesta(personaje);
    }

    public async Task<PersonajeResponse?> ActualizarAsync(
        int usuarioId,
        int personajeId,
        ActualizarPersonajeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(personajeId, nameof(personajeId));
        ArgumentNullException.ThrowIfNull(request);
        ValidarNombre(request.Nombre);

        var personaje = await personajeRepository.ObtenerPorIdAsync(personajeId, usuarioId, cancellationToken);
        if (personaje is null)
        {
            return null;
        }

        personaje.Nombre = request.Nombre.Trim();

        var personajeActualizado = await personajeRepository.ActualizarAsync(personaje, cancellationToken);
        return MapearRespuesta(personajeActualizado);
    }

    public async Task<ResultadoEliminacionPersonaje> EliminarAsync(
        int usuarioId,
        int personajeId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(personajeId, nameof(personajeId));

        var filasEliminadas = await personajeRepository.EliminarSinPartidasAsync(personajeId, usuarioId, cancellationToken);
        if (filasEliminadas > 0)
        {
            return ResultadoEliminacionPersonaje.Eliminado;
        }

        var personaje = await personajeRepository.ObtenerPorIdAsync(personajeId, usuarioId, cancellationToken);
        return personaje is null
            ? ResultadoEliminacionPersonaje.NoEncontrado
            : ResultadoEliminacionPersonaje.TienePartidas;
    }

    private async Task ValidarUsuarioAsync(int usuarioId, CancellationToken cancellationToken)
    {
        if (await usuarioRepository.ObtenerPorIdAsync(usuarioId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("El usuario no existe.");
        }
    }

    private static PersonajeResponse MapearRespuesta(Personaje personaje) => new(
        personaje.Id,
        personaje.Nombre,
        personaje.Nivel,
        personaje.Experiencia,
        personaje.VidaActual,
        personaje.VidaMaxima,
        personaje.Ataque,
        personaje.Defensa,
        personaje.Clase,
        ReglasProgresion.NivelMaximo,
        personaje.DificultadDesbloqueada);

    private static void ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del personaje es obligatorio.", nameof(nombre));
        }
    }

    private static void ValidarId(int id, string parameterName)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", parameterName);
        }
    }
}
