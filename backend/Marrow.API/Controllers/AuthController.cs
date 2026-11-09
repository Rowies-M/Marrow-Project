using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

/// <summary>Registro de usuarios e inicio de sesión con JWT. Es público porque emite el token.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AuthController(IUsuarioService usuarioService) : ControllerBase
{
    /// <summary>Registra un usuario y almacena su contraseña de forma segura.</summary>
    /// <response code="201">Usuario creado.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="409">El nombre de usuario o el email ya están registrados.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(
        RegistroUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var usuario = await usuarioService.RegistrarAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, usuario);
        }
        catch (UsuarioDuplicadoException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "No se pudo registrar el usuario.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    /// <summary>Verifica las credenciales y devuelve el token JWT para los endpoints protegidos.</summary>
    /// <response code="200">Sesión iniciada: incluye el token y su vencimiento.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="401">Credenciales incorrectas.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarioService.AutenticarAsync(request, cancellationToken);

        return usuario is null
            ? Unauthorized(new ProblemDetails
            {
                Title = "Credenciales inválidas.",
                Status = StatusCodes.Status401Unauthorized
            })
            : Ok(usuario);
    }
}
