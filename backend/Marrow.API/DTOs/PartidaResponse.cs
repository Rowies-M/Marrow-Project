using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Datos públicos de una partida.</summary>
public sealed record PartidaResponse(
    int Id,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaUltimaActualizacion,
    EstadoPartida Estado,
    int PersonajeId);
