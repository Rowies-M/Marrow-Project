namespace Marrow.API.DTOs;

/// <summary>Resultado de un ataque aceptado y estado actualizado de la partida.</summary>
/// <param name="Estado">Estado de la partida después del ataque.</param>
/// <param name="DanioInfligido">Daño realmente aplicado al enemigo.</param>
/// <param name="DanioRecibido">Daño del contraataque; 0 si el enemigo murió.</param>
/// <param name="ExperienciaGanada">Experiencia obtenida; solo es mayor que 0 en una victoria.</param>
public sealed record AtaquePartidaResponse(
    EstadoPartidaResponse Estado,
    int DanioInfligido,
    int DanioRecibido,
    int ExperienciaGanada);
