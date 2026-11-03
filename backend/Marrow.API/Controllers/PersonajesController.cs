using System.Security.Claims;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/personajes")]
public class PersonajesController(IPersonajeService personajeService) : ControllerBase
{
    /// <summary>Crea un personaje para el usuario autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PersonajeResponse), StatusCodes.Status201Created)]
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

    /// <summary>Actualiza un personaje propio.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PersonajeResponse), StatusCodes.Status200OK)]
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

    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out usuarioId) && usuarioId > 0;
    }
}
