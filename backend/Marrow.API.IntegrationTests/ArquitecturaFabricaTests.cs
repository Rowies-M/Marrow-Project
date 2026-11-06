using System.Text.Json;
using Marrow.API.Data;
using Marrow.API.IntegrationTests.Infraestructura;
using Marrow.API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marrow.API.IntegrationTests;

// Levanta la API real con una cadena ficticia y sin el fixture: ninguna prueba abre una conexión a una base.
public class ArquitecturaFabricaTests
{
    private const string CadenaFicticia =
        "Host=host-ficticio.invalid;Port=1;Database=base_ficticia;Username=usuario_ficticio;Password=ficticia";

    [Fact]
    public async Task Api_UsaElEntornoDePruebasYSoloConfiguracionEnMemoria()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();

        var configuracion = fabrica.Services.GetRequiredService<IConfiguration>();
        var proveedores = Assert.IsAssignableFrom<IConfigurationRoot>(configuracion).Providers.ToList();

        Assert.Equal("Testing", fabrica.Services.GetRequiredService<IWebHostEnvironment>().EnvironmentName);
        Assert.DoesNotContain(proveedores, proveedor => proveedor is FileConfigurationProvider);
        Assert.DoesNotContain(proveedores, proveedor => proveedor is EnvironmentVariablesConfigurationProvider);
        Assert.Null(configuracion.GetConnectionString("MarrowDatabase"));
        Assert.Equal("Marrow.IntegrationTests", configuracion["Jwt:Issuer"]);
    }

    [Theory]
    [InlineData(EstadoPartida.Activa, "Activa")]
    [InlineData(EstadoPartida.Finalizada, "Finalizada")]
    [InlineData(Dificultad.Normal, "Normal")]
    [InlineData(Dificultad.Dificil, "Dificil")]
    [InlineData(ResultadoCombate.Victoria, "Victoria")]
    [InlineData(ResultadoCombate.Derrota, "Derrota")]
    [InlineData(ClasePersonaje.Guerrero, "Guerrero")]
    public async Task Api_SerializaLosEnumsComoTexto(object valor, string esperado)
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();
        var opciones = fabrica.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

        Assert.Equal($"\"{esperado}\"", JsonSerializer.Serialize(valor, valor.GetType(), opciones));
    }

    [Fact]
    public async Task Api_TieneUnSoloRegistroDeEfCoreConLaCadenaDeLaFabrica()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();
        await using var alcance = fabrica.Services.CreateAsyncScope();

        var context = alcance.ServiceProvider.GetRequiredService<MarrowDbContext>();

        Assert.True(fabrica.UsaLaConexionValidada(context));
    }

    [Fact]
    public async Task WithWebHostBuilder_EstaBloqueado()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();

        Assert.Throws<NotSupportedException>(() => fabrica.WithWebHostBuilder(_ => { }));
    }

    [Fact]
    public async Task Factories_EstaBloqueado()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();

        Assert.Throws<NotSupportedException>(() => fabrica.Factories);
    }

    [Fact]
    public async Task Variacion_QueAgregaConfiguracionDeEfCore_SeRechazaAntesDeConectar()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();

        var error = await RechazoDeVariacionAsync(fabrica, servicios =>
            servicios.ConfigureDbContext<MarrowDbContext>(opciones => opciones.UseNpgsql(CadenaFicticia + "2")));

        Assert.Equal([MotivoRechazoConexion.RegistroEfCoreAlternativo], error.Motivos);
    }

    [Fact]
    public async Task Variacion_QueAgregaOtroContexto_SeRechazaAntesDeConectar()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();

        var error = await RechazoDeVariacionAsync(fabrica, servicios =>
            servicios.AddDbContext<ContextoAdicional>(opciones => opciones.UseNpgsql(CadenaFicticia)));

        Assert.Equal([MotivoRechazoConexion.RegistroEfCoreAlternativo], error.Motivos);
    }

    [Fact]
    public async Task Variacion_QueCambiaElContextoPorUnaFactoria_SeRechazaAntesDeConectar()
    {
        await using var fabrica = FabricaMarrow.CrearSinBaseParaPruebas();

        var error = await RechazoDeVariacionAsync(fabrica, servicios =>
            servicios.AddScoped(proveedor => proveedor.GetRequiredService<MarrowDbContext>()));

        Assert.Equal([MotivoRechazoConexion.RegistroEfCoreAlternativo], error.Motivos);
    }

    [Fact]
    public void RegistroEfCore_ReemplazaElDeProgramYQuedaUnico()
    {
        var servicios = new ServiceCollection();
        servicios.AddDbContext<MarrowDbContext>(opciones => opciones.UseNpgsql(CadenaFicticia));
        Assert.True(FabricaMarrow.TieneRegistroEfCoreUnico(servicios));

        FabricaMarrow.ReemplazarRegistroEfCore(servicios, CadenaFicticia + "2");

        Assert.True(FabricaMarrow.TieneRegistroEfCoreUnico(servicios));
        using var proveedor = servicios.BuildServiceProvider();
        using var alcance = proveedor.CreateScope();
        var context = alcance.ServiceProvider.GetRequiredService<MarrowDbContext>();
        Assert.Equal(CadenaFicticia + "2", context.Database.GetConnectionString());
    }

    [Fact]
    public void RegistroEfCore_SinContexto_NoEsValido()
    {
        Assert.False(FabricaMarrow.TieneRegistroEfCoreUnico(new ServiceCollection()));
    }

    // El rechazo ocurre al construir el host, que puede envolver la excepción original.
    private static async Task<ConexionDePruebaRechazadaException> RechazoDeVariacionAsync(
        FabricaMarrow fabrica,
        Action<IServiceCollection> configurar)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(() => fabrica.CrearVariacionVerificadaAsync(configurar));
        for (Exception? actual = error; actual is not null; actual = actual.InnerException)
        {
            if (actual is ConexionDePruebaRechazadaException rechazo)
            {
                return rechazo;
            }
        }

        throw new Xunit.Sdk.XunitException("La variación falló por otro motivo, no por las protecciones.");
    }

    private sealed class ContextoAdicional(DbContextOptions<ContextoAdicional> opciones) : DbContext(opciones);
}
