using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public class UsuarioService(IUsuarioRepository usuarioRepository) : IUsuarioService
{
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidarId(id);
        return usuarioRepository.ObtenerPorIdAsync(id, cancellationToken);
    }

    public async Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        ValidarUsuario(usuario);

        if (await usuarioRepository.ObtenerPorNombreUsuarioAsync(usuario.NombreUsuario, cancellationToken) is not null ||
            await usuarioRepository.ObtenerPorEmailAsync(usuario.Email, cancellationToken) is not null)
        {
            throw new InvalidOperationException("El nombre de usuario o email ya está registrado.");
        }

        usuario.FechaCreacion = usuario.FechaCreacion == default ? DateTime.UtcNow : usuario.FechaCreacion;
        return await usuarioRepository.CrearAsync(usuario, cancellationToken);
    }

    private static void ValidarUsuario(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario) || string.IsNullOrWhiteSpace(usuario.Email))
        {
            throw new ArgumentException("El nombre de usuario y el email son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("El usuario debe recibir un hash de contraseña válido.", nameof(usuario));
        }
    }

    private static void ValidarId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", nameof(id));
        }
    }
}
