namespace Marrow.API.Models;

public class Personaje
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Nivel { get; set; }

    public int Experiencia { get; set; }

    public int VidaActual { get; set; }

    public int VidaMaxima { get; set; }

    public int Ataque { get; set; }

    public int Defensa { get; set; }

    public ClasePersonaje Clase { get; set; }

    public Dificultad DificultadDesbloqueada { get; set; } = Dificultad.Normal;

    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
