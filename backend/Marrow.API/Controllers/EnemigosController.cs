using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

/// <summary>Consulta pública de los enemigos del juego, de solo lectura. No requiere token.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/enemigos")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class EnemigosController(IEnemigoService enemigoService) : ControllerBase
{
    /// <summary>
    /// Obtiene todos los enemigos disponibles.
    /// </summary>
    /// <response code="200">Lista de enemigos.</response>
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
    /// <param name="id">Identificador del enemigo.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Enemigo encontrado.</response>
    /// <response code="404">El enemigo no existe.</response>
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
