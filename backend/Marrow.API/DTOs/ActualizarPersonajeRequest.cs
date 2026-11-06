using System.ComponentModel.DataAnnotations;

namespace Marrow.API.DTOs;

/// <summary>Datos modificables de un personaje existente. Las estadísticas y el progreso los controla el backend.</summary>
public sealed record ActualizarPersonajeRequest
{
    /// <summary>Nombre del personaje.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Nombre { get; init; }
}
