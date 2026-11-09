using Microsoft.AspNetCore.Identity;

namespace Marrow.API.Models;

public class Usuario : IdentityUser<int>
{
    // Alias de UserName: el dato se guarda una sola vez, en la columna NombreUsuario.
    public string NombreUsuario
    {
        get => UserName ?? string.Empty;
        set => UserName = value;
    }

    public DateTime FechaCreacion { get; set; }
}
