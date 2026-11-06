using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

namespace Marrow.API.IntegrationTests.Arquitectura;

/// <summary>
/// Una referencia hecha desde código compilado. Origen es una identidad completa:
/// "Ensamblado!Namespace.Tipo.Metodo(TiposDeParametros)".
/// </summary>
internal sealed record UsoDeCodigo(string Origen, string Referencia, bool EsTexto, int Desplazamiento = 0);

internal readonly record struct Instruccion(int Desplazamiento, ushort Codigo, OperandType Operando, int Token, int Destinos);

internal sealed record AnalisisDeEnsamblado(
    string Ensamblado,
    List<UsoDeCodigo> Usos,
    List<string> Tipos,
    List<string> MetodosPublicos,
    List<string> ReferenciasDeEnsamblado,
    List<string> VisibleInternamentePara,
    bool TienePuntoDeEntrada,
    Dictionary<string, List<Instruccion>> Cuerpos);

/// <summary>Lee el IL de un ensamblado como bytes: no lo carga ni ejecuta nada de su código.</summary>
internal static class InspectorIl
{
    public const string FirmaAmbigua = "(ambiguo)";

    private static readonly Regex NombreGenerado = new("^<([^>]+)>", RegexOptions.CultureInvariant);

    private static readonly Dictionary<ushort, OperandType> Operandos = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(campo => (OpCode)campo.GetValue(null)!)
        .ToDictionary(codigo => unchecked((ushort)codigo.Value), codigo => codigo.OperandType);

    public static AnalisisDeEnsamblado Analizar(string rutaDelEnsamblado)
    {
        using var archivo = File.OpenRead(rutaDelEnsamblado);
        using var pe = new PEReader(archivo);
        var metadatos = pe.GetMetadataReader();
        var ensamblado = metadatos.GetString(metadatos.GetAssemblyDefinition().Name);
        var firmas = new FirmaComoTexto();

        var usos = new List<UsoDeCodigo>();
        var tipos = new List<string>();
        var publicos = new List<string>();
        var cuerpos = new Dictionary<string, List<Instruccion>>();

        foreach (var manejadorDeTipo in metadatos.TypeDefinitions)
        {
            var tipo = metadatos.GetTypeDefinition(manejadorDeTipo);
            var real = TipoNoGenerado(metadatos, manejadorDeTipo);
            var nombreReal = $"{ensamblado}!{NombreDeDefinicion(metadatos, real)}";

            if (!EsGenerado(metadatos.GetString(tipo.Name)))
            {
                tipos.Add($"{ensamblado}!{NombreDeDefinicion(metadatos, manejadorDeTipo)}");
                if (!tipo.BaseType.IsNil)
                {
                    usos.Add(new UsoDeCodigo($"{nombreReal}.<tipo>", $"base::{NombreDeTipo(metadatos, tipo.BaseType)}", false));
                }
            }

            foreach (var manejadorDeMetodo in tipo.GetMethods())
            {
                var metodo = metadatos.GetMethodDefinition(manejadorDeMetodo);
                var origen = $"{nombreReal}.{MetodoLogico(metadatos, firmas, tipo, real, metodo)}";
                if ((metodo.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public
                    && !EsGenerado(metadatos.GetString(tipo.Name)))
                {
                    publicos.Add(origen);
                }

                if (metodo.RelativeVirtualAddress == 0)
                {
                    continue;
                }

                var instrucciones = Decodificar(pe.GetMethodBody(metodo.RelativeVirtualAddress).GetILBytes() ?? []);
                cuerpos[$"{NombreDeDefinicion(metadatos, manejadorDeTipo)}::{metadatos.GetString(metodo.Name)}#{MetadataTokens.GetRowNumber(manejadorDeMetodo)}"] = instrucciones;
                AgregarUsos(metadatos, instrucciones, origen, usos);
            }
        }

        var visibles = new List<string>();
        foreach (var manejadorDeAtributo in metadatos.CustomAttributes)
        {
            var atributo = metadatos.GetCustomAttribute(manejadorDeAtributo);
            var constructor = NombreDeEntidad(metadatos, atributo.Constructor);
            usos.Add(new UsoDeCodigo($"{ensamblado}!<atributo>", constructor, false));

            if (constructor.Contains("InternalsVisibleToAttribute", StringComparison.Ordinal))
            {
                var valor = metadatos.GetBlobReader(atributo.Value);
                valor.ReadUInt16();
                visibles.Add(valor.ReadSerializedString() ?? string.Empty);
            }
        }

        var referencias = metadatos.AssemblyReferences
            .Select(manejador => metadatos.GetString(metadatos.GetAssemblyReference(manejador).Name))
            .ToList();
        var puntoDeEntrada = pe.PEHeaders.CorHeader?.EntryPointTokenOrRelativeVirtualAddress is > 0;

        return new AnalisisDeEnsamblado(ensamblado, usos, tipos, publicos, referencias, visibles, puntoDeEntrada, cuerpos);
    }

    /// <summary>Recorre las instrucciones y falla si el IL no termina exactamente alineado.</summary>
    public static List<Instruccion> Decodificar(byte[] il)
    {
        var instrucciones = new List<Instruccion>();
        var posicion = 0;
        while (posicion < il.Length)
        {
            var inicio = posicion;
            ushort codigo = il[posicion++];
            if (codigo == 0xFE)
            {
                codigo = (ushort)(0xFE00 | il[posicion++]);
            }

            if (!Operandos.TryGetValue(codigo, out var operando))
            {
                throw new InvalidOperationException($"Instrucción desconocida en el desplazamiento {inicio}.");
            }

            var token = 0;
            var destinos = 0;
            switch (operando)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    posicion += 1;
                    break;
                case OperandType.InlineVar:
                    posicion += 2;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                case OperandType.InlineSig:
                case OperandType.ShortInlineR:
                    posicion += 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    posicion += 8;
                    break;
                case OperandType.InlineSwitch:
                    // Primero se lee y se avanza sobre la cantidad; después, sobre la tabla de destinos.
                    destinos = LeerEntero(il, ref posicion);
                    posicion += 4 * destinos;
                    break;
                default:
                    token = LeerEntero(il, ref posicion);
                    break;
            }

            if (posicion > il.Length)
            {
                throw new InvalidOperationException($"La instrucción del desplazamiento {inicio} excede el método.");
            }

            instrucciones.Add(new Instruccion(inicio, codigo, operando, token, destinos));
        }

        return instrucciones;
    }

