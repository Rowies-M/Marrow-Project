using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IPartidaRepository
{
    Task<Partida?> ObtenerPorIdAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<Partida?> ObtenerEstadoAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Partida>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<Partida> CrearAsync(Partida partida, CancellationToken cancellationToken = default);

    Task<int> MoverAsync(
        int id,
        int usuarioId,
        int deltaX,
        int deltaY,
        CancellationToken cancellationToken = default);

    Task<int> FinalizarAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
