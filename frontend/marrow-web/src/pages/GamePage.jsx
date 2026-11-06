import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../context/useAuth.js'
import FormMessage from '../components/FormMessage.jsx'
import GameHud from '../components/GameHud.jsx'
import GameScene from '../components/GameScene.jsx'
import { getPartidaEstado, movePartida } from '../services/partidaService.js'

export default function GamePage({ partida, personaje, onBackToPartidas }) {
  const { token } = useAuth()
  const [gameState, setGameState] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isMoving, setIsMoving] = useState(false)
  const movingRef = useRef(false)
  const gameStateRef = useRef(null)

  useEffect(() => {
    gameStateRef.current = gameState
  }, [gameState])

  useEffect(() => {
    let cancelled = false

    async function loadGameState() {
      setGameState(null)
      setError('')
      setIsLoading(true)

      try {
        const response = await getPartidaEstado(token, partida.id)
        if (!cancelled) {
          setGameState(response)
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

    loadGameState()
    return () => { cancelled = true }
  }, [partida.id, token])

  useEffect(() => {
    const directions = {
      w: 'arriba',
      ArrowUp: 'arriba',
      s: 'abajo',
      ArrowDown: 'abajo',
      a: 'izquierda',
      ArrowLeft: 'izquierda',
      d: 'derecha',
      ArrowRight: 'derecha',
    }

    async function handleKeyDown(event) {
      const direction = directions[event.key]
      if (!direction || gameStateRef.current?.estado !== 'Activa') {
        return
      }

      event.preventDefault()
      if (event.repeat || movingRef.current) {
        return
      }

      movingRef.current = true
      setIsMoving(true)
      setError('')
      try {
        const updatedState = await movePartida(token, partida.id, direction)
        setGameState(updatedState)
      } catch (requestError) {
        setError(requestError.message)
      } finally {
        movingRef.current = false
        setIsMoving(false)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [partida.id, token])

  const characterName = gameState?.personajeNombre || personaje.nombre

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-8 sm:px-8">
      <section className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-4 border-b border-slate-800 pb-6 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.35em] text-violet-300">Marrow · Partida #{partida.id}</p>
            <h1 className="mt-2 text-4xl font-bold tracking-tight text-white">La entrada a la mazmorra</h1>
            <p className="mt-2 text-slate-400">{characterName} se prepara para avanzar.</p>
          </div>
          <button className="rounded-xl border border-slate-700 px-5 py-3 font-semibold text-slate-200 transition hover:border-violet-400 hover:text-white" type="button" onClick={onBackToPartidas}>Volver a partidas</button>
        </header>

        <div className="mt-6">
          {isLoading && <p className="rounded-xl bg-slate-900 p-4 text-sm text-slate-400" role="status">Cargando el estado de la partida...</p>}
          <FormMessage>{error}</FormMessage>
          {isMoving && <p className="text-sm text-slate-400" role="status">Guardando movimiento...</p>}
          {!isLoading && gameState && (
            <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
              <GameScene
                characterName={gameState.personajeNombre}
                enemyName={gameState.enemigoNombre}
                characterPosition={{ x: gameState.personajeX, y: gameState.personajeY }}
                enemyPosition={{ x: gameState.enemigoX, y: gameState.enemigoY }}
              />
              <aside className="space-y-6">
                <GameHud
                  characterName={gameState.personajeNombre}
                  characterHealth={{ current: gameState.vidaPersonajeActual, maximum: gameState.vidaPersonajeMaxima }}
                  enemyName={gameState.enemigoNombre}
                  enemyHealth={{ current: gameState.vidaEnemigoActual, maximum: gameState.vidaEnemigoMaxima }}
                />
              </aside>
            </div>
          )}
        </div>
      </section>
    </main>
  )
}