    private static int LeerEntero(byte[] il, ref int posicion)
    {
        if (posicion + 4 > il.Length)
        {
            throw new InvalidOperationException($"Operando incompleto en el desplazamiento {posicion}.");
        }

        var valor = BitConverter.ToInt32(il, posicion);
        posicion += 4;
        return valor;
    }

    private static void AgregarUsos(
        MetadataReader metadatos,
        List<Instruccion> instrucciones,
        string origen,
        List<UsoDeCodigo> usos)
    {
        foreach (var instruccion in instrucciones)
        {
            switch (instruccion.Operando)
            {
                case OperandType.InlineString:
                    var texto = metadatos.GetUserString(MetadataTokens.UserStringHandle(instruccion.Token & 0x00FFFFFF));
                    usos.Add(new UsoDeCodigo(origen, texto, true, instruccion.Desplazamiento));
                    break;
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                    var referencia = NombreDeEntidad(metadatos, MetadataTokens.EntityHandle(instruccion.Token));
                    usos.Add(new UsoDeCodigo(origen, referencia, false, instruccion.Desplazamiento));
                    break;
            }
        }
    }

    private static string NombreDeEntidad(MetadataReader metadatos, EntityHandle manejador)
    {
        switch (manejador.Kind)
        {
            case HandleKind.MemberReference:
                var miembro = metadatos.GetMemberReference((MemberReferenceHandle)manejador);
                return $"{NombreDeTipo(metadatos, miembro.Parent)}::{metadatos.GetString(miembro.Name)}";
            case HandleKind.MethodSpecification:
                return NombreDeEntidad(metadatos, metadatos.GetMethodSpecification((MethodSpecificationHandle)manejador).Method);
            case HandleKind.MethodDefinition:
                var metodo = metadatos.GetMethodDefinition((MethodDefinitionHandle)manejador);
                return $"{NombreDeDefinicion(metadatos, metodo.GetDeclaringType())}::{metadatos.GetString(metodo.Name)}";
            case HandleKind.FieldDefinition:
                var campo = metadatos.GetFieldDefinition((FieldDefinitionHandle)manejador);
                return $"{NombreDeDefinicion(metadatos, campo.GetDeclaringType())}::{metadatos.GetString(campo.Name)}";
            default:
                return $"{NombreDeTipo(metadatos, manejador)}::<tipo>";
        }
    }

    private static string NombreDeTipo(MetadataReader metadatos, EntityHandle manejador)
    {
        switch (manejador.Kind)
        {
            case HandleKind.TypeReference:
                var referencia = metadatos.GetTypeReference((TypeReferenceHandle)manejador);
                var nombre = metadatos.GetString(referencia.Name);
                return referencia.ResolutionScope.Kind == HandleKind.TypeReference
                    ? $"{NombreDeTipo(metadatos, (TypeReferenceHandle)referencia.ResolutionScope)}+{nombre}"
                    : Calificar(metadatos.GetString(referencia.Namespace), nombre);
            case HandleKind.TypeDefinition:
                return NombreDeDefinicion(metadatos, (TypeDefinitionHandle)manejador);
            case HandleKind.TypeSpecification:
                return NombreDeEspecificacion(metadatos, (TypeSpecificationHandle)manejador);
            case HandleKind.MethodDefinition:
                return NombreDeEntidad(metadatos, manejador);
            default:
                return "<otro>";
        }
    }

