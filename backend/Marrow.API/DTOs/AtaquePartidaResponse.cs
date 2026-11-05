namespace Marrow.API.DTOs;

/// <summary>Resultado de un ataque aceptado y estado actualizado de la partida.</summary>
public sealed record AtaquePartidaResponse(
    EstadoPartidaResponse Estado,
    int DanioInfligido,
    int DanioRecibido);
