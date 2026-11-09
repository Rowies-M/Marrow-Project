using System.Security.Claims;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

/// <summary>
/// Personajes del usuario autenticado, con el rol Jugador. Las estadísticas y el progreso los controla el backend.
/// El rol no da acceso a personajes ajenos: cada operación se filtra por el usuario del token.
/// </summary>
[ApiController]
[Authorize(Roles = RolesUsuario.Jugador)]
[Route("api/personajes")]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class PersonajesController(IPersonajeService personajeService) : ControllerBase
{
    /// <summary>Crea un personaje para el usuario autenticado. Solo se envía el nombre.</summary>
    /// <response code="201">Personaje creado con sus valores iniciales.</response>
    /// <response code="400">Nombre inválido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">El usuario del token ya no existe.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PersonajeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Crear(CrearPersonajeRequest request, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var personaje = await personajeService.CrearAsync(usuarioId, request, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = personaje.Id }, personaje);
    }

    /// <summary>Obtiene los personajes del usuario autenticado.</summary>
    /// <response code="200">Lista de personajes propios.</response>
    /// <response code="401">Token ausente o inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PersonajeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerTodos(CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return Ok(await personajeService.ObtenerPorUsuarioAsync(usuarioId, cancellationToken));
    }

    /// <summary>Obtiene un personaje propio por su identificador.</summary>
    /// <param name="id">Identificador del personaje.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Personaje encontrado.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">El personaje no existe o es de otro usuario.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PersonajeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var personaje = await personajeService.ObtenerPorIdAsync(usuarioId, id, cancellationToken);
        return personaje is null ? NotFound() : Ok(personaje);
    }

    /// <summary>Cambia el nombre de un personaje propio. No modifica estadísticas ni progreso.</summary>
    /// <param name="id">Identificador del personaje.</param>
    /// <param name="request">Nuevo nombre.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="200">Personaje actualizado.</response>
    /// <response code="400">Nombre inválido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">El personaje no existe o es de otro usuario.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PersonajeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Actualizar(
        int id,
        ActualizarPersonajeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var personaje = await personajeService.ActualizarAsync(usuarioId, id, request, cancellationToken);
        return personaje is null ? NotFound() : Ok(personaje);
    }

    /// <summary>Elimina un personaje propio, solo si no tiene partidas: así se conserva el historial.</summary>
    /// <param name="id">Identificador del personaje.</param>
    /// <param name="cancellationToken">Cancelación del pedido.</param>
    /// <response code="204">Personaje eliminado.</response>
    /// <response code="401">Token ausente o inválido.</response>
    /// <response code="404">El personaje no existe o es de otro usuario.</response>
    /// <response code="409">El personaje tiene partidas registradas.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        return await personajeService.EliminarAsync(usuarioId, id, cancellationToken) switch
        {
            ResultadoEliminacionPersonaje.Eliminado => NoContent(),
            ResultadoEliminacionPersonaje.TienePartidas => Conflict(new ProblemDetails
            {
                Title = "No se puede eliminar el personaje.",
                Detail = "El personaje tiene partidas registradas.",
                Status = StatusCodes.Status409Conflict
            }),
            _ => NotFound()
        };
    }

    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out usuarioId) && usuarioId > 0;
    }
}
