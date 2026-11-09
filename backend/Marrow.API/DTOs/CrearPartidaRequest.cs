using System.ComponentModel.DataAnnotations;
using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Datos necesarios para crear una partida.</summary>
public sealed record CrearPartidaRequest
{
    /// <summary>Identificador del personaje propio que jugará la partida.</summary>
    [Range(1, int.MaxValue)]
    public int PersonajeId { get; init; }

    /// <summary>Dificultad de la partida. Si no se indica, es Normal. Difícil requiere haberla desbloqueado.</summary>
    [EnumDataType(typeof(Dificultad))]
    public Dificultad Dificultad { get; init; } = Dificultad.Normal;
}
