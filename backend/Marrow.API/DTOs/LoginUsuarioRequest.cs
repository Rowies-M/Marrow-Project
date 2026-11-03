using System.ComponentModel.DataAnnotations;

namespace Marrow.API.DTOs;

/// <summary>
/// Credenciales para iniciar sesión con nombre de usuario o email.
/// </summary>
public sealed record LoginUsuarioRequest
{
    /// <summary>Nombre de usuario o dirección de email.</summary>
    [Required]
    public required string Usuario { get; init; }

    /// <summary>Contraseña del usuario.</summary>
    [Required]
    public required string Password { get; init; }
}
