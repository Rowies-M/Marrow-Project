using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class UsuarioRepository(MarrowDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Usuarios.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);
}
