namespace Marrow.API.DTOs;

/// <summary>
/// Representa un enemigo expuesto por la API.
/// </summary>
public sealed record EnemigoResponseDto(
    int Id,
    string Nombre,
    int Nivel,
    int VidaMaxima,
    int Ataque,
    int Defensa,
    int ExperienciaOtorgada);
