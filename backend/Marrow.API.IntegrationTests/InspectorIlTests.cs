using System.Reflection.Emit;
using Marrow.API.IntegrationTests.Arquitectura;

namespace Marrow.API.IntegrationTests;

public class InspectorIlTests
{
    private const string Ensamblado = "Marrow.API.IntegrationTests.Muestras";
    private const byte Switch = 0x45;
    private const byte Call = 0x28;
    private const byte Ret = 0x2A;
    private const byte Nop = 0x00;

    private static readonly AnalisisDeEnsamblado Muestras =
        InspectorIl.Analizar(ArquitecturaTests.RutaEnAnalisis(Ensamblado));

    [Fact]
    public void Decodificar_SwitchConDosDestinos_AvanzaCantidadYTabla()
    {
        byte[] il =
        [
            Switch, 2, 0, 0, 0,
            1, 0, 0, 0,
            2, 0, 0, 0,
            Nop,
            Call, 0x01, 0x00, 0x00, 0x0A,
            Ret
        ];

        var instrucciones = InspectorIl.Decodificar(il);

        Assert.Equal([0, 13, 14, 19], instrucciones.Select(instruccion => instruccion.Desplazamiento));
        Assert.Equal(2, instrucciones[0].Destinos);
        Assert.Equal(0x0A000001, instrucciones[2].Token);
        Assert.Equal(unchecked((ushort)OpCodes.Ret.Value), instrucciones[3].Codigo);
    }

    [Fact]
    public void Decodificar_DestinoQueSeLeeriaComoInstruccion_NoSeConfunde()
    {
        // El último destino vale 0x28, igual que la instrucción call: una tabla mal saltada desalinearía el resto.
        byte[] il =
        [
            Switch, 1, 0, 0, 0,
            0x28, 0, 0, 0,
            Ret
        ];

        var instrucciones = InspectorIl.Decodificar(il);

        Assert.Equal([0, 9], instrucciones.Select(instruccion => instruccion.Desplazamiento));
    }

    [Fact]
    public void Decodificar_SwitchSinDestinos_SoloAvanzaLaCantidad()
    {
        byte[] il = [Switch, 0, 0, 0, 0, Ret];

        Assert.Equal([0, 5], InspectorIl.Decodificar(il).Select(instruccion => instruccion.Desplazamiento));
    }

    [Fact]
    public void Decodificar_InstruccionDeDosBytes_SeLeeCompleta()
    {
        byte[] il = [0xFE, 0x01, Ret];

        var instrucciones = InspectorIl.Decodificar(il);

        Assert.Equal([0, 2], instrucciones.Select(instruccion => instruccion.Desplazamiento));
        Assert.Equal(unchecked((ushort)OpCodes.Ceq.Value), instrucciones[0].Codigo);
    }

    [Theory]
    [InlineData(new byte[] { Switch, 2, 0, 0, 0, 1, 0, 0, 0 })]
    [InlineData(new byte[] { Switch, 1, 0 })]
    [InlineData(new byte[] { Call, 1, 0 })]
    [InlineData(new byte[] { 0xA6 })]
    public void Decodificar_IlIncompletoODesconocido_FallaEnLugarDeIgnorarlo(byte[] il)
    {
        Assert.Throws<InvalidOperationException>(() => InspectorIl.Decodificar(il));
    }

    [Fact]
    public void MuestraConAwaitsYSwitch_TieneTablasDeVariosDestinos()
    {
        var instrucciones = CuerpoDeLaMaquinaDeEstados();

        var saltos = instrucciones.Where(instruccion => instruccion.Operando == OperandType.InlineSwitch).ToList();

        Assert.True(saltos.Count >= 2, "La muestra debe tener el switch de los await y el del código.");
        Assert.Contains(saltos, salto => salto.Destinos >= 4);
    }

    [Fact]
    public void MuestraConAwaitsYSwitch_LosDesplazamientosQuedanAlineados()
    {
        var instrucciones = CuerpoDeLaMaquinaDeEstados();

        for (var i = 0; i < instrucciones.Count - 1; i++)
        {
            if (instrucciones[i].Operando == OperandType.InlineSwitch)
            {
                var esperado = instrucciones[i].Desplazamiento + 5 + (4 * instrucciones[i].Destinos);
                Assert.Equal(esperado, instrucciones[i + 1].Desplazamiento);
            }
        }
    }

    [Fact]
    public void MuestraConAwaitsYSwitch_DetectaLaLlamadaProhibidaDespuesDelSwitch()
    {
        var instrucciones = CuerpoDeLaMaquinaDeEstados();
        var ultimoSwitch = instrucciones.Last(instruccion => instruccion.Operando == OperandType.InlineSwitch);
        var usos = Muestras.Usos
            .Where(uso => uso.Origen.Contains("MuestraDeSwitch.ConVariosAwaitYSwitch(", StringComparison.Ordinal))
            .ToList();

        var migracion = Assert.Single(usos, uso => uso.Referencia.EndsWith("::MigrateAsync", StringComparison.Ordinal));

        Assert.True(migracion.Desplazamiento > ultimoSwitch.Desplazamiento);
        Assert.Contains(
            ReglasArquitectura.BuscarViolaciones(usos),
            violacion => violacion.StartsWith("Creacion o destruccion de esquema: ", StringComparison.Ordinal));
    }

    private static List<Instruccion> CuerpoDeLaMaquinaDeEstados() =>
        Muestras.Cuerpos.Single(cuerpo =>
            cuerpo.Key.Contains("ConVariosAwaitYSwitch", StringComparison.Ordinal)
            && cuerpo.Key.Contains("::MoveNext#", StringComparison.Ordinal)).Value;
}
