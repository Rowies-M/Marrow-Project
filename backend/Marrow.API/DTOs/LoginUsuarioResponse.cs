namespace Marrow.API.DTOs;

/// <summary>
/// Resultado de un inicio de sesión exitoso.
/// </summary>
public sealed record LoginUsuarioResponse(
    string Token,
    DateTimeOffset ExpiraEn,
    UsuarioResponse Usuario);
