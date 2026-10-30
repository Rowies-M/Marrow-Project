using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class PartidaRepository(MarrowDbContext context) : IPartidaRepository
{
    public Task<Partida?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Partidas.AsNoTracking().Include(partida => partida.Personaje)
            .FirstOrDefaultAsync(partida => partida.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Partida>> ObtenerPorPersonajeAsync(int personajeId, CancellationToken cancellationToken = default) =>
        await context.Partidas.AsNoTracking().Where(partida => partida.PersonajeId == personajeId)
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
