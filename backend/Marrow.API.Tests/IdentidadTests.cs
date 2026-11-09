using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Marrow.API.Data;
using Marrow.API.Models;
using Marrow.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Marrow.API.Tests;

public class IdentidadTests
{
    // Nunca se abre esta conexión: estas pruebas solo leen el modelo de EF Core.
    private const string ConexionInexistente = "Host=127.0.0.1;Port=1;Database=sin_conexion";

    [Fact]
    public void NombreUsuario_EsUnAliasDeUserName()
    {
        var usuario = new Usuario { NombreUsuario = "Bruma" };

        Assert.Equal("Bruma", usuario.UserName);
    }

    [Fact]
    public void Modelo_ConservaLaTablaYLaColumnaDeUsuarios()
    {
        var entidad = ObtenerEntidadUsuario();

        Assert.Equal("Usuarios", entidad.GetTableName());
        Assert.Equal("NombreUsuario", entidad.FindProperty(nameof(Usuario.UserName))!.GetColumnName());
        Assert.Null(entidad.FindProperty(nameof(Usuario.NombreUsuario)));
        Assert.Equal(typeof(int), entidad.FindProperty(nameof(Usuario.Id))!.ClrType);
    }

    [Fact]
    public void Token_IncluyeElUsuarioYSusRoles()
    {
        var token = CrearToken(new Usuario { Id = 7, NombreUsuario = "Bruma", Email = "bruma@example.test" }, [RolesUsuario.Jugador, RolesUsuario.Administrador]);

        Assert.Equal("7", ValorDe(token, ClaimTypes.NameIdentifier));
        Assert.Equal("Bruma", ValorDe(token, ClaimTypes.Name));
        Assert.Equal(
            [RolesUsuario.Administrador, RolesUsuario.Jugador],
            token.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value).Order());
    }

    [Fact]
    public void Token_NoIncluyeDatosInternosDeIdentity()
    {
        var usuario = new Usuario { Id = 7, NombreUsuario = "Bruma", PasswordHash = "hash-de-prueba", SecurityStamp = "sello-de-prueba" };

        var token = CrearToken(usuario, []);

        Assert.DoesNotContain(token.Claims, claim => claim.Value is "hash-de-prueba" or "sello-de-prueba");
        Assert.DoesNotContain(token.Claims, claim => claim.Type == ClaimTypes.Role);
    }

    private static IEntityType ObtenerEntidadUsuario()
    {
        using var context = new MarrowDbContext(new DbContextOptionsBuilder<MarrowDbContext>()
            .UseNpgsql(ConexionInexistente)
            .Options);

        return context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Usuario))!;
    }

    private static JwtSecurityToken CrearToken(Usuario usuario, string[] roles)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "pruebas",
                ["Jwt:Audience"] = "pruebas",
                ["Jwt:SigningKey"] = "clave-ficticia-solo-para-pruebas-unitarias-0123456789",
                ["Jwt:ExpirationMinutes"] = "5"
            })
            .Build();

        var resultado = new JwtTokenService(configuracion).GenerarToken(usuario, roles);
        return new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(resultado.Token);
    }

    private static string? ValorDe(JwtSecurityToken token, string tipo) =>
        token.Claims.FirstOrDefault(claim => claim.Type == tipo)?.Value;
}
