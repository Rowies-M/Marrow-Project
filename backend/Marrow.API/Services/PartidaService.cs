using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public class PartidaService(
    IPartidaRepository partidaRepository,
    IPersonajeRepository personajeRepository) : IPartidaService
{
    public Task<Partida?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        return partidaRepository.ObtenerPorIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Partida>> ObtenerPorPersonajeAsync(int personajeId, CancellationToken cancellationToken = default)
    {
        ValidarId(personajeId, nameof(personajeId));
        return partidaRepository.ObtenerPorPersonajeAsync(personajeId, cancellationToken);
    }

    public async Task<Partida> CrearAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        await ValidarPartidaAsync(partida, cancellationToken);

        var ahora = DateTime.UtcNow;
        partida.FechaCreacion = partida.FechaCreacion == default ? ahora : partida.FechaCreacion;
        partida.FechaUltimaActualizacion = partida.FechaUltimaActualizacion == default
            ? partida.FechaCreacion
            : partida.FechaUltimaActualizacion;

        return await partidaRepository.CrearAsync(partida, cancellationToken);
    }

    public async Task<Partida> ActualizarAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        await ValidarPartidaAsync(partida, cancellationToken);
        ValidarId(partida.Id, nameof(partida.Id));

        if (await partidaRepository.ObtenerPorIdAsync(partida.Id, cancellationToken) is null)
        {
            throw new KeyNotFoundException("La partida no existe.");
        }

        partida.FechaUltimaActualizacion = DateTime.UtcNow;
        return await partidaRepository.ActualizarAsync(partida, cancellationToken);
    }

    private async Task ValidarPartidaAsync(Partida partida, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(partida);
        ValidarId(partida.PersonajeId, nameof(partida.PersonajeId));

        if (!Enum.IsDefined(partida.Estado))
        {
            throw new ArgumentException("El estado de la partida no es válido.", nameof(partida));
        }

        if (await personajeRepository.ObtenerPorIdAsync(partida.PersonajeId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("El personaje no existe.");
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
