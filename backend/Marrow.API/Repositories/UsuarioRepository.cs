using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class UsuarioRepository(MarrowDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.Usuarios.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default) =>
        context.Usuarios.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.NombreUsuario == nombreUsuario, cancellationToken);

    public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Usuarios.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.Email == email, cancellationToken);

    public async Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
        return usuario;
    }
}
