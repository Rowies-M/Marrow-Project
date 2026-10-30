using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
