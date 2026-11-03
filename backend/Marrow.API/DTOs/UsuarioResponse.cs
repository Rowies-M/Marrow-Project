namespace Marrow.API.DTOs;

/// <summary>
/// Datos públicos de un usuario.
/// </summary>
public sealed record UsuarioResponse(
    int Id,
    string NombreUsuario,
    string Email,
    DateTimeOffset FechaCreacion);
