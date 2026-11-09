using Marrow.API.Models;
using Marrow.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Data;

/// <summary>
/// Completa los datos de referencia al arrancar, sin actualizarlos ni eliminarlos.
/// Además corrige solo los valores inválidos de personajes existentes; no elimina personajes ni partidas.
/// </summary>
public static class InicializadorDatos
{
    public static async Task InicializarAsync(
        MarrowDbContext context,
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<Usuario> userManager,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Las migraciones son un paso aparte: aquí solo se avisa si faltan, nunca se aplican.
        var migracionesPendientes = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        if (migracionesPendientes.Any())
        {
            throw new InvalidOperationException(
                "La base de datos tiene migraciones pendientes. Aplicalas antes de iniciar la API.");
        }

        await InicializarEnemigosAsync(context, logger, cancellationToken);
        await NormalizarPersonajesAsync(context, logger, cancellationToken);
        await InicializarRolesAsync(roleManager, logger);
        await InicializarAdministradorAsync(userManager, configuration, logger);
        await AsignarRolJugadorAUsuariosExistentesAsync(userManager, logger, cancellationToken);
    }

    private static async Task InicializarEnemigosAsync(
        MarrowDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var idsExistentes = await context.Enemigos
            .AsNoTracking()
            .Select(enemigo => enemigo.Id)
            .ToListAsync(cancellationToken);

        var faltantes = DatosIniciales.Enemigos()
            .Where(enemigo => !idsExistentes.Contains(enemigo.Id))
            .ToList();

        if (faltantes.Count == 0)
        {
            logger.LogInformation("Datos iniciales: no faltan enemigos.");
            return;
        }

        context.Enemigos.AddRange(faltantes);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Datos iniciales: se agregaron {Cantidad} enemigos.", faltantes.Count);
    }

    // Cada actualización filtra solo los valores inválidos: la experiencia válida y las estadísticas correctas no se tocan.
    public static async Task NormalizarPersonajesAsync(
        MarrowDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var correcciones = 0;

        correcciones += await context.Personajes
            .Where(personaje => personaje.Nivel > ReglasProgresion.NivelMaximo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(personaje => personaje.Nivel, ReglasProgresion.NivelMaximo), cancellationToken);

        correcciones += await context.Personajes
            .Where(personaje => personaje.Nivel < ReglasProgresion.NivelMinimo)
            .ExecuteUpdateAsync(setters => setters.SetProperty(personaje => personaje.Nivel, ReglasProgresion.NivelMinimo), cancellationToken);

        correcciones += await context.Personajes
            .Where(personaje => personaje.Experiencia < 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(personaje => personaje.Experiencia, 0), cancellationToken);

        correcciones += await context.Personajes
            .Where(personaje => personaje.VidaMaxima <= 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(personaje => personaje.VidaMaxima, DatosIniciales.VidaInicialPersonaje)
                .SetProperty(personaje => personaje.VidaActual, DatosIniciales.VidaInicialPersonaje), cancellationToken);

        correcciones += await context.Personajes
            .Where(personaje => personaje.Ataque <= 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(personaje => personaje.Ataque, DatosIniciales.AtaqueInicialPersonaje), cancellationToken);

        correcciones += await context.Personajes
            .Where(personaje => personaje.Defensa < 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(personaje => personaje.Defensa, DatosIniciales.DefensaInicialPersonaje), cancellationToken);

        if (correcciones > 0)
        {
            logger.LogWarning("Datos iniciales: se aplicaron {Cantidad} correcciones a personajes con valores inválidos.", correcciones);
        }
    }

    private static async Task InicializarRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger logger)
    {
        foreach (var rol in RolesUsuario.Todos)
        {
            if (await roleManager.RoleExistsAsync(rol))
            {
                continue;
            }

            var resultado = await roleManager.CreateAsync(new IdentityRole<int>(rol));
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el rol {rol}.");
            }

            logger.LogInformation("Datos iniciales: se creó el rol {Rol}.", rol);
        }
    }

    // Las credenciales salen de User Secrets o variables de entorno. Sin ellas, no se crea ningún administrador.
    private static async Task InicializarAdministradorAsync(
        UserManager<Usuario> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var nombreUsuario = configuration["AdministradorInicial:NombreUsuario"]?.Trim();
        var email = configuration["AdministradorInicial:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["AdministradorInicial:Password"];

        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Datos iniciales: no se creó el administrador porque falta configurar AdministradorInicial:NombreUsuario, Email o Password.");
            return;
        }

        var existente = await userManager.FindByNameAsync(nombreUsuario) ?? await userManager.FindByEmailAsync(email);
        if (existente is not null)
        {
            // Nunca se promueve a una cuenta existente: podría pertenecer a otra persona.
            if (!await userManager.IsInRoleAsync(existente, RolesUsuario.Administrador))
            {
                logger.LogWarning(
                    "Datos iniciales: el administrador configurado coincide con el usuario {UsuarioId}, que no tiene ese rol. No se modificó.",
                    existente.Id);
            }

            return;
        }

        var administrador = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Email = email,
            FechaCreacion = DateTime.UtcNow
        };

        var creacion = await userManager.CreateAsync(administrador, password);
        if (!creacion.Succeeded)
        {
            logger.LogError(
                "Datos iniciales: no se pudo crear el administrador: {Codigos}.",
                creacion.Errors.Select(error => error.Code).ToList());
            return;
        }

        var asignacion = await userManager.AddToRoleAsync(administrador, RolesUsuario.Administrador);
        if (!asignacion.Succeeded)
        {
            await userManager.DeleteAsync(administrador);
            logger.LogError(
                "Datos iniciales: no se pudo asignar el rol de administrador: {Codigos}.",
                asignacion.Errors.Select(error => error.Code).ToList());
            return;
        }

        logger.LogInformation("Datos iniciales: se creó el administrador {UsuarioId}.", administrador.Id);
    }

    // Completa a los usuarios creados antes de incorporar los roles. Solo agrega: no quita ni cambia otros roles.
    private static async Task AsignarRolJugadorAUsuariosExistentesAsync(
        UserManager<Usuario> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var usuarios = await userManager.Users.ToListAsync(cancellationToken);
        var asignados = 0;
        var fallidos = 0;

        foreach (var usuario in usuarios)
        {
            if (await userManager.IsInRoleAsync(usuario, RolesUsuario.Jugador))
            {
                continue;
            }

            var asignacion = await userManager.AddToRoleAsync(usuario, RolesUsuario.Jugador);
            if (asignacion.Succeeded)
            {
                asignados++;
                continue;
            }

            fallidos++;
            logger.LogError(
                "Datos iniciales: no se pudo asignar el rol Jugador al usuario {UsuarioId}: {Codigos}.",
                usuario.Id,
                asignacion.Errors.Select(error => error.Code).ToList());
        }

        if (asignados > 0)
        {
            logger.LogInformation("Datos iniciales: se asignó el rol Jugador a {Cantidad} usuarios.", asignados);
        }

        if (fallidos > 0)
        {
            logger.LogError("Datos iniciales: {Cantidad} usuarios quedaron sin el rol Jugador. Se reintentará en el próximo arranque.", fallidos);
        }
    }
}