    // Un tipo genérico cerrado se reduce a su definición: WebApplicationFactory`1<Program> es WebApplicationFactory`1.
    private static string NombreDeEspecificacion(MetadataReader metadatos, TypeSpecificationHandle manejador)
    {
        var firma = metadatos.GetBlobReader(metadatos.GetTypeSpecification(manejador).Signature);
        if (firma.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
        {
            return "<especificacion>";
        }

        firma.ReadSignatureTypeCode();
        return NombreDeTipo(metadatos, firma.ReadTypeHandle());
    }

    private static string NombreDeDefinicion(MetadataReader metadatos, TypeDefinitionHandle manejador)
    {
        var tipo = metadatos.GetTypeDefinition(manejador);
        var nombre = metadatos.GetString(tipo.Name);
        return tipo.GetDeclaringType().IsNil
            ? Calificar(metadatos.GetString(tipo.Namespace), nombre)
            : $"{NombreDeDefinicion(metadatos, tipo.GetDeclaringType())}+{nombre}";
    }

    private static string Calificar(string espacio, string nombre) =>
        espacio.Length == 0 ? nombre : $"{espacio}.{nombre}";

    private static bool EsGenerado(string nombre) => nombre.StartsWith('<');

    // Una lambda o máquina de estados pertenece al tipo que escribió el programador, no a la clase generada.
    private static TypeDefinitionHandle TipoNoGenerado(MetadataReader metadatos, TypeDefinitionHandle manejador)
    {
        var actual = manejador;
        while (EsGenerado(metadatos.GetString(metadatos.GetTypeDefinition(actual).Name)))
        {
            var contenedor = metadatos.GetTypeDefinition(actual).GetDeclaringType();
            if (contenedor.IsNil)
            {
                return actual;
            }

            actual = contenedor;
        }

        return actual;
    }

    private static string MetodoLogico(
        MetadataReader metadatos,
        FirmaComoTexto firmas,
        TypeDefinition tipo,
        TypeDefinitionHandle real,
        MethodDefinition metodo)
    {
        var nombre = metadatos.GetString(metodo.Name);
        var generado = NombreGenerado.Match(nombre);
        var actual = tipo;
        while (!generado.Success)
        {
            generado = NombreGenerado.Match(metadatos.GetString(actual.Name));
            if (generado.Success || actual.GetDeclaringType().IsNil)
            {
                break;
            }

            actual = metadatos.GetTypeDefinition(actual.GetDeclaringType());
        }

        if (!generado.Success)
        {
            return $"{nombre}{Firma(firmas, metodo)}";
        }

        // El código generado solo guarda el nombre del método: con sobrecargas no se sabe de cuál proviene,
        // así que su identidad queda ambigua y ningún permiso puede cubrirla.
        var logico = generado.Groups[1].Value;
        var candidatos = metadatos.GetTypeDefinition(real).GetMethods()
            .Select(metadatos.GetMethodDefinition)
            .Where(candidato => metadatos.GetString(candidato.Name) == logico)
            .ToList();

        return candidatos.Count == 1 ? $"{logico}{Firma(firmas, candidatos[0])}" : $"{logico}{FirmaAmbigua}";
    }

    private static string Firma(FirmaComoTexto firmas, MethodDefinition metodo) =>
        $"({string.Join(",", metodo.DecodeSignature(firmas, null).ParameterTypes)})";

    /// <summary>Firma con nombres de tipo sin namespace: alcanza para distinguir sobrecargas y es legible.</summary>
    private sealed class FirmaComoTexto : ISignatureTypeProvider<string, object?>
    {
        public string GetArrayType(string elementType, ArrayShape shape) => $"{elementType}[,]";

        public string GetByReferenceType(string elementType) => $"{elementType}&";

        public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
            $"{genericType}<{string.Join(",", typeArguments)}>";

        public string GetGenericMethodParameter(object? genericContext, int index) => $"!!{index}";

        public string GetGenericTypeParameter(object? genericContext, int index) => $"!{index}";

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

        public string GetPinnedType(string elementType) => elementType;

        public string GetPointerType(string elementType) => $"{elementType}*";

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

        public string GetSZArrayType(string elementType) => $"{elementType}[]";

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            reader.GetString(reader.GetTypeDefinition(handle).Name);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            reader.GetString(reader.GetTypeReference(handle).Name);

        public string GetTypeFromSpecification(
            MetadataReader reader,
            object? genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
