using System.ComponentModel.DataAnnotations;

namespace Marrow.API.DTOs;

/// <summary>Dirección de una casilla que debe recorrer el personaje.</summary>
public sealed record MoverPartidaRequest
{
    /// <summary>Dirección del movimiento: arriba, abajo, izquierda o derecha.</summary>
    [Required]
    [RegularExpression("^(arriba|abajo|izquierda|derecha)$")]
    public required string Direccion { get; init; }
}
