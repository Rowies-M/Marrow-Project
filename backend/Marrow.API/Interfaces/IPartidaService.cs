using Marrow.API.DTOs;

namespace Marrow.API.Interfaces;

public interface IPartidaService
{
    Task<CrearPartidaResult?> CrearAsync(
        int usuarioId,
        CrearPartidaRequest request,
        CancellationToken cancellationToken = default);

    Task<PaginaPartidasResponse> ObtenerPorUsuarioAsync(
        int usuarioId,
        ConsultaPartidasRequest consulta,
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

    Task<AtaquePartidaResult?> AtacarAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default);

    Task<AtaquePartidaResult?> UsarGolpePoderosoAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default);
}
