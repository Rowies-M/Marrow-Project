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

    public async Task<Partida> ActualizarAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        context.Partidas.Update(partida);
        await context.SaveChangesAsync(cancellationToken);
        return partida;
    }
}
