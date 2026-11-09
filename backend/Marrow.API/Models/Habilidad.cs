namespace Marrow.API.Models;

public class Habilidad
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public ICollection<Personaje> Personajes { get; set; } = new List<Personaje>();
}
