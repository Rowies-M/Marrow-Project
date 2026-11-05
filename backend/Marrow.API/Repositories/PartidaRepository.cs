using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class PartidaRepository(MarrowDbContext context) : IPartidaRepository
{
    public Task<Partida?> ObtenerPorIdAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Partidas.AsNoTracking().Include(partida => partida.Personaje)
            .FirstOrDefaultAsync(
                partida => partida.Id == id && partida.Personaje.UsuarioId == usuarioId,
                cancellationToken);

    public Task<Partida?> ObtenerEstadoAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Partidas.AsNoTracking()
            .Include(partida => partida.Personaje)
            .Include(partida => partida.Enemigo)
            .FirstOrDefaultAsync(
                partida => partida.Id == id && partida.Personaje.UsuarioId == usuarioId,
                cancellationToken);

    public async Task<IReadOnlyList<Partida>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        await context.Partidas.AsNoTracking()
            .Where(partida => partida.Personaje.UsuarioId == usuarioId)
            .OrderByDescending(partida => partida.FechaUltimaActualizacion).ToListAsync(cancellationToken);

    public async Task<Partida> CrearAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        context.Partidas.Add(partida);
        await context.SaveChangesAsync(cancellationToken);
        return partida;
    }

    public Task<int> MoverAsync(
        int id,
        int usuarioId,
        int deltaX,
        int deltaY,
        CancellationToken cancellationToken = default) =>
        context.Partidas
            .Where(partida => partida.Id == id
                && partida.Personaje.UsuarioId == usuarioId
                && partida.Estado == EstadoPartida.Activa)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    partida => partida.PersonajeX,
                    partida => partida.PersonajeX + deltaX < 0
                        ? 0
                        : partida.PersonajeX + deltaX > 10
                            ? 10
                            : partida.PersonajeX + deltaX)
                .SetProperty(
                    partida => partida.PersonajeY,
                    partida => partida.PersonajeY + deltaY < 0
                        ? 0
                        : partida.PersonajeY + deltaY > 6
                            ? 6
                            : partida.PersonajeY + deltaY),
                cancellationToken);

    public Task<int> FinalizarAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Partidas
            .Where(partida => partida.Id == id
                && partida.Personaje.UsuarioId == usuarioId
                && partida.Estado == EstadoPartida.Activa)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(partida => partida.Estado, EstadoPartida.Finalizada)
                .SetProperty(partida => partida.FechaUltimaActualizacion, _ => DateTime.UtcNow),
                cancellationToken);
}
