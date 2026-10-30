using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public class EnemigoService(IEnemigoRepository enemigoRepository) : IEnemigoService
{
    public Task<Enemigo?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        return enemigoRepository.ObtenerPorIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Enemigo>> ObtenerTodosAsync(CancellationToken cancellationToken = default) =>
        enemigoRepository.ObtenerTodosAsync(cancellationToken);

    public async Task<Enemigo> CrearAsync(Enemigo enemigo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(enemigo);

        if (string.IsNullOrWhiteSpace(enemigo.Nombre) || enemigo.Nivel <= 0 || enemigo.VidaMaxima < 0 ||
            enemigo.Ataque < 0 || enemigo.Defensa < 0 || enemigo.ExperienciaOtorgada < 0)
        {
            throw new ArgumentException("Los datos del enemigo no son válidos.", nameof(enemigo));
        }

        return await enemigoRepository.CrearAsync(enemigo, cancellationToken);
    }

    private static void ValidarId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", nameof(id));
        }
    }
}
