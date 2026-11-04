using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Estado persistido de una partida y sus entidades jugables.</summary>
public sealed record EstadoPartidaResponse(
    int PartidaId,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaUltimaActualizacion,
    EstadoPartida Estado,
    int PersonajeId,
    string PersonajeNombre,
    int PersonajeNivel,
    int VidaPersonajeActual,
    int VidaPersonajeMaxima,
    int PersonajeX,
    int PersonajeY,
    int EnemigoId,
    string EnemigoNombre,
    int EnemigoNivel,
    int VidaEnemigoActual,
    int VidaEnemigoMaxima,
    int EnemigoX,
    int EnemigoY);
