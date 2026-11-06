using Marrow.API.IntegrationTests.Infraestructura;

namespace Marrow.API.PostgresTests;

/// <summary>Si la verificación falla, xUnit no ejecuta ninguna prueba de la colección.</summary>
public sealed class EntornoIntegracion : IAsyncLifetime
{
    public FabricaMarrow Fabrica { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Fabrica = await FabricaMarrow.CrearVerificadaAsync();
    }

    public async Task DisposeAsync()
    {
        if (Fabrica is not null)
        {
            await Fabrica.DisposeAsync();
        }
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionIntegracion : ICollectionFixture<EntornoIntegracion>
{
    public const string Nombre = "Integracion";
}
