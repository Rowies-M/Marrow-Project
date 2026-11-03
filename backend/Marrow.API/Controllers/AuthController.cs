using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IUsuarioService usuarioService) : ControllerBase
{
    /// <summary>Registra un usuario y almacena su contraseña de forma segura.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "No se pudo registrar el usuario.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    /// <summary>Verifica las credenciales de un usuario.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUsuarioResponse), StatusCodes.Status200OK)]
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
