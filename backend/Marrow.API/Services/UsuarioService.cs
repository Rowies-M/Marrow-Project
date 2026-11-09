using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.AspNetCore.Identity;

namespace Marrow.API.Services;

public sealed class UsuarioService(
    UserManager<Usuario> userManager,
    IJwtTokenService jwtTokenService,
    ILogger<UsuarioService> logger) : IUsuarioService
{
    private static readonly string[] CodigosDeDuplicado = ["DuplicateUserName", "DuplicateEmail"];

    public async Task<UsuarioResponse> RegistrarAsync(
        RegistroUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarRegistro(request);

        var nombreUsuario = request.NombreUsuario.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await userManager.FindByNameAsync(nombreUsuario) is not null ||
            await userManager.FindByEmailAsync(email) is not null)
        {
            logger.LogWarning("Registro rechazado: el nombre de usuario o el email ya existen.");
            throw new UsuarioDuplicadoException();
        }

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Email = email,
            FechaCreacion = DateTime.UtcNow
        };

        var creacion = await userManager.CreateAsync(usuario, request.Password);
        if (!creacion.Succeeded)
        {
            // Se registran solo los códigos: las descripciones podrían incluir datos del usuario.
            var codigos = creacion.Errors.Select(error => error.Code).ToList();
            logger.LogWarning("Registro rechazado por Identity: {Codigos}.", codigos);

            if (codigos.Intersect(CodigosDeDuplicado).Any())
            {
                throw new UsuarioDuplicadoException();
            }

            throw new ArgumentException("Los datos de registro no son válidos.");
        }

        await AsignarRolJugadorAsync(usuario);
        logger.LogInformation("Usuario {UsuarioId} registrado.", usuario.Id);

        return MapearRespuesta(usuario);
    }

    public async Task<LoginUsuarioResponse?> AutenticarAsync(
        LoginUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var identificador = request.Usuario.Trim();
        var usuario = await userManager.FindByNameAsync(identificador)
            ?? await userManager.FindByEmailAsync(identificador);

        if (usuario is null || !await userManager.CheckPasswordAsync(usuario, request.Password))
        {
            // No se registra lo escrito: un usuario podría pegar su contraseña en el campo equivocado.
            logger.LogWarning("Inicio de sesión rechazado: credenciales inválidas.");
            return null;
        }

        logger.LogInformation("Usuario {UsuarioId} inició sesión.", usuario.Id);
        var roles = await userManager.GetRolesAsync(usuario);
        var token = jwtTokenService.GenerarToken(usuario, roles);
        return new LoginUsuarioResponse(token.Token, token.ExpiraEn, MapearRespuesta(usuario));
    }

    // Si no se puede asignar el rol, se quita el usuario: no debe quedar una cuenta a medio crear.
    private async Task AsignarRolJugadorAsync(Usuario usuario)
    {
        var asignado = false;
        try
        {
            asignado = (await userManager.AddToRoleAsync(usuario, RolesUsuario.Jugador)).Succeeded;
        }
        finally
        {
            if (!asignado)
            {
                await userManager.DeleteAsync(usuario);
            }
        }

        if (!asignado)
        {
            throw new InvalidOperationException("No se pudo asignar el rol inicial al usuario.");
        }
    }

    private static UsuarioResponse MapearRespuesta(Usuario usuario) => new(
        usuario.Id,
        usuario.NombreUsuario,
        usuario.Email ?? string.Empty,
        new DateTimeOffset(DateTime.SpecifyKind(usuario.FechaCreacion, DateTimeKind.Utc)));

    private static void ValidarRegistro(RegistroUsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Los datos de registro son obligatorios.");
        }
    }
}
