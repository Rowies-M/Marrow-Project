using Marrow.API.IntegrationTests.Infraestructura;
using Npgsql;

namespace Marrow.API.IntegrationTests;

// Pruebas puras: no abren conexiones ni leen la variable real del entorno.
public class ProteccionBaseDePruebasTests
{
    private const string ClaveFicticia = "CLAVE-FICTICIA-NO-MOSTRAR";
    private const string CadenaValida =
        $"Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}";

    [Fact]
    public void ValidarCadena_CadenaValida_NoTieneMotivos()
    {
        Assert.Empty(ProteccionBaseDePruebas.ValidarCadena(CadenaValida));
    }

    [Fact]
    public void ValidarCadena_NombresDeParametroEnOtrasMayusculas_SonValidos()
    {
        Assert.Empty(ProteccionBaseDePruebas.ValidarCadena(
            $"HOST=localhost;port=5432;DATABASE=marrow_test;username=marrow_test;PASSWORD={ClaveFicticia}"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void ValidarCadena_Vacia_SeRechaza(string? cadena)
    {
        Assert.Equal([MotivoRechazoConexion.CadenaVacia], ProteccionBaseDePruebas.ValidarCadena(cadena));
    }

    [Theory]
    [InlineData("marrow")]
    [InlineData("Marrow")]
    [InlineData("postgres")]
    [InlineData("template1")]
    [InlineData("otra_base")]
    [InlineData("marrow_test2")]
    [InlineData("MARROW_TEST")]
    [InlineData("Marrow_Test")]
    [InlineData("marrow_Test")]
    public void ValidarCadena_BaseIncorrecta_SeRechaza(string baseDeDatos)
    {
        VerificarUnicoMotivo(
            $"Host=localhost;Port=5432;Database={baseDeDatos};Username=marrow_test;Password={ClaveFicticia}",
            MotivoRechazoConexion.BaseIncorrecta);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("marrow")]
    [InlineData("MARROW_TEST")]
    [InlineData("Marrow_Test")]
    [InlineData("otro_usuario")]
    public void ValidarCadena_UsuarioIncorrecto_SeRechaza(string usuario)
    {
        VerificarUnicoMotivo(
            $"Host=localhost;Port=5432;Database=marrow_test;Username={usuario};Password={ClaveFicticia}",
            MotivoRechazoConexion.UsuarioIncorrecto);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("LOCALHOST")]
    [InlineData("db.example.test")]
    [InlineData("localhost,db.example.test")]
    [InlineData("localhost:5433")]
    public void ValidarCadena_HostIncorrecto_SeRechaza(string host)
    {
        VerificarUnicoMotivo(
            $"Host={host};Port=5432;Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}",
            MotivoRechazoConexion.HostIncorrecto);
    }

    [Theory]
    [InlineData("5433")]
    [InlineData("1")]
    [InlineData("54320")]
    public void ValidarCadena_PuertoIncorrecto_SeRechaza(string puerto)
    {
        VerificarUnicoMotivo(
            $"Host=localhost;Port={puerto};Database=marrow_test;Username=marrow_test;Password={ClaveFicticia}",
            MotivoRechazoConexion.PuertoIncorrecto);
    }

    [Theory]
    [InlineData("Port=5432;Database=marrow_test;Username=marrow_test;Password=x", MotivoRechazoConexion.HostIncorrecto)]
    [InlineData("Host=localhost;Database=marrow_test;Username=marrow_test;Password=x", MotivoRechazoConexion.PuertoIncorrecto)]
    [InlineData("Host=localhost;Port=5432;Username=marrow_test;Password=x", MotivoRechazoConexion.BaseIncorrecta)]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Password=x", MotivoRechazoConexion.UsuarioIncorrecto)]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test", MotivoRechazoConexion.ContrasenaAusente)]
    public void ValidarCadena_ParametroOmitido_SeRechaza(string cadena, MotivoRechazoConexion motivo)
    {
        VerificarUnicoMotivo(cadena, motivo);
    }

    [Theory]
    [InlineData("Host=;Port=5432;Database=marrow_test;Username=marrow_test;Password=x", MotivoRechazoConexion.HostIncorrecto)]
    [InlineData("Host=localhost;Port=5432;Database=;Username=marrow_test;Password=x", MotivoRechazoConexion.BaseIncorrecta)]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Username=;Password=x", MotivoRechazoConexion.UsuarioIncorrecto)]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password=", MotivoRechazoConexion.ContrasenaAusente)]
    public void ValidarCadena_ValorVacio_SeRechaza(string cadena, MotivoRechazoConexion motivo)
    {
        VerificarUnicoMotivo(cadena, motivo);
    }

    [Theory]
    [InlineData("Passfile=C:\\pgpass")]
    [InlineData("Options=-c role=postgres")]
    [InlineData("Search Path=otro")]
    [InlineData("Target Session Attributes=any")]
    [InlineData("Pooling=false")]
    public void ValidarCadena_ParametroAdicional_SeRechaza(string adicional)
    {
        VerificarUnicoMotivo($"{CadenaValida};{adicional}", MotivoRechazoConexion.ParametroNoPermitido);
    }

    [Fact]
    public void ValidarCadena_AliasDeParametro_SeRechaza()
    {
        var motivos = ProteccionBaseDePruebas.ValidarCadena(
            "Server=localhost;Port=5432;Db=marrow_test;User Id=marrow_test;Password=x");

        Assert.Contains(MotivoRechazoConexion.ParametroNoPermitido, motivos);
    }

    [Theory]
    [InlineData("Host=localhost;Port=abc;Database=marrow_test;Username=marrow_test;Password=x")]
    [InlineData("texto sin formato")]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password=x;sinvalor")]
    public void ValidarCadena_FormatoInvalido_SeRechaza(string cadena)
    {
        VerificarUnicoMotivo(cadena, MotivoRechazoConexion.FormatoInvalido);
    }

    [Theory]
    [InlineData("Marrow")]
    [InlineData("marrow")]
    public void ValidarCadena_BaseRepetidaAlFinal_SeRechaza(string baseDeDatos)
    {
        VerificarUnicoMotivo($"{CadenaValida};Database={baseDeDatos}", MotivoRechazoConexion.BaseIncorrecta);
    }

    [Fact]
    public void ValidarCadena_ContrasenaSinComillasQueInyectaOtraBase_SeRechaza()
    {
        VerificarUnicoMotivo(
            "Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password=x;Database=marrow",
            MotivoRechazoConexion.BaseIncorrecta);
    }

    [Theory]
    [InlineData("con;punto;y;coma")]
    [InlineData("con=igual=")]
    [InlineData("comilla'simple")]
    [InlineData("comilla\"doble")]
    [InlineData("con espacios y ñandú")]
    [InlineData("{llaves}[corchetes]%$#@!")]
    [InlineData("x;Database=marrow")]
    [InlineData("x';Username=postgres;Password='y")]
    public void ValidarCadena_ContrasenaConCaracteresEspeciales_EsValida(string contrasena)
    {
        var cadena = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Database = "marrow_test",
            Username = "marrow_test",
            Password = contrasena
        }.ToString();

        Assert.Empty(ProteccionBaseDePruebas.ValidarCadena(cadena));
        Assert.Equal(contrasena, new NpgsqlConnectionStringBuilder(cadena).Password);
    }

    [Fact]
    public void ValidarCadena_VariosErrores_InformaTodosLosMotivos()
    {
        var motivos = ProteccionBaseDePruebas.ValidarCadena(
            $"Host=otro;Port=1;Database=marrow;Username=postgres;Password={ClaveFicticia}");

        Assert.Equal(
            [
                MotivoRechazoConexion.HostIncorrecto,
                MotivoRechazoConexion.PuertoIncorrecto,
                MotivoRechazoConexion.BaseIncorrecta,
                MotivoRechazoConexion.UsuarioIncorrecto
            ],
            motivos);
    }

    [Fact]
    public void ObtenerConexionValidada_VariableValida_DevuelveLaCadena()
    {
        Assert.Equal(CadenaValida, ProteccionBaseDePruebas.ObtenerConexionValidada(_ => CadenaValida));
    }

    [Fact]
    public void ObtenerConexionValidada_VariableAusente_SeRechaza()
    {
        var error = Assert.Throws<ConexionDePruebaRechazadaException>(
            () => ProteccionBaseDePruebas.ObtenerConexionValidada(_ => null));

        Assert.Equal([MotivoRechazoConexion.VariableAusente], error.Motivos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ObtenerConexionValidada_VariableVacia_SeRechaza(string valor)
    {
        var error = Assert.Throws<ConexionDePruebaRechazadaException>(
            () => ProteccionBaseDePruebas.ObtenerConexionValidada(_ => valor));

        Assert.Equal([MotivoRechazoConexion.CadenaVacia], error.Motivos);
    }

    [Fact]
    public void ObtenerConexionValidada_SoloConsultaLaVariableDePruebas()
    {
        var consultadas = new List<string>();
        var cadenaDeDesarrollo =
            $"Host=localhost;Port=5432;Database=marrow;Username=postgres;Password={ClaveFicticia}";

        var error = Assert.Throws<ConexionDePruebaRechazadaException>(
            () => ProteccionBaseDePruebas.ObtenerConexionValidada(nombre =>
            {
                consultadas.Add(nombre);
                return nombre == "ConnectionStrings__MarrowDatabase" ? cadenaDeDesarrollo : null;
            }));

        Assert.Equal(["MARROW_TEST_CONNECTION"], consultadas);
        Assert.Equal([MotivoRechazoConexion.VariableAusente], error.Motivos);
    }

    [Theory]
    [InlineData("Host=localhost;Port=5432;Database=marrow;Username=marrow_test;Password=CLAVE-FICTICIA-NO-MOSTRAR")]
    [InlineData("Host=otro;Port=1;Database=Marrow;Username=postgres;Password=CLAVE-FICTICIA-NO-MOSTRAR")]
    [InlineData("Host=localhost;Port=abc;Database=marrow_test;Username=marrow_test;Password=CLAVE-FICTICIA-NO-MOSTRAR")]
    [InlineData("Password=CLAVE-FICTICIA-NO-MOSTRAR;fragmento-sin-igual")]
    [InlineData("Host=localhost;Port=5432;Database=marrow_test;Username=marrow_test;Password=CLAVE-FICTICIA-NO-MOSTRAR;Passfile=x")]
    public void Rechazo_NoRevelaLaContrasenaNiValoresDeLaCadena(string cadena)
    {
        var error = Assert.Throws<ConexionDePruebaRechazadaException>(
            () => ProteccionBaseDePruebas.ObtenerConexionValidada(_ => cadena));

        Assert.Null(error.InnerException);
        var textoCompleto = error.ToString();
        Assert.DoesNotContain(ClaveFicticia, textoCompleto);
        Assert.DoesNotContain("Password=", textoCompleto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", textoCompleto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fragmento-sin-igual", textoCompleto);
    }

    private static void VerificarUnicoMotivo(string cadena, MotivoRechazoConexion motivo)
    {
        Assert.Equal([motivo], ProteccionBaseDePruebas.ValidarCadena(cadena));
    }
}
