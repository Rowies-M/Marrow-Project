using System.ComponentModel.DataAnnotations;
using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Filtros, orden y paginación del listado de partidas.</summary>
public sealed record ConsultaPartidasRequest
{
    /// <summary>Solo partidas en este estado: Activa o Finalizada.</summary>
    [EnumDataType(typeof(EstadoPartida))]
    public EstadoPartida? Estado { get; init; }

    /// <summary>Solo partidas de esta dificultad: Normal o Dificil.</summary>
    [EnumDataType(typeof(Dificultad))]
    public Dificultad? Dificultad { get; init; }

    /// <summary>Orden por fecha de creación: asc o desc.</summary>
    [RegularExpression("^(asc|desc)$")]
    public string Orden { get; init; } = "desc";

    /// <summary>Número de página, empezando en 1.</summary>
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    /// <summary>Cantidad de partidas por página, de 1 a 50.</summary>
    [Range(1, 50)]
    public int PageSize { get; init; } = 10;
}
