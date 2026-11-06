using Marrow.API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

// Una clase ajena con el mismo nombre corto que la fábrica autorizada.
namespace X
{
    public sealed class FabricaMarrow : WebApplicationFactory<Program>
    {
        public FabricaMarrow(string cadena)
        {
            _ = cadena;
        }

        public static void ReemplazarRegistroEfCore(IServiceCollection servicios, string conexion) =>
            servicios.AddDbContext<MarrowDbContext>(opciones => opciones.UseNpgsql(conexion));
    }
}

// La misma identidad de namespace y tipo que la fábrica autorizada, pero en otro ensamblado.
namespace Marrow.API.IntegrationTests.Infraestructura
{
    public sealed class FabricaMarrow : WebApplicationFactory<Program>
    {
        public FabricaMarrow(string cadena)
        {
            _ = cadena;
        }

        public static void ReemplazarRegistroEfCore(IServiceCollection servicios, string conexion) =>
            servicios.AddDbContext<MarrowDbContext>(opciones => opciones.UseNpgsql(conexion));
    }
}

namespace Marrow.API.IntegrationTests.Muestras
{
    /// <summary>Dos sobrecargas: un permiso sobre una no debe alcanzar a la otra.</summary>
    public static class MuestraDeSobrecargas
    {
        public static object Usar(string cadena) => new NpgsqlConnection(cadena);

        public static object Usar(int puerto) => new NpgsqlConnection($"Port={puerto}");

        // En código generado por el compilador no se sabe a qué sobrecarga corresponde: no debe heredar permisos.
        public static async Task<object> UsarAsincrono(string cadena)
        {
            await Task.Yield();
            return new NpgsqlConnection(cadena);
        }

        public static async Task<object> UsarAsincrono(int puerto)
        {
            await Task.Yield();
            return new NpgsqlConnection($"Port={puerto}");
        }
    }

    /// <summary>La llamada prohibida queda después de los switch de la máquina de estados y del código.</summary>
    public static class MuestraDeSwitch
    {
        public static async Task<int> ConVariosAwaitYSwitch(int opcion, MarrowDbContext context)
        {
            await Task.Yield();
            await Task.Yield();

            int resultado;
            switch (opcion)
            {
                case 0:
                    resultado = 10;
                    break;
                case 1:
                    resultado = 20;
                    break;
                case 2:
                    resultado = 30;
                    break;
                case 3:
                    resultado = 40;
                    break;
                default:
                    resultado = 50;
                    break;
            }

            await context.Database.MigrateAsync();
            return resultado;
        }
    }
}
