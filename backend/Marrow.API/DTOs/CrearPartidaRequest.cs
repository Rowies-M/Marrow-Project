using System.ComponentModel.DataAnnotations;

namespace Marrow.API.DTOs;

/// <summary>Datos necesarios para crear una partida.</summary>
public sealed record CrearPartidaRequest
{
    /// <summary>Identificador del personaje propio que jugará la partida.</summary>
    [Range(1, int.MaxValue)]
    public int PersonajeId { get; init; }
}
