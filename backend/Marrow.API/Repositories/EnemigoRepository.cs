using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class EnemigoRepository(MarrowDbContext context) : IEnemigoRepository
{
    public Task<Enemigo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Enemigos.AsNoTracking().FirstOrDefaultAsync(enemigo => enemigo.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Enemigo>> ObtenerTodosAsync(CancellationToken cancellationToken = default) =>
        await context.Enemigos.AsNoTracking().OrderBy(enemigo => enemigo.Id).ToListAsync(cancellationToken);

    public async Task<Enemigo> CrearAsync(Enemigo enemigo, CancellationToken cancellationToken = default)
    {
        context.Enemigos.Add(enemigo);
        await context.SaveChangesAsync(cancellationToken);
        return enemigo;
    }
}
