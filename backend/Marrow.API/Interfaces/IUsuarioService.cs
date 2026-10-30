using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IUsuarioService
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
