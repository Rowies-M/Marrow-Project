using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
}
