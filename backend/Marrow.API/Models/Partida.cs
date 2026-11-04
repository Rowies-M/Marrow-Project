namespace Marrow.API.Models;

public class Partida
{
    public int Id { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaUltimaActualizacion { get; set; }

    public EstadoPartida Estado { get; set; }

    public int PersonajeId { get; set; }

    public Personaje Personaje { get; set; } = null!;

    public int EnemigoId { get; set; }

    public Enemigo Enemigo { get; set; } = null!;

    public int VidaPersonajeActual { get; set; }

    public int VidaEnemigoActual { get; set; }

    public int PersonajeX { get; set; }

    public int PersonajeY { get; set; }

    public int EnemigoX { get; set; }

    public int EnemigoY { get; set; }
}
