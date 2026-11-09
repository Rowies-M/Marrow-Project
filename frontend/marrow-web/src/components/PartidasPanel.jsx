import { useEffect, useState } from 'react'
import FormMessage from './FormMessage.jsx'
import { createPartida, DEFAULT_PARTIDAS_QUERY, finalizePartida, getPartidaEstado, getPartidas } from '../services/partidaService.js'
import { formatDate } from '../utils/formatDate.js'

const FILTER_CLASS = 'mt-1 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white'
const PAGE_BUTTON_CLASS = 'rounded-lg border border-slate-700 px-3 py-2 text-sm font-semibold text-slate-200 transition hover:border-violet-400 disabled:cursor-not-allowed disabled:opacity-50'

const RESULT_STYLES = {
  Victoria: 'font-semibold text-amber-300',
  Derrota: 'font-semibold text-rose-300',
  Abandonada: 'font-semibold text-slate-300',
}

export default function PartidasPanel({ token, personajeId, isHardUnlocked, query, onQueryChange, onPartidaSelected }) {
  const [newGameDifficulty, setNewGameDifficulty] = useState('Normal')
  const [partidas, setPartidas] = useState([])
  const [pagination, setPagination] = useState({ totalItems: 0, totalPages: 0 })
  const [reloadCount, setReloadCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [finalizingId, setFinalizingId] = useState(null)
  const [loadError, setLoadError] = useState('')
  const [actionError, setActionError] = useState('')
  const [resultados, setResultados] = useState({})

  // El listado no incluye el resultado, así que se lee del estado de cada partida finalizada.
  // El backend solo deja una partida finalizada sin resultado cuando se abandona.
  // Si la lectura falla, el resultado queda vacío: nunca se deduce.
  useEffect(() => {
    const pendientes = partidas.filter((partida) => partida.estado === 'Finalizada' && !(partida.id in resultados))
    if (pendientes.length === 0) {
      return undefined
    }

    let cancelled = false

    async function loadResultados() {
      const lecturas = await Promise.allSettled(pendientes.map((partida) => getPartidaEstado(token, partida.id)))
      if (cancelled) {
        return
      }

      setResultados((current) => {
        const updated = { ...current }
        pendientes.forEach((partida, index) => {
          const lectura = lecturas[index]
          updated[partida.id] = lectura.status === 'fulfilled' ? (lectura.value.resultadoCombate ?? 'Abandonada') : null
        })
        return updated
      })
    }

    loadResultados()
    return () => { cancelled = true }
  }, [partidas, resultados, token])

  useEffect(() => {
    let cancelled = false

    async function loadPartidas() {
      setIsLoading(true)
      setLoadError('')

      try {
        const response = await getPartidas(token, query)
        if (cancelled) {
          return
        }

        // Una acción pudo vaciar la página actual: se vuelve a la última que tenga partidas.
        if (response.items.length === 0 && query.page > 1) {
          onQueryChange({ ...query, page: Math.max(1, response.totalPages) })
          return
        }

        setPartidas(response.items)
        setPagination({ totalItems: response.totalItems, totalPages: response.totalPages })
      } catch (requestError) {
        if (!cancelled) {
          setLoadError(requestError.message)
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    loadPartidas()
    return () => { cancelled = true }
  }, [token, query, onQueryChange, reloadCount])

  // Solo guía la interfaz: el backend vuelve a comprobar el desbloqueo al crear la partida.
  const selectedDifficulty = isHardUnlocked ? newGameDifficulty : 'Normal'

  function handleFilterChange(event) {
    onQueryChange({ ...query, [event.target.name]: event.target.value, page: 1 })
  }

  async function handleCreate() {
    setActionError('')
    setIsCreating(true)

    try {
      await createPartida(token, personajeId, selectedDifficulty)
      // Sin filtros y en la primera página, para que la partida nueva quede a la vista.
      onQueryChange(DEFAULT_PARTIDAS_QUERY)
      setReloadCount((current) => current + 1)
    } catch (requestError) {
      setActionError(requestError.message)
    } finally {
      setIsCreating(false)
    }
  }

  async function handleFinalize(partidaId) {
    if (!window.confirm('¿Querés finalizar esta partida?')) {
      return
    }

    setActionError('')
    setFinalizingId(partidaId)

    try {
      await finalizePartida(token, partidaId)
      setReloadCount((current) => current + 1)
    } catch (requestError) {
      setActionError(requestError.message)
    } finally {
      setFinalizingId(null)
    }
  }

  return (
    <section className="mt-8 rounded-2xl border border-violet-400/20 bg-slate-950/50 p-5 text-left">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h2 className="text-2xl font-bold text-white">Tus partidas</h2>
          <p className="mt-1 text-sm text-slate-400">Continuá una aventura o comenzá una nueva.</p>
        </div>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <label className="text-sm text-slate-400">
            Dificultad de la nueva partida
            <select className={FILTER_CLASS} name="nuevaDificultad" value={selectedDifficulty} onChange={(event) => setNewGameDifficulty(event.target.value)}>
              <option value="Normal">Normal</option>
              <option value="Dificil" disabled={!isHardUnlocked}>{isHardUnlocked ? 'Difícil' : 'Difícil (bloqueada)'}</option>
            </select>
          </label>
          <button
            className="rounded-xl bg-violet-500 px-4 py-3 font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-60"
            type="button"
            onClick={handleCreate}
            disabled={isCreating || isLoading || finalizingId !== null}
          >
            {isCreating ? 'Creando...' : 'Nueva partida'}
          </button>
        </div>
      </div>
      {!isHardUnlocked && (
        <p className="mt-2 text-sm text-slate-400">Ganá una partida en Normal para desbloquear Difícil.</p>
      )}

      <div className="mt-5 grid gap-3 sm:grid-cols-3">
        <label className="text-sm text-slate-400">
          Estado
          <select className={FILTER_CLASS} name="estado" value={query.estado} onChange={handleFilterChange}>
            <option value="">Todos</option>
            <option value="Activa">Activa</option>
            <option value="Finalizada">Finalizada</option>
          </select>
        </label>
        <label className="text-sm text-slate-400">
          Dificultad
          <select className={FILTER_CLASS} name="dificultad" value={query.dificultad} onChange={handleFilterChange}>
            <option value="">Todas</option>
            <option value="Normal">Normal</option>
            <option value="Dificil">Difícil</option>
          </select>
        </label>
        <label className="text-sm text-slate-400">
          Orden
          <select className={FILTER_CLASS} name="orden" value={query.orden} onChange={handleFilterChange}>
            <option value="desc">Más recientes</option>
            <option value="asc">Más antiguas</option>
          </select>
        </label>
      </div>

      <div className="mt-5 space-y-3">
        <FormMessage>{actionError}</FormMessage>

        {isLoading ? (
          <p className="rounded-xl bg-slate-900 p-4 text-sm text-slate-400" role="status">Cargando tus partidas...</p>
        ) : loadError ? (
          <div className="space-y-3">
            <FormMessage>{loadError}</FormMessage>
            <button className={PAGE_BUTTON_CLASS} type="button" onClick={() => setReloadCount((current) => current + 1)}>
              Reintentar
            </button>
          </div>
        ) : partidas.length === 0 ? (
          <p className="rounded-xl border border-dashed border-slate-700 bg-slate-900 p-5 text-sm text-slate-400">
            {query.estado || query.dificultad ? 'No hay partidas con estos filtros.' : 'Todavía no tenés partidas.'}
          </p>
        ) : (
          partidas.map((partida) => {
            const isActive = partida.estado === 'Activa'
            const isFinalizing = finalizingId === partida.id
            const isBusy = finalizingId !== null || isCreating
            const resultado = resultados[partida.id]

            return (
              <article key={partida.id} className="rounded-xl bg-slate-900 p-4">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className={`grid gap-2 text-sm sm:gap-6 ${resultado ? 'sm:grid-cols-4' : 'sm:grid-cols-3'}`}>
                    <div>
                      <p className="text-slate-500">ID</p>
                      <p className="font-semibold text-white">{partida.id}</p>
                    </div>
                    <div>
                      <p className="text-slate-500">Creada</p>
                      <p className="font-semibold text-white">{formatDate(partida.fechaCreacion)}</p>
                    </div>
                    <div>
                      <p className="text-slate-500">Estado</p>
                      <p className={isActive ? 'font-semibold text-emerald-300' : 'font-semibold text-slate-300'}>{partida.estado}</p>
                    </div>
                    {resultado && (
                      <div>
                        <p className="text-slate-500">Resultado</p>
                        <p className={RESULT_STYLES[resultado]}>{resultado}</p>
                      </div>
                    )}
                  </div>
                  {partida.personajeId !== personajeId && (
                    <p className="text-xs font-semibold text-amber-300">De otro personaje</p>
                  )}
                  {isActive && (
                    <div className="flex flex-wrap gap-2">
                      <button
                        className="rounded-xl bg-emerald-500 px-4 py-2 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
                        type="button"
                        onClick={() => onPartidaSelected(partida)}
                        disabled={isBusy}
                      >
                        Jugar
                      </button>
                      <button
                        className="rounded-xl border border-rose-400/40 px-4 py-2 font-semibold text-rose-200 transition hover:border-rose-300 hover:text-white disabled:cursor-not-allowed disabled:opacity-60"
                        type="button"
                        onClick={() => handleFinalize(partida.id)}
                        disabled={isBusy}
                      >
                        {isFinalizing ? 'Finalizando...' : 'Finalizar'}
                      </button>
                    </div>
                  )}
                </div>
              </article>
            )
          })
        )}
      </div>

      {pagination.totalItems > 0 && !loadError && (
        <nav className="mt-4 flex items-center justify-between gap-3" aria-label="Paginación de partidas">
          <button className={PAGE_BUTTON_CLASS} type="button" onClick={() => onQueryChange({ ...query, page: query.page - 1 })} disabled={isLoading || query.page <= 1}>
            Anterior
          </button>
          <p className="text-center text-sm text-slate-400">
            Página {query.page} de {pagination.totalPages} · {pagination.totalItems} {pagination.totalItems === 1 ? 'partida' : 'partidas'}
          </p>
          <button className={PAGE_BUTTON_CLASS} type="button" onClick={() => onQueryChange({ ...query, page: query.page + 1 })} disabled={isLoading || query.page >= pagination.totalPages}>
            Siguiente
          </button>
        </nav>
      )}
    </section>
  )
}
