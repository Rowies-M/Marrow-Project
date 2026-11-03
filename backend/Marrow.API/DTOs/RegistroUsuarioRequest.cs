using System.ComponentModel.DataAnnotations;

namespace Marrow.API.DTOs;

/// <summary>
/// Datos necesarios para registrar un usuario.
/// </summary>
public sealed record RegistroUsuarioRequest
{
    /// <summary>Nombre único del usuario.</summary>
    [Required, StringLength(50, MinimumLength = 3)]
    public required string NombreUsuario { get; init; }

    /// <summary>Dirección de email del usuario.</summary>
    [Required, EmailAddress, StringLength(255)]
    public required string Email { get; init; }

    /// <summary>Contraseña elegida por el usuario.</summary>
    [Required, MinLength(8), StringLength(128)]
    public required string Password { get; init; }
}
