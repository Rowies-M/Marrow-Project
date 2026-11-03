using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IPersonajeRepository
{
    Task<Personaje?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Personaje?> ObtenerPorIdAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Personaje>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<Personaje> CrearAsync(Personaje personaje, CancellationToken cancellationToken = default);

    Task<Personaje> ActualizarAsync(Personaje personaje, CancellationToken cancellationToken = default);
}
