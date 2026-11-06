import { useEffect, useState } from 'react'
import FormMessage from './FormMessage.jsx'
import { createPartida, finalizePartida, getPartidaEstado, getPartidas } from '../services/partidaService.js'
import { formatDate } from '../utils/formatDate.js'

const RESULT_STYLES = {
  Victoria: 'font-semibold text-amber-300',
  Derrota: 'font-semibold text-rose-300',
  Abandonada: 'font-semibold text-slate-300',
}

export default function PartidasPanel({ token, personajeId, onPartidaSelected }) {
  const [partidas, setPartidas] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [finalizingId, setFinalizingId] = useState(null)
  const [error, setError] = useState('')
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
      setError('')

      try {
        const response = await getPartidas(token)
        if (!cancelled) {
          setPartidas(Array.isArray(response) ? response : [])
        }
      } catch (requestError) {
        if (!cancelled) {
          setError(requestError.message)
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    loadPartidas()
    return () => { cancelled = true }
  }, [token])

  async function handleCreate() {
    setError('')
    setIsCreating(true)

    try {
      await createPartida(token, personajeId)
      const response = await getPartidas(token)
      setPartidas(Array.isArray(response) ? response : [])
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setIsCreating(false)
    }
  }

  async function handleFinalize(partidaId) {
    if (!window.confirm('¿Querés finalizar esta partida?')) {
      return
    }

    setError('')
    setFinalizingId(partidaId)

    try {
      const updatedPartida = await finalizePartida(token, partidaId)
      setPartidas((current) => current.map((partida) => (
        partida.id === updatedPartida.id ? updatedPartida : partida
      )))
    } catch (requestError) {
      setError(requestError.message)
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
        <button
          className="rounded-xl bg-violet-500 px-4 py-3 font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-60"
          type="button"
          onClick={handleCreate}
          disabled={isCreating || isLoading}
        >
          {isCreating ? 'Creando...' : 'Nueva partida'}
        </button>
      </div>

      <div className="mt-5 space-y-3">
        <FormMessage>{error}</FormMessage>

        {isLoading ? (
          <p className="rounded-xl bg-slate-900 p-4 text-sm text-slate-400">Cargando tus partidas...</p>
        ) : partidas.length === 0 ? (
          <p className="rounded-xl border border-dashed border-slate-700 bg-slate-900 p-5 text-sm text-slate-400">Todavía no tenés partidas.</p>
        ) : (
          partidas.map((partida) => {
            const isActive = partida.estado === 'Activa'
            const isFinalizing = finalizingId === partida.id
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
                  {isActive && (
                    <div className="flex flex-wrap gap-2">
                      <button
                        className="rounded-xl bg-emerald-500 px-4 py-2 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
                        type="button"
                        onClick={() => onPartidaSelected(partida)}
                        disabled={isFinalizing || isCreating}
                      >
                        Jugar
                      </button>
                      <button
                        className="rounded-xl border border-rose-400/40 px-4 py-2 font-semibold text-rose-200 transition hover:border-rose-300 hover:text-white disabled:cursor-not-allowed disabled:opacity-60"
                        type="button"
                        onClick={() => handleFinalize(partida.id)}
                        disabled={isFinalizing || isCreating}
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
    </section>
  )
}
