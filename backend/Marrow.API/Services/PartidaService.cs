using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public sealed class PartidaService(
    IPartidaRepository partidaRepository,
    IPersonajeRepository personajeRepository) : IPartidaService
{
    public async Task<PartidaResponse?> CrearAsync(
        int usuarioId,
        CrearPartidaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ArgumentNullException.ThrowIfNull(request);
        ValidarId(request.PersonajeId, nameof(request.PersonajeId));

        var personaje = await personajeRepository.ObtenerPorIdAsync(
            request.PersonajeId,
            usuarioId,
            cancellationToken);

        if (personaje is null)
        {
            return null;
        }

        var ahora = DateTime.UtcNow;
        var partida = new Partida
        {
            FechaCreacion = ahora,
            FechaUltimaActualizacion = ahora,
            Estado = EstadoPartida.Activa,
            PersonajeId = personaje.Id
        };

        var partidaCreada = await partidaRepository.CrearAsync(partida, cancellationToken);
        return MapearRespuesta(partidaCreada);
    }

    public async Task<IReadOnlyList<PartidaResponse>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        var partidas = await partidaRepository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
        return partidas.Select(MapearRespuesta).ToList();
    }

    public async Task<PartidaResponse?> ObtenerPorIdAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));

        var partida = await partidaRepository.ObtenerPorIdAsync(partidaId, usuarioId, cancellationToken);
        return partida is null ? null : MapearRespuesta(partida);
    }

    public async Task<PartidaResponse?> FinalizarAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));

        var partida = await partidaRepository.ObtenerPorIdAsync(partidaId, usuarioId, cancellationToken);
        if (partida is null)
        {
            return null;
        }

        if (partida.Estado == EstadoPartida.Finalizada)
        {
            return MapearRespuesta(partida);
        }

        partida.Estado = EstadoPartida.Finalizada;
        partida.FechaUltimaActualizacion = DateTime.UtcNow;
        var partidaActualizada = await partidaRepository.ActualizarAsync(partida, cancellationToken);
        return MapearRespuesta(partidaActualizada);
    }

    private static PartidaResponse MapearRespuesta(Partida partida) => new(
        partida.Id,
        new DateTimeOffset(DateTime.SpecifyKind(partida.FechaCreacion, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(partida.FechaUltimaActualizacion, DateTimeKind.Utc)),
        partida.Estado,
        partida.PersonajeId);

    private static void ValidarId(int id, string parameterName)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", parameterName);
        }
    }
}
