using Marrow.API.Models;

namespace Marrow.API.DTOs;

public sealed record CrearPartidaResult(
    PartidaResponse? Partida,
    bool DificultadBloqueada);

/// <param name="DanioRecibido">Daño del contraataque cuando el personaje intentó ocupar la casilla del enemigo; 0 en otro caso.</param>
public sealed record MovimientoPartidaResult(
    EstadoPartidaResponse Estado,
    bool ConcurrenciaPerdida,
    int DanioRecibido = 0);

public sealed record FinalizacionPartidaResult(
    PartidaResponse Partida,
    bool ConcurrenciaPerdida);

public enum RechazoAtaque
{
    PartidaNoActiva,
    FueraDeAlcance,
    EstadoObsoleto,
    HabilidadBloqueada
}

public sealed record AtaquePartidaResult(
    AtaquePartidaResponse? Respuesta,
    RechazoAtaque? Rechazo);
