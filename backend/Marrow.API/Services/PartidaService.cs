using Marrow.API.DTOs;
using Marrow.API.Interfaces;
using Marrow.API.Models;

namespace Marrow.API.Services;

public sealed class PartidaService(
    IPartidaRepository partidaRepository,
    IPersonajeRepository personajeRepository,
    IEnemigoRepository enemigoRepository) : IPartidaService
{
    private const int EnemigoInicialId = 1;
    private const int PersonajeXInicial = 1;
    private const int PersonajeYInicial = 1;
    private const int EnemigoXInicial = 5;
    private const int EnemigoYInicial = 3;

    public async Task<PartidaResponse?> CrearAsync(
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

        var enemigo = await enemigoRepository.ObtenerPorIdAsync(EnemigoInicialId, cancellationToken)
            ?? throw new InvalidOperationException("El enemigo inicial no existe.");

        var estadisticas = ReglasCombate.ObtenerEstadisticas(personaje.DificultadDesbloqueada);
        var ahora = DateTime.UtcNow;
        var partida = new Partida
        {
            FechaCreacion = ahora,
            FechaUltimaActualizacion = ahora,
            Estado = EstadoPartida.Activa,
            Dificultad = personaje.DificultadDesbloqueada,
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
        return MapearRespuesta(partidaCreada);
    }

    public async Task<IReadOnlyList<PartidaResponse>> ObtenerPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        ValidarId(usuarioId, nameof(usuarioId));
        var partidas = await partidaRepository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);
        return partidas.Select(MapearRespuesta).ToList();
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

        return new MovimientoPartidaResult(MapearEstado(partida), filasActualizadas == 0);
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
        var partida = await partidaRepository.ObtenerPorIdAsync(partidaId, usuarioId, cancellationToken);
        if (partida is null)
        {
            return filasActualizadas == 0
                ? null
                : throw new InvalidOperationException("La partida finalizada no se pudo leer después de actualizarla.");
        }

        return new FinalizacionPartidaResult(MapearRespuesta(partida), filasActualizadas == 0);
    }

    public async Task<AtaquePartidaResult?> AtacarAsync(
        int usuarioId,
        int partidaId,
        CancellationToken cancellationToken = default)
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

        var resolucion = ReglasCombate.ResolverAtaque(
            leida.Dificultad,
            leida.VidaPersonajeActual,
            leida.VidaEnemigoActual);
        var desbloquearDificil = resolucion.Resultado is { } resultado
            && ReglasCombate.DesbloqueaDificil(leida.Dificultad, resultado);
        var progreso = resolucion.Resultado == ResultadoCombate.Victoria
            ? ReglasProgresion.AplicarVictoria(leida.Personaje.Nivel, leida.Personaje.Experiencia)
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

        if (actual is null)
        {
            throw new InvalidOperationException("La partida atacada no se pudo leer después de actualizarla.");
        }

        return new AtaquePartidaResult(
            new AtaquePartidaResponse(
                MapearEstado(actual),
                resolucion.DanioInfligido,
                resolucion.DanioRecibido,
                progreso is null ? 0 : ReglasProgresion.ExperienciaPorVictoria),
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

    private static EstadoPartidaResponse MapearEstado(Partida partida) => new(
        partida.Id,
        new DateTimeOffset(DateTime.SpecifyKind(partida.FechaCreacion, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(partida.FechaUltimaActualizacion, DateTimeKind.Utc)),
        partida.Estado,
        partida.PersonajeId,
        partida.Personaje.Nombre,
        partida.Personaje.Nivel,
        partida.Personaje.Experiencia,
        ReglasProgresion.ExperienciaParaSubir(partida.Personaje.Nivel),
        partida.VidaPersonajeActual,
        ReglasCombate.ObtenerEstadisticas(partida.Dificultad).VidaJugador,
        partida.PersonajeX,
        partida.PersonajeY,
        partida.EnemigoId,
        partida.Enemigo.Nombre,
        partida.Enemigo.Nivel,
        partida.VidaEnemigoActual,
        ReglasCombate.ObtenerEstadisticas(partida.Dificultad).VidaEnemigo,
        partida.EnemigoX,
        partida.EnemigoY,
        partida.Dificultad,
        partida.ResultadoCombate);

    private static void ValidarId(int id, string parameterName)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El id debe ser mayor que cero.", parameterName);
        }
    }
}
