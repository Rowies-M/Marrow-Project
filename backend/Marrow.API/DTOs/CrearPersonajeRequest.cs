using System.ComponentModel.DataAnnotations;
using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Datos necesarios para crear un personaje.</summary>
public sealed record CrearPersonajeRequest
{
    /// <summary>Nombre del personaje.</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Nombre { get; init; }

    /// <summary>Nivel inicial del personaje.</summary>
    [Range(1, int.MaxValue)]
    public int Nivel { get; init; }

    /// <summary>Experiencia inicial del personaje.</summary>
    [Range(0, int.MaxValue)]
    public int Experiencia { get; init; }

    /// <summary>Defensa del personaje.</summary>
    [Range(0, int.MaxValue)]
    public int Defensa { get; init; }

    /// <summary>Clase del personaje, actualmente solo Guerrero.</summary>
    [EnumDataType(typeof(ClasePersonaje))]
    public ClasePersonaje Clase { get; init; }
}
