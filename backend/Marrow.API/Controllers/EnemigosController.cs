using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

[ApiController]
[Route("api/enemigos")]
public class EnemigosController(IEnemigoService enemigoService) : ControllerBase
{
    /// <summary>
    /// Obtiene todos los enemigos disponibles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EnemigoResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken)
    {
        var enemigos = await enemigoService.ObtenerTodosAsync(cancellationToken);
        var respuesta = enemigos.Select(Mapear).ToList();

        return Ok(respuesta);
    }

    /// <summary>
    /// Obtiene un enemigo por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnemigoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        var enemigo = await enemigoService.ObtenerPorIdAsync(id, cancellationToken);

        return enemigo is null ? NotFound() : Ok(Mapear(enemigo));
    }

    private static EnemigoResponseDto Mapear(Enemigo enemigo) => new(
        enemigo.Id,
        enemigo.Nombre,
        enemigo.Nivel,
        enemigo.VidaMaxima,
        enemigo.Ataque,
        enemigo.Defensa,
        enemigo.ExperienciaOtorgada);
}
