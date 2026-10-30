namespace Marrow.API.Models;

public class Partida
{
    public int Id { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaUltimaActualizacion { get; set; }

    public EstadoPartida Estado { get; set; }

    public int PersonajeId { get; set; }

    public Personaje Personaje { get; set; } = null!;
}
