namespace Marrow.API.DTOs;

/// <summary>Una página del listado de partidas y los datos para navegar entre páginas.</summary>
/// <param name="Items">Partidas de esta página.</param>
/// <param name="Page">Número de página pedido.</param>
/// <param name="PageSize">Cantidad máxima de partidas por página.</param>
/// <param name="TotalItems">Total de partidas que cumplen los filtros.</param>
/// <param name="TotalPages">Total de páginas disponibles.</param>
public sealed record PaginaPartidasResponse(
    IReadOnlyList<PartidaResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
