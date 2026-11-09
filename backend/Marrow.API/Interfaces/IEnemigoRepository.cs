using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IEnemigoRepository
{
    Task<Enemigo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Enemigo?> ObtenerPorNivelAsync(int nivel, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Enemigo>> ObtenerTodosAsync(CancellationToken cancellationToken = default);

    Task<Enemigo> CrearAsync(Enemigo enemigo, CancellationToken cancellationToken = default);
}
