using Marrow.API.Models;

namespace Marrow.API.DTOs;

public sealed record MovimientoPartidaResult(
    EstadoPartidaResponse Estado,
    bool ConcurrenciaPerdida);

public sealed record FinalizacionPartidaResult(
    PartidaResponse Partida,
    bool ConcurrenciaPerdida);
