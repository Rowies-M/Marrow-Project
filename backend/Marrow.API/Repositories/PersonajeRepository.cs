using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class PersonajeRepository(MarrowDbContext context) : IPersonajeRepository
{
    public Task<Personaje?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Personajes.AsNoTracking().Include(personaje => personaje.Usuario)
            .FirstOrDefaultAsync(personaje => personaje.Id == id, cancellationToken);

    public Task<Personaje?> ObtenerPorIdAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Personajes.AsNoTracking().Include(personaje => personaje.Usuario)
            .FirstOrDefaultAsync(personaje => personaje.Id == id && personaje.UsuarioId == usuarioId, cancellationToken);

    public async Task<IReadOnlyList<Personaje>> ObtenerPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        await context.Personajes.AsNoTracking().Where(personaje => personaje.UsuarioId == usuarioId)
            .ToListAsync(cancellationToken);

    public async Task<Personaje> CrearAsync(Personaje personaje, CancellationToken cancellationToken = default)
    {
        context.Personajes.Add(personaje);
        await context.SaveChangesAsync(cancellationToken);
        return personaje;
    }

    // La condición va en la misma sentencia: la base borra en cascada las partidas de un personaje,
    // y eso destruiría su historial.
    public Task<int> EliminarSinPartidasAsync(int id, int usuarioId, CancellationToken cancellationToken = default) =>
        context.Personajes
            .Where(personaje => personaje.Id == id
                && personaje.UsuarioId == usuarioId
                && !context.Partidas.Any(partida => partida.PersonajeId == personaje.Id))
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<Personaje> ActualizarAsync(Personaje personaje, CancellationToken cancellationToken = default)
    {
        // Solo se escribe el nombre: las estadísticas y el progreso los controla el combate.
        context.Personajes.Attach(personaje);
        context.Entry(personaje).Property(entidad => entidad.Nombre).IsModified = true;
        await context.SaveChangesAsync(cancellationToken);
        return personaje;
    }
}
