using Marrow.API.Models;

namespace Marrow.API.Data;

/// <summary>
/// Datos de referencia que el juego necesita para funcionar. Aquí no van datos creados por usuarios.
/// Agregar o cambiar un enemigo modifica el modelo de EF Core y requiere una migración.
/// </summary>
public static class DatosIniciales
{
    public const int VidaInicialPersonaje = 80;
    public const int AtaqueInicialPersonaje = 15;
    public const int DefensaInicialPersonaje = 5;

    // Devuelve instancias nuevas en cada llamada, para no compartir entidades entre contextos.
    public static Enemigo[] Enemigos() =>
    [
        new Enemigo
        {
            Id = 1,
            Nombre = "Goblin",
            Nivel = 1,
            VidaMaxima = 30,
            Ataque = 5,
            Defensa = 2,
            ExperienciaOtorgada = 100
        },
        new Enemigo
        {
            Id = 2,
            Nombre = "Esqueleto",
            Nivel = 2,
            VidaMaxima = 45,
            Ataque = 8,
            Defensa = 3,
            ExperienciaOtorgada = 100
        },
        new Enemigo
        {
            Id = 3,
            Nombre = "Orco",
            Nivel = 3,
            VidaMaxima = 80,
            Ataque = 12,
            Defensa = 5,
            ExperienciaOtorgada = 100
        }
    ];
}
