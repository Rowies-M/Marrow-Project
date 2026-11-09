using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Estado persistido de una partida y sus entidades jugables.</summary>
/// <param name="PartidaId">Identificador de la partida.</param>
/// <param name="FechaCreacion">Fecha de creación.</param>
/// <param name="FechaUltimaActualizacion">Fecha del último cambio registrado.</param>
/// <param name="Estado">Activa o Finalizada.</param>
/// <param name="PersonajeId">Identificador del personaje.</param>
/// <param name="PersonajeNombre">Nombre del personaje.</param>
/// <param name="PersonajeNivel">Nivel actual del personaje.</param>
/// <param name="PersonajeExperiencia">Experiencia acumulada en el nivel actual.</param>
/// <param name="PersonajeNivelMaximo">Nivel máximo que puede alcanzar el personaje.</param>
/// <param name="PersonajeExperienciaSiguienteNivel">Experiencia necesaria para subir de nivel; nula en el nivel máximo.</param>
/// <param name="VidaPersonajeActual">Vida actual del personaje en esta partida.</param>
/// <param name="VidaPersonajeMaxima">Vida máxima del personaje en esta partida.</param>
/// <param name="PersonajeX">Columna del personaje en el mapa.</param>
/// <param name="PersonajeY">Fila del personaje en el mapa.</param>
/// <param name="EnemigoId">Identificador del enemigo.</param>
/// <param name="EnemigoNombre">Nombre del enemigo.</param>
/// <param name="EnemigoNivel">Nivel del enemigo.</param>
/// <param name="VidaEnemigoActual">Vida actual del enemigo.</param>
/// <param name="VidaEnemigoMaxima">Vida máxima del enemigo en esta partida.</param>
/// <param name="EnemigoX">Columna del enemigo en el mapa.</param>
/// <param name="EnemigoY">Fila del enemigo en el mapa.</param>
/// <param name="Dificultad">Normal o Dificil.</param>
/// <param name="ResultadoCombate">Victoria o Derrota; nulo si el combate no terminó o la partida se abandonó.</param>
/// <param name="GolpePoderosoDesbloqueado">Indica si el nivel del personaje permite usar Golpe poderoso.</param>
/// <param name="GolpePoderosoNivelRequerido">Nivel desde el que se desbloquea Golpe poderoso.</param>
public sealed record EstadoPartidaResponse(
    int PartidaId,
    DateTimeOffset FechaCreacion,
    DateTimeOffset FechaUltimaActualizacion,
    EstadoPartida Estado,
    int PersonajeId,
    string PersonajeNombre,
    int PersonajeNivel,
    int PersonajeExperiencia,
    int PersonajeNivelMaximo,
    int? PersonajeExperienciaSiguienteNivel,
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
    int EnemigoY,
    Dificultad Dificultad,
    ResultadoCombate? ResultadoCombate,
    bool GolpePoderosoDesbloqueado,
    int GolpePoderosoNivelRequerido);
