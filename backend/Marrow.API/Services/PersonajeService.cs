using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public class PersonajeService(
    IPersonajeRepository personajeRepository,
    IUsuarioRepository usuarioRepository) : IPersonajeService
{
    public Task<Personaje?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        return personajeRepository.ObtenerPorIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Personaje>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        return personajeRepository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
    }

    public async Task<Personaje> CrearAsync(Personaje personaje, CancellationToken cancellationToken = default)
    {
        ValidarPersonaje(personaje);
        await ValidarUsuarioAsync(personaje.UsuarioId, cancellationToken);
        return await personajeRepository.CrearAsync(personaje, cancellationToken);
    }

    public async Task<Personaje> ActualizarAsync(Personaje personaje, CancellationToken cancellationToken = default)
    {
        ValidarPersonaje(personaje);
        ValidarId(personaje.Id, nameof(personaje.Id));
        await ValidarUsuarioAsync(personaje.UsuarioId, cancellationToken);

        if (await personajeRepository.ObtenerPorIdAsync(personaje.Id, cancellationToken) is null)
        {
            throw new KeyNotFoundException("El personaje no existe.");
        }

        return await personajeRepository.ActualizarAsync(personaje, cancellationToken);
    }

    private async Task ValidarUsuarioAsync(int usuarioId, CancellationToken cancellationToken)
    {
        ValidarId(usuarioId, nameof(usuarioId));

        if (await usuarioRepository.ObtenerPorIdAsync(usuarioId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("El usuario no existe.");
        }
    }

    private static void ValidarPersonaje(Personaje personaje)
    {
        ArgumentNullException.ThrowIfNull(personaje);

        if (string.IsNullOrWhiteSpace(personaje.Nombre))
        {
            throw new ArgumentException("El nombre del personaje es obligatorio.", nameof(personaje));
        }

        if (personaje.Nivel <= 0 || personaje.Experiencia < 0 || personaje.VidaActual < 0 ||
            personaje.VidaMaxima < 0 || personaje.Ataque < 0 || personaje.Defensa < 0 ||
            personaje.VidaActual > personaje.VidaMaxima)
        {
            throw new ArgumentException("Las estadísticas del personaje no son válidas.", nameof(personaje));
        }

        if (!Enum.IsDefined(personaje.Clase))
        {
            throw new ArgumentException("La clase del personaje no es válida.", nameof(personaje));
        }
    }

    private static void ValidarId(int id, string parameterName = "id")
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", parameterName);
        }
    }
}
