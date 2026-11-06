using System.ComponentModel.DataAnnotations;

namespace Marrow.API.DTOs;

/// <summary>Datos necesarios para crear un personaje. Las estadísticas las asigna el backend.</summary>
public sealed record CrearPersonajeRequest
{
    /// <summary>Nombre del personaje.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Nombre { get; init; }
}
