using Marrow.API.Data;
using Marrow.API.Interfaces;
using Marrow.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Marrow.API.Repositories;

public class PartidaRepository(MarrowDbContext context) : IPartidaRepository
{
    public Task<Partida?> ObtenerPorIdAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Partidas.AsNoTracking().Include(partida => partida.Personaje)
            .FirstOrDefaultAsync(
                partida => partida.Id == id && partida.Personaje.UsuarioId == usuarioId,
                cancellationToken);

    public Task<Partida?> ObtenerEstadoAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Partidas.AsNoTracking()
            .Include(partida => partida.Personaje)
            .Include(partida => partida.Enemigo)
            .FirstOrDefaultAsync(
                partida => partida.Id == id && partida.Personaje.UsuarioId == usuarioId,
                cancellationToken);

    public async Task<(IReadOnlyList<Partida> Partidas, int Total)> ObtenerPorUsuarioAsync(
        int usuarioId,
        EstadoPartida? estado,
        Dificultad? dificultad,
        bool ascendente,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Sin Include: el listado no devuelve datos del personaje ni del enemigo, y el filtro por usuario ya hace el JOIN.
        var consulta = context.Partidas.AsNoTracking()
            .Where(partida => partida.Personaje.UsuarioId == usuarioId);

        if (estado is not null)
        {
            consulta = consulta.Where(partida => partida.Estado == estado);
        }

        if (dificultad is not null)
        {
            consulta = consulta.Where(partida => partida.Dificultad == dificultad);
        }

        var total = await consulta.CountAsync(cancellationToken);

        // El Id desempata las partidas con la misma fecha, para que ninguna se repita ni se pierda entre páginas.
        var ordenada = ascendente
            ? consulta.OrderBy(partida => partida.FechaCreacion).ThenBy(partida => partida.Id)
            : consulta.OrderByDescending(partida => partida.FechaCreacion).ThenByDescending(partida => partida.Id);

        var partidas = await ordenada
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (partidas, total);
    }

    public async Task<Partida> CrearAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        context.Partidas.Add(partida);
        await context.SaveChangesAsync(cancellationToken);
        return partida;
    }

    public Task<int> MoverAsync(
        int id,
        int usuarioId,
        int deltaX,
        int deltaY,
        CancellationToken cancellationToken = default) =>
        context.Partidas
            .Where(partida => partida.Id == id
                && partida.Personaje.UsuarioId == usuarioId
                && partida.Estado == EstadoPartida.Activa
                && !(partida.PersonajeX + deltaX == partida.EnemigoX
                    && partida.PersonajeY + deltaY == partida.EnemigoY))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    partida => partida.PersonajeX,
                    partida => partida.PersonajeX + deltaX < 0
                        ? 0
                        : partida.PersonajeX + deltaX > 10
                            ? 10
                            : partida.PersonajeX + deltaX)
                .SetProperty(
                    partida => partida.PersonajeY,
                    partida => partida.PersonajeY + deltaY < 0
                        ? 0
                        : partida.PersonajeY + deltaY > 6
                            ? 6
                            : partida.PersonajeY + deltaY),
                cancellationToken);

    public Task<int> FinalizarAsync(
        int id,
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        context.Partidas
            .Where(partida => partida.Id == id
                && partida.Personaje.UsuarioId == usuarioId
                && partida.Estado == EstadoPartida.Activa)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(partida => partida.Estado, EstadoPartida.Finalizada)
                .SetProperty(partida => partida.FechaUltimaActualizacion, _ => DateTime.UtcNow),
                cancellationToken);

    public async Task<int> AplicarAtaqueAsync(
        Partida leida,
        int usuarioId,
        int vidaPersonaje,
        int vidaEnemigo,
        ResultadoCombate? resultado,
        bool desbloquearDificil,
        ProgresoPersonaje? progreso = null,
        CancellationToken cancellationToken = default)
    {
        var estado = resultado is null ? EstadoPartida.Activa : EstadoPartida.Finalizada;

        await using var transaccion = await context.Database.BeginTransactionAsync(cancellationToken);

        var filasActualizadas = await context.Partidas
            .Where(partida => partida.Id == leida.Id
                && partida.Personaje.UsuarioId == usuarioId
                && partida.Estado == EstadoPartida.Activa
                && partida.Dificultad == leida.Dificultad
                && partida.VidaPersonajeActual == leida.VidaPersonajeActual
                && partida.VidaEnemigoActual == leida.VidaEnemigoActual
                && partida.PersonajeX == leida.PersonajeX
                && partida.PersonajeY == leida.PersonajeY
                && partida.EnemigoX == leida.EnemigoX
                && partida.EnemigoY == leida.EnemigoY)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(partida => partida.VidaPersonajeActual, vidaPersonaje)
                .SetProperty(partida => partida.VidaEnemigoActual, vidaEnemigo)
                .SetProperty(partida => partida.Estado, estado)
                .SetProperty(partida => partida.ResultadoCombate, resultado)
                .SetProperty(partida => partida.FechaUltimaActualizacion, _ => DateTime.UtcNow),
                cancellationToken);

        // Solo pasa de Normal a Difícil, así que nunca retira un desbloqueo.
        if (filasActualizadas > 0 && desbloquearDificil)
        {
            await context.Personajes
                .Where(personaje => personaje.Id == leida.PersonajeId
                    && personaje.UsuarioId == usuarioId
                    && personaje.DificultadDesbloqueada == Dificultad.Normal)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(personaje => personaje.DificultadDesbloqueada, Dificultad.Dificil),
                    cancellationToken);
        }

        // Solo se llega aquí si este ataque finalizó la partida, por lo que una victoria se recompensa una sola vez.
        if (filasActualizadas > 0 && progreso is not null)
        {
            var nivelLeido = leida.Personaje.Nivel;
            var experienciaLeida = leida.Personaje.Experiencia;
            var filasProgreso = await context.Personajes
                .Where(personaje => personaje.Id == leida.PersonajeId
                    && personaje.UsuarioId == usuarioId
                    && personaje.Nivel == nivelLeido
                    && personaje.Experiencia == experienciaLeida)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(personaje => personaje.Nivel, progreso.Nivel)
                    .SetProperty(personaje => personaje.Experiencia, progreso.Experiencia),
                    cancellationToken);

            // Otra victoria cambió el personaje: se revierte todo y el jugador puede volver a intentarlo.
            if (filasProgreso == 0)
            {
                await transaccion.RollbackAsync(cancellationToken);
                return 0;
            }
        }

        await transaccion.CommitAsync(cancellationToken);
        return filasActualizadas;
    }
}
