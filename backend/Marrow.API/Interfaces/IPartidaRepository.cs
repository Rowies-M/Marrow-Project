using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IPartidaRepository
{
    Task<Partida?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Partida>> ObtenerPorPersonajeAsync(int personajeId, CancellationToken cancellationToken = default);

    Task<Partida> CrearAsync(Partida partida, CancellationToken cancellationToken = default);

    Task<Partida> ActualizarAsync(Partida partida, CancellationToken cancellationToken = default);
}
