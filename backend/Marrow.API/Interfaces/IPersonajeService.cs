using Marrow.API.DTOs;

namespace Marrow.API.Interfaces;

public interface IPersonajeService
{
    Task<PersonajeResponse> CrearAsync(
        int usuarioId,
        CrearPersonajeRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonajeResponse>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<PersonajeResponse?> ObtenerPorIdAsync(
        int usuarioId,
        int personajeId,
        CancellationToken cancellationToken = default);

    Task<PersonajeResponse?> ActualizarAsync(
        int usuarioId,
        int personajeId,
        ActualizarPersonajeRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultadoEliminacionPersonaje> EliminarAsync(
        int usuarioId,
        int personajeId,
        CancellationToken cancellationToken = default);
}
