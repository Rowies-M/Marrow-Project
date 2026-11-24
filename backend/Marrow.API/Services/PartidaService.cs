using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public sealed class PartidaService(
    IPartidaRepository partidaRepository,
    IPersonajeRepository personajeRepository,
    IEnemigoRepository enemigoRepository,
    ILogger<PartidaService> logger) : IPartidaService
{
    private const int PersonajeXInicial = 1;
    private const int PersonajeYInicial = 1;
    private const int EnemigoXInicial = 5;
    private const int EnemigoYInicial = 3;

    public async Task<CrearPartidaResult?> CrearAsync(
        int usuarioId,
        CrearPartidaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ArgumentNullException.ThrowIfNull(request);
        ValidarId(request.PersonajeId, nameof(request.PersonajeId));

        var personaje = await personajeRepository.ObtenerPorIdAsync(
            request.PersonajeId,
            usuarioId,
            cancellationToken);

        if (personaje is null)
        {
            return null;
        }

        // Esta validación es la que decide: el selector del frontend solo es una ayuda visual.
        if (!ReglasCombate.DificultadDisponible(request.Dificultad, personaje.DificultadDesbloqueada))
        {
            return new CrearPartidaResult(null, DificultadBloqueada: true);
        }

        var nivelEnemigo = ReglasProgresion.NivelDeEnemigo(personaje.Nivel);
        var enemigo = await enemigoRepository.ObtenerPorNivelAsync(nivelEnemigo, cancellationToken)
            ?? throw new InvalidOperationException($"No existe un enemigo de nivel {nivelEnemigo}.");

        var estadisticas = ReglasCombate.ObtenerEstadisticas(request.Dificultad, personaje, enemigo);
        var ahora = DateTime.UtcNow;
        var partida = new Partida
        {
            FechaCreacion = ahora,
            FechaUltimaActualizacion = ahora,
            Estado = EstadoPartida.Activa,
            Dificultad = request.Dificultad,
            PersonajeId = personaje.Id,
            EnemigoId = enemigo.Id,
            VidaPersonajeActual = estadisticas.VidaJugador,
            VidaEnemigoActual = estadisticas.VidaEnemigo,
            PersonajeX = PersonajeXInicial,
            PersonajeY = PersonajeYInicial,
            EnemigoX = EnemigoXInicial,
            EnemigoY = EnemigoYInicial
        };

        var partidaCreada = await partidaRepository.CrearAsync(partida, cancellationToken);
        logger.LogInformation(
            "Partida {PartidaId} creada para el personaje {PersonajeId} en dificultad {Dificultad}.",
            partidaCreada.Id,
            partidaCreada.PersonajeId,
            partidaCreada.Dificultad);

        return new CrearPartidaResult(MapearRespuesta(partidaCreada), DificultadBloqueada: false);
    }

    public async Task<PaginaPartidasResponse> ObtenerPorUsuarioAsync(
        int usuarioId,
        ConsultaPartidasRequest consulta,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ArgumentNullException.ThrowIfNull(consulta);

        var (partidas, total) = await partidaRepository.ObtenerPorUsuarioAsync(
            usuarioId,
            consulta.Estado,
            consulta.Dificultad,
            consulta.Orden == "asc",
            consulta.Page,
            consulta.PageSize,
            cancellationToken);

        return new PaginaPartidasResponse(
            partidas.Select(MapearRespuesta).ToList(),
            consulta.Page,
            consulta.PageSize,
            total,
            (int)Math.Ceiling(total / (double)consulta.PageSize));
    }

    public async Task<PartidaResponse?> ObtenerPorIdAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));

        var partida = await partidaRepository.ObtenerPorIdAsync(partidaId, usuarioId, cancellationToken);
        return partida is null ? null : MapearRespuesta(partida);
    }

    public async Task<EstadoPartidaResponse?> ObtenerEstadoAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));

        var partida = await partidaRepository.ObtenerEstadoAsync(partidaId, usuarioId, cancellationToken);
        return partida is null ? null : MapearEstado(partida);
    }

    public async Task<MovimientoPartidaResult?> MoverAsync(
        int usuarioId,
        int partidaId,
        MoverPartidaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));
        ArgumentNullException.ThrowIfNull(request);

        var (deltaX, deltaY) = request.Direccion switch
        {
            "arriba" => (0, -1),
            "abajo" => (0, 1),
            "izquierda" => (-1, 0),
            "derecha" => (1, 0),
            _ => throw new ArgumentException("La dirección no es válida.", nameof(request))
        };

        var filasActualizadas = await partidaRepository.MoverAsync(
            partidaId,
            usuarioId,
            deltaX,
            deltaY,
            cancellationToken);
        var partida = await partidaRepository.ObtenerEstadoAsync(partidaId, usuarioId, cancellationToken);
        if (partida is null)
        {
            return filasActualizadas == 0
                ? null
                : throw new InvalidOperationException("La partida movida no se pudo leer después de actualizarla.");
        }

        // El personaje no puede ocupar la casilla del enemigo: no se mueve y el enemigo contraataca.
        if (filasActualizadas == 0
            && partida.Estado == EstadoPartida.Activa
            && partida.PersonajeX + deltaX == partida.EnemigoX
            && partida.PersonajeY + deltaY == partida.EnemigoY)
        {
            return await AplicarContraataqueAsync(partida, usuarioId, cancellationToken);
        }

        return new MovimientoPartidaResult(MapearEstado(partida), filasActualizadas == 0);
    }

    // Usa la misma escritura condicional que un ataque: solo se aplica si la partida sigue activa y sin cambios
    // desde la lectura, así que un pedido repetido o concurrente no puede contraatacar dos veces sobre el mismo estado.
    private async Task<MovimientoPartidaResult?> AplicarContraataqueAsync(
        Partida leida,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var resolucion = ReglasCombate.ResolverContraataque(
            ReglasCombate.ObtenerEstadisticas(leida.Dificultad, leida.Personaje, leida.Enemigo),
            leida.VidaPersonajeActual,
            leida.VidaEnemigoActual);

        // Un contraataque nunca da la victoria: no hay experiencia ni desbloqueo de Difícil.
        var filasActualizadas = await partidaRepository.AplicarAtaqueAsync(
            leida,
            usuarioId,
            resolucion.VidaJugador,
            resolucion.VidaEnemigo,
            resolucion.Resultado,
            desbloquearDificil: false,
            progreso: null,
            cancellationToken);
        var actual = await partidaRepository.ObtenerEstadoAsync(leida.Id, usuarioId, cancellationToken);

        if (actual is null)
        {
            return filasActualizadas == 0
                ? null
                : throw new InvalidOperationException("La partida no se pudo leer después del contraataque.");
        }

        if (filasActualizadas == 0)
        {
            return new MovimientoPartidaResult(MapearEstado(actual), ConcurrenciaPerdida: true);
        }

        if (resolucion.Resultado is not null)
        {
            logger.LogInformation("Partida {PartidaId} finalizada con resultado {Resultado}.", leida.Id, resolucion.Resultado);
        }

        return new MovimientoPartidaResult(MapearEstado(actual), ConcurrenciaPerdida: false, resolucion.DanioRecibido);
    }

    public async Task<FinalizacionPartidaResult?> FinalizarAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));

        var filasActualizadas = await partidaRepository.FinalizarAsync(
            partidaId,
            usuarioId,
            cancellationToken);
        if (filasActualizadas > 0)
        {
            logger.LogInformation("Partida {PartidaId} abandonada.", partidaId);
        }

        var partida = await partidaRepository.ObtenerPorIdAsync(partidaId, usuarioId, cancellationToken);
        if (partida is null)
        {
            return filasActualizadas == 0
                ? null
                : throw new InvalidOperationException("La partida finalizada no se pudo leer después de actualizarla.");
        }

        return new FinalizacionPartidaResult(MapearRespuesta(partida), filasActualizadas == 0);
    }

    public Task<AtaquePartidaResult?> AtacarAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default) =>
        EjecutarAtaqueAsync(usuarioId, partidaId, golpePoderoso: false, cancellationToken);

    public Task<AtaquePartidaResult?> UsarGolpePoderosoAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default) =>
        EjecutarAtaqueAsync(usuarioId, partidaId, golpePoderoso: true, cancellationToken);

    private async Task<AtaquePartidaResult?> EjecutarAtaqueAsync(
        int usuarioId,
        int partidaId,
        bool golpePoderoso,
        CancellationToken cancellationToken)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        ValidarId(partidaId, nameof(partidaId));

        var leida = await partidaRepository.ObtenerEstadoAsync(partidaId, usuarioId, cancellationToken);
        if (leida is null)
        {
            return null;
        }

        var rechazo = ClasificarRechazo(leida);
        if (rechazo is not null)
        {
            return new AtaquePartidaResult(null, rechazo);
        }

        // Esta validación es la que decide: el estado del botón en el frontend solo es una ayuda visual.
        if (golpePoderoso && !ReglasCombate.GolpePoderosoDesbloqueado(leida.Personaje.Nivel))
        {
            return new AtaquePartidaResult(null, RechazoAtaque.HabilidadBloqueada);
        }

        var resolucion = ReglasCombate.ResolverAtaque(
            ReglasCombate.ObtenerEstadisticas(leida.Dificultad, leida.Personaje, leida.Enemigo),
            leida.VidaPersonajeActual,
            leida.VidaEnemigoActual,
            golpePoderoso);
        var desbloquearDificil = resolucion.Resultado is { } resultado
            && ReglasCombate.DesbloqueaDificil(leida.Dificultad, resultado);
        var progreso = resolucion.Resultado == ResultadoCombate.Victoria
            ? ReglasProgresion.AplicarVictoria(
                leida.Personaje.Nivel,
                leida.Personaje.Experiencia,
                leida.Enemigo.ExperienciaOtorgada)
            : null;

        var filasActualizadas = await partidaRepository.AplicarAtaqueAsync(
            leida,
            usuarioId,
            resolucion.VidaJugador,
            resolucion.VidaEnemigo,
            resolucion.Resultado,
            desbloquearDificil,
            progreso,
            cancellationToken);
        var actual = await partidaRepository.ObtenerEstadoAsync(partidaId, usuarioId, cancellationToken);

        if (filasActualizadas == 0)
        {
            return actual is null
                ? null
                : new AtaquePartidaResult(null, ClasificarRechazo(actual) ?? RechazoAtaque.EstadoObsoleto);
        }

        if (resolucion.Resultado is not null)
        {
            logger.LogInformation("Partida {PartidaId} finalizada con resultado {Resultado}.", partidaId, resolucion.Resultado);
        }

        if (progreso is not null && progreso.Nivel > leida.Personaje.Nivel)
        {
            logger.LogInformation("Personaje {PersonajeId} subió al nivel {Nivel}.", leida.PersonajeId, progreso.Nivel);
        }

        if (actual is null)
        {
            throw new InvalidOperationException("La partida atacada no se pudo leer después de actualizarla.");
        }

        return new AtaquePartidaResult(
            new AtaquePartidaResponse(
                MapearEstado(actual),
                resolucion.DanioInfligido,
                resolucion.DanioRecibido,
                progreso is null ? 0 : leida.Enemigo.ExperienciaOtorgada),
            null);
    }

    private static RechazoAtaque? ClasificarRechazo(Partida partida)
    {
        if (partida.Estado != EstadoPartida.Activa)
        {
            return RechazoAtaque.PartidaNoActiva;
        }

        return ReglasCombate.EsAdyacente(partida.PersonajeX, partida.PersonajeY, partida.EnemigoX, partida.EnemigoY)
            ? null
            : RechazoAtaque.FueraDeAlcance;
    }

    private static PartidaResponse MapearRespuesta(Partida partida) => new(
        partida.Id,
        new DateTimeOffset(DateTime.SpecifyKind(partida.FechaCreacion, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(partida.FechaUltimaActualizacion, DateTimeKind.Utc)),
        partida.Estado,
        partida.PersonajeId);

    private static EstadoPartidaResponse MapearEstado(Partida partida)
    {
        var estadisticas = ReglasCombate.ObtenerEstadisticas(partida.Dificultad, partida.Personaje, partida.Enemigo);

        return new EstadoPartidaResponse(
            partida.Id,
            new DateTimeOffset(DateTime.SpecifyKind(partida.FechaCreacion, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(partida.FechaUltimaActualizacion, DateTimeKind.Utc)),
            partida.Estado,
            partida.PersonajeId,
            partida.Personaje.Nombre,
            partida.Personaje.Nivel,
            partida.Personaje.Experiencia,
            ReglasProgresion.NivelMaximo,
            ReglasProgresion.ExperienciaSiguienteNivel(partida.Personaje.Nivel),
            partida.VidaPersonajeActual,
            estadisticas.VidaJugador,
            partida.PersonajeX,
            partida.PersonajeY,
            partida.EnemigoId,
            partida.Enemigo.Nombre,
            partida.Enemigo.Nivel,
            partida.VidaEnemigoActual,
            estadisticas.VidaEnemigo,
            partida.EnemigoX,
            partida.EnemigoY,
            partida.Dificultad,
            partida.ResultadoCombate,
            ReglasCombate.GolpePoderosoDesbloqueado(partida.Personaje.Nivel),
            ReglasCombate.NivelGolpePoderoso);
    }

    private static void ValidarId(int id, string parameterName)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", parameterName);
        }
    }
}
