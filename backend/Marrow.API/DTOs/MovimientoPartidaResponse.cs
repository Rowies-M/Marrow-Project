namespace Marrow.API.DTOs;

/// <summary>Resultado de un movimiento aceptado y estado actualizado de la partida.</summary>
/// <param name="Estado">Estado de la partida después del movimiento.</param>
/// <param name="DanioRecibido">
/// Daño del contraataque cuando el personaje intentó ocupar la casilla del enemigo: el personaje no se mueve
/// y el enemigo responde. Es 0 en un movimiento normal.
/// </param>
public sealed record MovimientoPartidaResponse(
    EstadoPartidaResponse Estado,
    int DanioRecibido);
