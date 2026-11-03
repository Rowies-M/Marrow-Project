using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public sealed class PersonajeService(
    IPersonajeRepository personajeRepository,
    IUsuarioRepository usuarioRepository) : IPersonajeService
{
    public async Task<PersonajeResponse> CrearAsync(
        int usuarioId,
        CrearPersonajeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ArgumentNullException.ThrowIfNull(request);
        ValidarRequest(request);
        await ValidarUsuarioAsync(usuarioId, cancellationToken);

        var personaje = new Personaje
        {
            Nombre = request.Nombre.Trim(),
            Nivel = request.Nivel,
            Experiencia = request.Experiencia,
            VidaActual = request.VidaActual,
            VidaMaxima = request.VidaMaxima,
            Ataque = request.Ataque,
            Defensa = request.Defensa,
            Clase = request.Clase,
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
        ValidarRequest(request);

        var personaje = await personajeRepository.ObtenerPorIdAsync(personajeId, usuarioId, cancellationToken);
        if (personaje is null)
        {
            return null;
        }

        personaje.Nombre = request.Nombre.Trim();
        personaje.Nivel = request.Nivel;
        personaje.Experiencia = request.Experiencia;
        personaje.VidaActual = request.VidaActual;
        personaje.VidaMaxima = request.VidaMaxima;
        personaje.Ataque = request.Ataque;
        personaje.Defensa = request.Defensa;
        personaje.Clase = request.Clase;

        var personajeActualizado = await personajeRepository.ActualizarAsync(personaje, cancellationToken);
        return MapearRespuesta(personajeActualizado);
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
        personaje.Clase);

    private static void ValidarRequest(CrearPersonajeRequest request) =>
        ValidarDatos(request.Nombre, request.Nivel, request.Experiencia, request.VidaActual,
            request.VidaMaxima, request.Ataque, request.Defensa, request.Clase);

    private static void ValidarRequest(ActualizarPersonajeRequest request) =>
        ValidarDatos(request.Nombre, request.Nivel, request.Experiencia, request.VidaActual,
            request.VidaMaxima, request.Ataque, request.Defensa, request.Clase);

    private static void ValidarDatos(
        string nombre,
        int nivel,
        int experiencia,
        int vidaActual,
        int vidaMaxima,
        int ataque,
        int defensa,
        ClasePersonaje clase)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del personaje es obligatorio.", nameof(nombre));
        }

        if (nivel <= 0 || experiencia < 0 || vidaActual < 0 || vidaMaxima < 0 ||
            ataque < 0 || defensa < 0 || vidaActual > vidaMaxima)
        {
            throw new ArgumentException("Las estadísticas del personaje no son válidas.");
        }

        if (clase != ClasePersonaje.Guerrero)
        {
            throw new ArgumentException("La única clase disponible es Guerrero.", nameof(clase));
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
