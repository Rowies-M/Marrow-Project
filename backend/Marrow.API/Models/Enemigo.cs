namespace Marrow.API.Models;

public class Enemigo
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Nivel { get; set; }

    public int VidaMaxima { get; set; }

    public int Ataque { get; set; }

    public int Defensa { get; set; }

    public int ExperienciaOtorgada { get; set; }
}
