using Marrow.API.DTOs;

namespace Marrow.API.Interfaces;

public interface IPartidaService
{
    Task<PartidaResponse?> CrearAsync(
        int usuarioId,
        CrearPartidaRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartidaResponse>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<PartidaResponse?> ObtenerPorIdAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default);

    Task<EstadoPartidaResponse?> ObtenerEstadoAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default);

    Task<MovimientoPartidaResult?> MoverAsync(
        int usuarioId,
        int partidaId,
        MoverPartidaRequest request,
        CancellationToken cancellationToken = default);

    Task<FinalizacionPartidaResult?> FinalizarAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default);
}
