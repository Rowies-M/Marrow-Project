using System.Security.Claims;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/partidas")]
public class PartidasController(IPartidaService partidaService) : ControllerBase
{
    /// <summary>Crea una partida para un personaje del usuario autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PartidaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear(
        CrearPartidaRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var partida = await partidaService.CrearAsync(usuarioId, request, cancellationToken);
        return partida is null
            ? NotFound()
            : CreatedAtAction(nameof(ObtenerPorId), new { id = partida.Id }, partida);
    }

    /// <summary>Obtiene las partidas de los personajes del usuario autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PartidaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await partidaService.ObtenerPorUsuarioAsync(usuarioId, cancellationToken));
    }

    /// <summary>Obtiene una partida propia por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var partida = await partidaService.ObtenerPorIdAsync(usuarioId, id, cancellationToken);
        return partida is null ? NotFound() : Ok(partida);
    }

    /// <summary>Obtiene el estado de juego de una partida propia.</summary>
    [HttpGet("{id:int}/estado")]
    [ProducesResponseType(typeof(EstadoPartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerEstado(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var estado = await partidaService.ObtenerEstadoAsync(usuarioId, id, cancellationToken);
        return estado is null ? NotFound() : Ok(estado);
    }

    /// <summary>Finaliza una partida propia.</summary>
    [HttpPatch("{id:int}/finalizar")]
    [ProducesResponseType(typeof(PartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalizar(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var partida = await partidaService.FinalizarAsync(usuarioId, id, cancellationToken);
        return partida is null ? NotFound() : Ok(partida);
    }

    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out usuarioId) && usuarioId > 0;
    }
}
