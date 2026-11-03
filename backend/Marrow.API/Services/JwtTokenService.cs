using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace Marrow.API.Services;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public JwtTokenResult GenerarToken(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var issuer = ObtenerConfiguracion("Jwt:Issuer");
        var audience = ObtenerConfiguracion("Jwt:Audience");
        var signingKey = ObtenerConfiguracion("Jwt:SigningKey");
        var expirationMinutes = ObtenerMinutosExpiracion();
        var ahora = DateTime.UtcNow;
        var expiraEn = ahora.AddMinutes(expirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: ahora,
            expires: expiraEn,
            signingCredentials: credentials);

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            new DateTimeOffset(expiraEn, TimeSpan.Zero));
    }

    private string ObtenerConfiguracion(string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Falta la configuración requerida: {key}.");

    private int ObtenerMinutosExpiracion()
    {
        var value = ObtenerConfiguracion("Jwt:ExpirationMinutes");

        return int.TryParse(value, out var minutes) && minutes > 0
            ? minutes
            : throw new InvalidOperationException("La expiración JWT debe ser un número entero positivo.");
    }
}
