using Marrow.API.Models;

namespace Marrow.API.DTOs;

public sealed record CrearPartidaResult(
    PartidaResponse? Partida,
    bool DificultadBloqueada);

public sealed record MovimientoPartidaResult(
    EstadoPartidaResponse Estado,
    bool ConcurrenciaPerdida);

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
