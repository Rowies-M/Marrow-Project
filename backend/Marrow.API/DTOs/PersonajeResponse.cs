using Marrow.API.Models;

namespace Marrow.API.DTOs;

/// <summary>Datos públicos de un personaje.</summary>
public sealed record PersonajeResponse(
    int Id,
    string Nombre,
    int Nivel,
    int Experiencia,
    int VidaActual,
    int VidaMaxima,
    int Ataque,
    int Defensa,
    ClasePersonaje Clase);
