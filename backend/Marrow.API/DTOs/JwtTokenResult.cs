namespace Marrow.API.DTOs;

/// <summary>
/// Token JWT generado para un usuario autenticado.
/// </summary>
public sealed record JwtTokenResult(
    string Token,
    DateTimeOffset ExpiraEn);
