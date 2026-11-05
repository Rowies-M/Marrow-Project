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

    /// <summary>Solicita el desplazamiento de una casilla del personaje en una partida propia.</summary>
    [HttpPatch("{id:int}/movimiento")]
    [ProducesResponseType(typeof(EstadoPartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Mover(
        int id,
        MoverPartidaRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var resultado = await partidaService.MoverAsync(usuarioId, id, request, cancellationToken);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.ConcurrenciaPerdida)
        {
            var partidaActiva = resultado.Estado.Estado == Models.EstadoPartida.Activa;
            return Conflict(new ProblemDetails
            {
                Title = partidaActiva ? "Movimiento no permitido." : "La partida no está activa.",
                Detail = partidaActiva
                    ? "La casilla está ocupada por el enemigo."
                    : "No se puede mover el personaje porque la partida ya fue finalizada."
            });
        }

        return Ok(resultado.Estado);
    }

    /// <summary>Finaliza una partida propia.</summary>
    [HttpPatch("{id:int}/finalizar")]
    [ProducesResponseType(typeof(PartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finalizar(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var resultado = await partidaService.FinalizarAsync(usuarioId, id, cancellationToken);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.ConcurrenciaPerdida)
        {
            return Conflict(new ProblemDetails
            {
                Title = "La partida no está activa.",
                Detail = "No se puede finalizar la partida porque ya fue finalizada."
            });
        }

        return Ok(resultado.Partida);
    }

    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out usuarioId) && usuarioId > 0;
    }
}
