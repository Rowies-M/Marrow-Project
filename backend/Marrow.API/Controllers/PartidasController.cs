using System.Security.Claims;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

/// <summary>
/// Partidas del usuario autenticado, con el rol Jugador: creación, consulta, movimiento, combate y abandono.
/// El rol no da acceso a partidas ajenas: cada operación se filtra por el usuario del token.
/// </summary>
[ApiController]
[Authorize(Roles = RolesUsuario.Jugador)]
[Route("api/partidas")]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PartidasController(IPartidaService partidaService) : ControllerBase
{
    /// <summary>Crea una partida para un personaje del usuario autenticado. La dificultad predeterminada es Normal.</summary>
    /// <response code="201">Partida creada.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">El personaje no existe o es de otro usuario.</response>
    /// <response code="409">El personaje todavía no desbloqueó la dificultad Difícil.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PartidaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        CrearPartidaRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var resultado = await partidaService.CrearAsync(usuarioId, request, cancellationToken);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Partida is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Dificultad bloqueada.",
                Detail = "Ganá una partida en Normal para desbloquear Difícil."
            });
        }

        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Partida.Id }, resultado.Partida);
    }

    /// <summary>Obtiene una página de las partidas del usuario autenticado, con filtros y orden opcionales.</summary>
    /// <response code="200">Página de partidas con los datos de paginación.</response>
    /// <response code="400">Algún parámetro de consulta es inválido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaPartidasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] ConsultaPartidasRequest consulta,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await partidaService.ObtenerPorUsuarioAsync(usuarioId, consulta, cancellationToken));
    }

    /// <summary>Obtiene una partida propia por su identificador.</summary>
    /// <param name="id">Identificador de la partida.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Partida encontrada.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">La partida no existe o es de otro usuario.</response>
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

    /// <summary>Obtiene el estado de juego: posiciones, vidas, dificultad, resultado y progreso del personaje.</summary>
    /// <param name="id">Identificador de la partida.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Estado actual de la partida.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">La partida no existe o es de otro usuario.</response>
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

    /// <summary>Mueve al personaje una casilla. En el borde del mapa se queda en su lugar.</summary>
    /// <param name="id">Identificador de la partida.</param>
    /// <param name="request">Dirección del movimiento.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">
    /// Estado actualizado de la partida. Si el destino es la casilla del enemigo, el personaje no se mueve,
    /// el enemigo contraataca y el daño se informa en danioRecibido.
    /// </response>
    /// <response code="400">Dirección inválida.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">La partida no existe o es de otro usuario.</response>
    /// <response code="409">La partida ya finalizó o cambió durante el pedido.</response>
    [HttpPatch("{id:int}/movimiento")]
    [ProducesResponseType(typeof(MovimientoPartidaResponse), StatusCodes.Status200OK)]
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
                // Con la partida activa solo se llega aquí si cambió mientras se aplicaba el contraataque.
                Title = partidaActiva ? "La partida cambió." : "La partida no está activa.",
                Detail = partidaActiva
                    ? "La partida cambió mientras se procesaba el movimiento. Volvé a intentarlo."
                    : "No se puede mover el personaje porque la partida ya fue finalizada."
            });
        }

        return Ok(new MovimientoPartidaResponse(resultado.Estado, resultado.DanioRecibido));
    }

    /// <summary>Abandona una partida propia. No otorga resultado de combate ni experiencia.</summary>
    /// <param name="id">Identificador de la partida.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Partida finalizada.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">La partida no existe o es de otro usuario.</response>
    /// <response code="409">La partida ya estaba finalizada.</response>
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

    /// <summary>Ataca al enemigo adyacente. Si sobrevive, contraataca; una victoria otorga experiencia.</summary>
    /// <param name="id">Identificador de la partida.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Ataque aplicado: estado actualizado, daño y experiencia ganada.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">La partida no existe o es de otro usuario.</response>
    /// <response code="409">El enemigo está fuera de alcance, la partida ya finalizó o cambió durante el ataque.</response>
    [HttpPost("{id:int:min(1)}/ataque")]
    [ProducesResponseType(typeof(AtaquePartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atacar(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return ResponderAtaque(await partidaService.AtacarAsync(usuarioId, id, cancellationToken));
    }

    /// <summary>Usa Golpe poderoso contra el enemigo adyacente: duplica el ataque del personaje en este golpe. Requiere nivel 2.</summary>
    /// <param name="id">Identificador de la partida.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Golpe aplicado: estado actualizado, daño y experiencia ganada.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">La partida no existe o es de otro usuario.</response>
    /// <response code="409">La habilidad está bloqueada, el enemigo está fuera de alcance, la partida ya finalizó o cambió durante el ataque.</response>
    [HttpPost("{id:int:min(1)}/golpe-poderoso")]
    [ProducesResponseType(typeof(AtaquePartidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UsarGolpePoderoso(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return ResponderAtaque(await partidaService.UsarGolpePoderosoAsync(usuarioId, id, cancellationToken));
    }

    private IActionResult ResponderAtaque(AtaquePartidaResult? resultado)
    {
        if (resultado is null)
        {
            return NotFound();
        }

        return resultado switch
        {
            { Respuesta: null, Rechazo: RechazoAtaque.HabilidadBloqueada } => Conflict(new ProblemDetails
            {
                Title = "Golpe poderoso bloqueado.",
                Detail = "Golpe poderoso se desbloquea en el nivel 2."
            }),
            { Respuesta: not null, Rechazo: null } => Ok(resultado.Respuesta),
            { Respuesta: null, Rechazo: RechazoAtaque.PartidaNoActiva } => Conflict(new ProblemDetails
            {
                Title = "La partida no está activa.",
                Detail = "No se puede atacar porque la partida ya fue finalizada."
            }),
            { Respuesta: null, Rechazo: RechazoAtaque.FueraDeAlcance } => Conflict(new ProblemDetails
            {
                Title = "Ataque no permitido.",
                Detail = "El enemigo está fuera de alcance."
            }),
            { Respuesta: null, Rechazo: RechazoAtaque.EstadoObsoleto } => Conflict(new ProblemDetails
            {
                Title = "La partida cambió.",
                Detail = "La partida cambió mientras se procesaba el ataque. Volvé a intentarlo."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out usuarioId) && usuarioId > 0;
    }
}
