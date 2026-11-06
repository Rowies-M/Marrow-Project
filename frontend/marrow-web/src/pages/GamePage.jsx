import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../context/useAuth.js'
import CombatPanel from '../components/CombatPanel.jsx'
import FormMessage from '../components/FormMessage.jsx'
import GameHud from '../components/GameHud.jsx'
import GameInfoPanel from '../components/GameInfoPanel.jsx'
import GameResultPanel from '../components/GameResultPanel.jsx'
import GameScene from '../components/GameScene.jsx'
import TouchControls from '../components/TouchControls.jsx'
import useGameKeyboard from '../hooks/useGameKeyboard.js'
import useIsTouchDevice from '../hooks/useIsTouchDevice.js'
import { attackPartida, finalizePartida, getPartidaEstado, movePartida } from '../services/partidaService.js'

// Solo guía la interfaz: la decisión final sobre el ataque la toma el backend.
function isEnemyAdjacent(gameState) {
  return Math.abs(gameState.personajeX - gameState.enemigoX) + Math.abs(gameState.personajeY - gameState.enemigoY) === 1
}

export default function GamePage({ partida, personaje, onBackToPartidas }) {
  const { token } = useAuth()
  const isTouchDevice = useIsTouchDevice()
  const [gameState, setGameState] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isMoving, setIsMoving] = useState(false)
  const [isAttacking, setIsAttacking] = useState(false)
  const [isAbandoning, setIsAbandoning] = useState(false)
  const [lastAttack, setLastAttack] = useState(null)
  const requestInProgressRef = useRef(false)

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

  const handleMove = useCallback(async (direction) => {
    if (requestInProgressRef.current) {
      return
    }

    requestInProgressRef.current = true
    setIsMoving(true)
    setError('')
    setLastAttack(null)
    try {
      setGameState(await movePartida(token, partida.id, direction))
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      requestInProgressRef.current = false
      setIsMoving(false)
    }
  }, [partida.id, token])

  const isActive = gameState?.estado === 'Activa'
  const isAdjacent = gameState ? isEnemyAdjacent(gameState) : false

  const handleAttack = useCallback(async () => {
    if (requestInProgressRef.current || !isActive || !isAdjacent) {
      return
    }

    requestInProgressRef.current = true
    setIsAttacking(true)
    setError('')
    try {
      const response = await attackPartida(token, partida.id)
      setGameState(response.estado)
      setLastAttack({ danioInfligido: response.danioInfligido, danioRecibido: response.danioRecibido })
    } catch (requestError) {
      setError(requestError.message)
      setLastAttack(null)
      if (requestError.status === 409) {
        // Un rechazo indica que la pantalla quedó desactualizada: se vuelve a leer el estado real.
        try {
          setGameState(await getPartidaEstado(token, partida.id))
        } catch {
          // El error del ataque ya está a la vista y se conserva el último estado confirmado.
        }
      }
    } finally {
      requestInProgressRef.current = false
      setIsAttacking(false)
    }
  }, [isActive, isAdjacent, partida.id, token])

  useGameKeyboard(isActive && !isAbandoning, handleMove, handleAttack)

  async function handleAbandon() {
    if (requestInProgressRef.current || !window.confirm('¿Querés abandonar esta partida? No vas a poder retomarla.')) {
      return
    }

    requestInProgressRef.current = true
    setIsAbandoning(true)
    setError('')
    try {
      await finalizePartida(token, partida.id)
      onBackToPartidas()
    } catch (requestError) {
      setError(requestError.message)
      setIsAbandoning(false)
    } finally {
      requestInProgressRef.current = false
    }
  }

  const characterName = gameState?.personajeNombre || personaje.nombre
  const isBusy = isMoving || isAttacking || isAbandoning
  const combatPanel = gameState && (
    <CombatPanel
      enemyName={gameState.enemigoNombre}
      isActive={isActive}
      isAdjacent={isAdjacent}
      isAttacking={isAttacking}
      isBusy={isBusy}
      isTouchDevice={isTouchDevice}
      lastAttack={lastAttack}
      onAttack={handleAttack}
    />
  )

  return (
    <main className="min-h-screen bg-stone-950 px-4 py-3 sm:px-8">
      <section className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-3 border-b border-stone-700 pb-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-amber-500">Marrow · Partida #{partida.id}</p>
            <h1 className="mt-1 font-serif text-3xl font-bold text-amber-50">La entrada a la mazmorra</h1>
            <p className="mt-1 text-sm text-stone-400">
              {gameState && !isActive ? `La aventura de ${characterName} terminó.` : `${characterName} se prepara para avanzar.`}
            </p>
          </div>
          <button className="rounded-sm border border-stone-600 px-4 py-2 font-serif font-semibold text-stone-200 transition hover:border-amber-500 hover:text-amber-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-400" type="button" onClick={onBackToPartidas}>Volver a partidas</button>
        </header>

        <div className="mt-4">
          {isLoading && <p className="stone-panel rounded p-4 text-sm text-stone-300" role="status">Cargando el estado de la partida...</p>}
          {!gameState && <FormMessage>{error}</FormMessage>}
          {!isLoading && gameState && (
            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
              <div className="min-w-0 space-y-4">
                <GameScene
                  characterName={gameState.personajeNombre}
                  enemyName={gameState.enemigoNombre}
                  characterPosition={{ x: gameState.personajeX, y: gameState.personajeY }}
                  enemyPosition={{ x: gameState.enemigoX, y: gameState.enemigoY }}
                >
                  {!isActive && (
                    <GameResultPanel resultado={gameState.resultadoCombate} enemyName={gameState.enemigoNombre} onBackToPartidas={onBackToPartidas} />
                  )}
                </GameScene>
                {isTouchDevice && (
                  <>
                    <FormMessage>{error}</FormMessage>
                    <div className="grid grid-cols-[auto_minmax(0,1fr)] gap-3">
                      <TouchControls isActive={isActive} disabled={!isActive || isBusy} onDirection={handleMove} />
                      {combatPanel}
                    </div>
                  </>
                )}
                <GameHud
                  characterName={gameState.personajeNombre}
                  characterLevel={gameState.personajeNivel}
                  characterHealth={{ current: gameState.vidaPersonajeActual, maximum: gameState.vidaPersonajeMaxima }}
                  enemyName={gameState.enemigoNombre}
                  enemyLevel={gameState.enemigoNivel}
                  enemyHealth={{ current: gameState.vidaEnemigoActual, maximum: gameState.vidaEnemigoMaxima }}
                />
              </div>
              <aside className="space-y-4">
                {!isTouchDevice && (
                  <>
                    <FormMessage>{error}</FormMessage>
                    {combatPanel}
                  </>
                )}
                <GameInfoPanel gameState={gameState} isMoving={isMoving} isBusy={isBusy} isAbandoning={isAbandoning} isTouchDevice={isTouchDevice} onAbandon={handleAbandon} />
              </aside>
            </div>
          )}
        </div>
      </section>
    </main>
  )
}
