using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.AspNetCore.Identity;

namespace Marrow.API.Services;

public sealed class UsuarioService(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher<Usuario> passwordHasher,
    IJwtTokenService jwtTokenService) : IUsuarioService
{
    public async Task<UsuarioResponse> RegistrarAsync(
        RegistroUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarRegistro(request);

        var nombreUsuario = request.NombreUsuario.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await usuarioRepository.ObtenerPorNombreUsuarioAsync(nombreUsuario, cancellationToken) is not null ||
            await usuarioRepository.ObtenerPorEmailAsync(email, cancellationToken) is not null)
        {
            throw new InvalidOperationException("El nombre de usuario o email ya está registrado.");
        }

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Email = email,
            FechaCreacion = DateTime.UtcNow
        };

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Password);
        var usuarioCreado = await usuarioRepository.CrearAsync(usuario, cancellationToken);

        return MapearRespuesta(usuarioCreado);
    }

    public async Task<LoginUsuarioResponse?> AutenticarAsync(
        LoginUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var identificador = request.Usuario.Trim();
        var usuario = await usuarioRepository.ObtenerPorNombreUsuarioAsync(identificador, cancellationToken)
            ?? await usuarioRepository.ObtenerPorEmailAsync(identificador.ToLowerInvariant(), cancellationToken);

        if (usuario is null ||
            passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token = jwtTokenService.GenerarToken(usuario);
        return new LoginUsuarioResponse(token.Token, token.ExpiraEn, MapearRespuesta(usuario));
    }

    private static UsuarioResponse MapearRespuesta(Usuario usuario) => new(
        usuario.Id,
        usuario.NombreUsuario,
        usuario.Email,
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
