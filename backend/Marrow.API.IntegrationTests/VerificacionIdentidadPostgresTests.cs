using Marrow.API.IntegrationTests.Infraestructura;

namespace Marrow.API.IntegrationTests;

// Pruebas puras de la regla de validación: la consulta real se ejecutará en la siguiente subetapa.
public class VerificacionIdentidadPostgresTests
{
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

    [Theory]
    [InlineData("::1")]
    [InlineData("127.0.0.1")]
    public void ValidarIdentidad_IdentidadCorrecta_NoTieneMotivos(string direccion)
    {
        Assert.Empty(VerificacionIdentidadPostgres.ValidarIdentidad(
            IdentidadValida with { DireccionServidor = direccion }));
    }

    [Theory]
    [InlineData("marrow")]
    [InlineData("Marrow")]
    [InlineData("MARROW_TEST")]
    [InlineData("postgres")]
    public void ValidarIdentidad_BaseIncorrecta_SeRechaza(string baseActual)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { BaseActual = baseActual },
            MotivoRechazoConexion.BaseRealIncorrecta);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("MARROW_TEST")]
    public void ValidarIdentidad_SessionUserIncorrecto_SeRechaza(string usuario)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { SessionUser = usuario },
            MotivoRechazoConexion.SessionUserIncorrecto);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("MARROW_TEST")]
    public void ValidarIdentidad_CurrentUserIncorrecto_SeRechaza(string usuario)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { CurrentUser = usuario },
            MotivoRechazoConexion.CurrentUserIncorrecto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("192.168.0.10")]
    [InlineData("10.0.0.1")]
    [InlineData("::ffff:192.168.0.10")]
    public void ValidarIdentidad_DireccionNoLocal_SeRechaza(string? direccion)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { DireccionServidor = direccion },
            MotivoRechazoConexion.DireccionNoLocal);
    }

    [Theory]
    [InlineData(5433)]
    [InlineData(1)]
    public void ValidarIdentidad_PuertoIncorrecto_SeRechaza(int puerto)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { PuertoServidor = puerto },
            MotivoRechazoConexion.PuertoRealIncorrecto);
    }

    [Fact]
    public void ValidarIdentidad_PropietarioIncorrecto_SeRechaza()
    {
        VerificarUnicoMotivo(
            IdentidadValida with { PropietarioBase = "postgres" },
            MotivoRechazoConexion.PropietarioIncorrecto);
    }

    public static TheoryData<IdentidadPostgres> ConPrivilegios() =>
    [
        IdentidadValida with { EsSuperusuario = true },
        IdentidadValida with { PuedeCrearBases = true },
        IdentidadValida with { PuedeCrearRoles = true },
        IdentidadValida with { PuedeReplicar = true },
        IdentidadValida with { OmiteRls = true }
    ];

    [Theory]
    [MemberData(nameof(ConPrivilegios))]
    public void ValidarIdentidad_PrivilegioAdministrativo_SeRechaza(IdentidadPostgres identidad)
    {
        VerificarUnicoMotivo(identidad, MotivoRechazoConexion.PrivilegiosAdministrativos);
    }

    [Fact]
    public void ValidarIdentidad_ConMembresias_SeRechaza()
    {
        VerificarUnicoMotivo(
            IdentidadValida with { Membresias = 1 },
            MotivoRechazoConexion.MembresiasEnOtrosRoles);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ValidarIdentidad_FaltanBasesDeDesarrollo_SeRechaza(long encontradas)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { BasesDeDesarrolloEncontradas = encontradas },
            MotivoRechazoConexion.BasesDeDesarrolloNoEncontradas);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ValidarIdentidad_ConnectEnBasesDeDesarrollo_SeRechaza(long conConnect)
    {
        VerificarUnicoMotivo(
            IdentidadValida with { BasesDeDesarrolloConConnect = conConnect },
            MotivoRechazoConexion.ConnectEnBasesDeDesarrollo);
    }

    [Fact]
    public void Verificar_IdentidadCorrecta_NoLanza()
    {
        VerificacionIdentidadPostgres.Verificar(IdentidadValida);
    }

    [Fact]
    public void Verificar_ConexionComoDesarrollo_LanzaConTodosLosMotivosYSinValores()
    {
        var desarrollo = IdentidadValida with
        {
            BaseActual = "marrow",
            SessionUser = "postgres",
            CurrentUser = "postgres",
            PropietarioBase = "postgres",
            EsSuperusuario = true,
            BasesDeDesarrolloConConnect = 2
        };

        var error = Assert.Throws<ConexionDePruebaRechazadaException>(
            () => VerificacionIdentidadPostgres.Verificar(desarrollo));

        Assert.Equal(
            [
                MotivoRechazoConexion.BaseRealIncorrecta,
                MotivoRechazoConexion.SessionUserIncorrecto,
                MotivoRechazoConexion.CurrentUserIncorrecto,
                MotivoRechazoConexion.PropietarioIncorrecto,
                MotivoRechazoConexion.PrivilegiosAdministrativos,
                MotivoRechazoConexion.ConnectEnBasesDeDesarrollo
            ],
            error.Motivos);
        Assert.DoesNotContain("postgres", error.Message);
    }

    private static void VerificarUnicoMotivo(IdentidadPostgres identidad, MotivoRechazoConexion motivo)
    {
        Assert.Equal([motivo], VerificacionIdentidadPostgres.ValidarIdentidad(identidad));
    }
}
