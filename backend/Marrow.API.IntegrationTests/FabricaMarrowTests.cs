using System.Data.Common;
using Marrow.API.IntegrationTests.Infraestructura;

namespace Marrow.API.IntegrationTests;

// Sin PostgreSQL y sin levantar la API: todos los pasos que podrían conectarse están sustituidos por dobles,
// así que una regresión en la validación haría fallar la prueba, pero no abriría una conexión real.
public class FabricaMarrowTests
{
    private const string ClaveFicticia = "CLAVE-FICTICIA-NO-MOSTRAR";
    private const string CadenaFicticiaValida =
        $"Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}";

    private static readonly IdentidadPostgres IdentidadValida = new(
        "marrow_test",
        "marrow_test",
        "marrow_test",
        "::1",
        5432,
        "marrow_test",
        EsSuperusuario: false,
        PuedeCrearBases: false,
        PuedeCrearRoles: false,
        PuedeReplicar: false,
        OmiteRls: false,
        Membresias: 0,
        BasesDeDesarrolloEncontradas: 2,
        BasesDeDesarrolloConConnect: 0);

    private static readonly IdentidadPostgres IdentidadDeDesarrollo = new(
        "marrow",
        "postgres",
        "postgres",
        "::1",
        5432,
        "postgres",
        EsSuperusuario: true,
        PuedeCrearBases: true,
        PuedeCrearRoles: true,
        PuedeReplicar: true,
        OmiteRls: true,
        Membresias: 0,
        BasesDeDesarrolloEncontradas: 2,
        BasesDeDesarrolloConConnect: 2);

    [Fact]
    public async Task Crear_VariableAusente_SeRechazaSinAvanzar()
    {
        await VerificarRechazoSinAvanzar(null, MotivoRechazoConexion.VariableAusente);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_VariableVacia_SeRechazaSinAvanzar(string cadena)
    {
        await VerificarRechazoSinAvanzar(cadena, MotivoRechazoConexion.CadenaVacia);
    }

    [Theory]
    [InlineData("marrow")]
    [InlineData("Marrow")]
    [InlineData("MARROW_TEST")]
    [InlineData("postgres")]
    public async Task Crear_OtraBase_SeRechazaSinAvanzar(string baseDeDatos)
    {
        await VerificarRechazoSinAvanzar(
            $"Host=localhost;Port=5432;Database={baseDeDatos};Username=marrow_test;Password={ClaveFicticia}",
            MotivoRechazoConexion.BaseIncorrecta);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("MARROW_TEST")]
    public async Task Crear_OtroUsuario_SeRechazaSinAvanzar(string usuario)
    {
        await VerificarRechazoSinAvanzar(
            $"Host=localhost;Port=5432;Database=marrow_test;Username={usuario};Password={ClaveFicticia}",
            MotivoRechazoConexion.UsuarioIncorrecto);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("db.example.test")]
    public async Task Crear_OtroHost_SeRechazaSinAvanzar(string host)
    {
        await VerificarRechazoSinAvanzar(
            $"Host={host};Port=5432;Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}",
            MotivoRechazoConexion.HostIncorrecto);
    }

    [Fact]
    public async Task Crear_OtroPuerto_SeRechazaSinAvanzar()
    {
        await VerificarRechazoSinAvanzar(
            $"Host=localhost;Port=5433;Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}",
            MotivoRechazoConexion.PuertoIncorrecto);
    }

    [Fact]
    public async Task Crear_IdentidadRealIncorrecta_SeRechazaSinConstruirLaFabrica()
    {
        var construida = false;

        var error = await Assert.ThrowsAsync<ConexionDePruebaRechazadaException>(
            () => FabricaMarrow.CrearVerificadaCoreAsync(
                _ => CadenaFicticiaValida,
                (_, _) => Task.FromResult(IdentidadDeDesarrollo),
                (_, _) =>
                {
                    construida = true;
                    throw new InvalidOperationException("No debió construirse.");
                }));

        Assert.False(construida);
        Assert.Contains(MotivoRechazoConexion.BaseRealIncorrecta, error.Motivos);
        Assert.Contains(MotivoRechazoConexion.SessionUserIncorrecto, error.Motivos);
        Assert.Contains(MotivoRechazoConexion.PrivilegiosAdministrativos, error.Motivos);
        Assert.Contains(MotivoRechazoConexion.ConnectEnBasesDeDesarrollo, error.Motivos);
        Assert.DoesNotContain(ClaveFicticia, error.ToString());
    }

    [Fact]
    public async Task Crear_TodoValido_LlegaAConstruirConLaMismaCadena()
    {
        string? recibida = null;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => FabricaMarrow.CrearVerificadaCoreAsync(
                _ => CadenaFicticiaValida,
                (_, _) => Task.FromResult(IdentidadValida),
                (cadena, _) =>
                {
                    recibida = cadena;
                    throw new InvalidOperationException("Fin de la prueba: no se construye nada real.");
                }));

        Assert.Equal(CadenaFicticiaValida, recibida);
    }

    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=marrow;Username=marrow_test;Password=CLAVE-FICTICIA-NO-MOSTRAR", MotivoRechazoConexion.BaseIncorrecta)]
    [InlineData("Host=localhost;Port=5432;Database=Marrow;Username=marrow_test;Password=CLAVE-FICTICIA-NO-MOSTRAR", MotivoRechazoConexion.BaseIncorrecta)]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Username=postgres;Password=CLAVE-FICTICIA-NO-MOSTRAR", MotivoRechazoConexion.UsuarioIncorrecto)]
    [InlineData("Host=otro.invalid;Port=5432;Database=marrow_test;Username=marrow_test;Password=CLAVE-FICTICIA-NO-MOSTRAR", MotivoRechazoConexion.HostIncorrecto)]
    [InlineData("", MotivoRechazoConexion.CadenaVacia)]
    public async Task LeerIdentidad_ConCadenaInvalida_SeRechazaSinCrearConexion(
        string cadena,
        MotivoRechazoConexion motivo)
    {
        var conexionCreada = false;

        var error = await Assert.ThrowsAsync<ConexionDePruebaRechazadaException>(
            () => FabricaMarrow.LeerIdentidadConFabricaAsync(cadena, _ =>
            {
                conexionCreada = true;
                throw new InvalidOperationException("No debió crearse una conexión.");
            }));

        Assert.False(conexionCreada);
        Assert.Equal([motivo], error.Motivos);
        Assert.DoesNotContain(ClaveFicticia, error.ToString());
    }

    private static async Task VerificarRechazoSinAvanzar(
        string? cadena,
        MotivoRechazoConexion motivo)
    {
        var identidadConsultada = false;
        var construida = false;

        var error = await Assert.ThrowsAsync<ConexionDePruebaRechazadaException>(
            () => FabricaMarrow.CrearVerificadaCoreAsync(
                _ => cadena,
                (_, _) =>
                {
                    identidadConsultada = true;
                    return Task.FromResult(IdentidadDeDesarrollo);
                },
                (_, _) =>
                {
                    construida = true;
                    throw new InvalidOperationException("No debió construirse.");
                }));

        Assert.Equal([motivo], error.Motivos);
        Assert.False(identidadConsultada);
        Assert.False(construida);
        Assert.DoesNotContain(ClaveFicticia, error.ToString());
    }
}
