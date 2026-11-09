namespace Marrow.API.Models;

/// <summary>Roles de la aplicación. Para restringir un endpoint: [Authorize(Roles = RolesUsuario.Administrador)].</summary>
public static class RolesUsuario
{
    public const string Administrador = "Administrador";

    public const string Jugador = "Jugador";

    public static readonly string[] Todos = [Administrador, Jugador];
}
