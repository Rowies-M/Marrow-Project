import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '../context/useAuth.js'
import BattleEndPanel from '../components/BattleEndPanel.jsx'
import CombatPanel from '../components/CombatPanel.jsx'
import FormMessage from '../components/FormMessage.jsx'
import GameHud from '../components/GameHud.jsx'
import GameInfoPanel from '../components/GameInfoPanel.jsx'
import GameResultPanel from '../components/GameResultPanel.jsx'
import GameScene from '../components/GameScene.jsx'
import TouchControls from '../components/TouchControls.jsx'
import useGameKeyboard from '../hooks/useGameKeyboard.js'
import useIsTouchDevice from '../hooks/useIsTouchDevice.js'
import { attackPartida, finalizePartida, getPartidaEstado, movePartida, powerStrikePartida } from '../services/partidaService.js'

// Solo guía la interfaz: la decisión final sobre el ataque la toma el backend.
function isEnemyAdjacent(gameState) {
  return Math.abs(gameState.personajeX - gameState.enemigoX) + Math.abs(gameState.personajeY - gameState.enemigoY) === 1
}

const DIRECTION_OFFSETS = { arriba: [0, -1], abajo: [0, 1], izquierda: [-1, 0], derecha: [1, 0] }

// Solo decide la animación: el bloqueo y el contraataque los resuelve el backend.
function targetsEnemyCell(gameState, direction) {
  const [offsetX, offsetY] = DIRECTION_OFFSETS[direction] ?? [0, 0]
  return gameState?.personajeX + offsetX === gameState?.enemigoX && gameState?.personajeY + offsetY === gameState?.enemigoY
}

export default function GamePage({ partida, personaje, onBackToPartidas }) {
  const { token } = useAuth()
  const isTouchDevice = useIsTouchDevice()
  const [gameState, setGameState] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isMoving, setIsMoving] = useState(false)
  const [activeAttack, setActiveAttack] = useState(null)
  const [isAbandoning, setIsAbandoning] = useState(false)
  const [lastAttack, setLastAttack] = useState(null)
  const [reward, setReward] = useState(null)
  const [loadAttempt, setLoadAttempt] = useState(0)
  // Solo visual: qué animación muestra el Guerrero y qué destellos se reproducen.
  const [warriorAction, setWarriorAction] = useState(null)
  const [powerFlashId, setPowerFlashId] = useState(0)
  const [damageFlashId, setDamageFlashId] = useState(0)
  const warriorActionIdRef = useRef(0)
  // Solo visual: contraataque y destello del enemigo.
  const [enemyAction, setEnemyAction] = useState(null)
  const [enemyDamageFlashId, setEnemyDamageFlashId] = useState(0)
  const enemyActionIdRef = useRef(0)
  // Al ganar o perder, el popup de resultado espera a que el jugador pulse «Continuar».
  const [showResultDialog, setShowResultDialog] = useState(false)
  const requestInProgressRef = useRef(false)

  const startWarriorAction = useCallback((type) => {
    warriorActionIdRef.current += 1
    setWarriorAction({ type, id: warriorActionIdRef.current })
  }, [])

  const handleWarriorActionEnd = useCallback((id) => {
    setWarriorAction((current) => (current?.id === id ? null : current))
  }, [])

  const startEnemyAction = useCallback((type) => {
    enemyActionIdRef.current += 1
    setEnemyAction({ type, id: enemyActionIdRef.current })
  }, [])

  const handleEnemyActionEnd = useCallback((id) => {
    setEnemyAction((current) => (current?.id === id ? null : current))
  }, [])

  useEffect(() => {
    let cancelled = false

    async function loadGameState() {
      setGameState(null)
      setError('')
      setIsLoading(true)
      // Cada carga empieza sin restos visuales: el resultado final se deriva solo de esta partida.
      setShowResultDialog(false)
      setWarriorAction(null)
      setEnemyAction(null)
      setLastAttack(null)
      setReward(null)

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
  }, [loadAttempt, partida.id, token])

  const handleMove = useCallback(async (direction) => {
    if (requestInProgressRef.current) {
      return
    }

    requestInProgressRef.current = true
    setIsMoving(true)
    // Contra la casilla del enemigo el Guerrero no camina: queda quieto y espera el contraataque.
    if (!targetsEnemyCell(gameState, direction)) {
      startWarriorAction('walk')
    }
    setError('')
    setLastAttack(null)
    try {
      const response = await movePartida(token, partida.id, direction)
      setGameState(response.estado)
      // Igual que tras un ataque: la animación y el destello dependen del daño que confirmó el backend.
      if (response.danioRecibido > 0) {
        setDamageFlashId((current) => current + 1)
        startEnemyAction('attack')
      }
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      requestInProgressRef.current = false
      setIsMoving(false)
    }
  }, [gameState, partida.id, startEnemyAction, startWarriorAction, token])

  const isActive = gameState?.estado === 'Activa'
  // Solo una victoria o una derrota de esta partida; un abandono no tiene resultado de combate.
  const combatOutcome = !isActive && (gameState?.resultadoCombate === 'Victoria' || gameState?.resultadoCombate === 'Derrota')
    ? gameState.resultadoCombate
    : null
  const isAdjacent = gameState ? isEnemyAdjacent(gameState) : false
  const characterLevel = gameState?.personajeNivel

  const powerStrikeUnlocked = gameState?.golpePoderosoDesbloqueado === true

  const executeAttack = useCallback(async (attackType, sendAttack) => {
    if (requestInProgressRef.current || !isActive || !isAdjacent) {
      return
    }

    requestInProgressRef.current = true
    setActiveAttack(attackType)
    startWarriorAction('attack')
    setError('')
    try {
      const response = await sendAttack(token, partida.id)
      setGameState(response.estado)
      setLastAttack({ danioInfligido: response.danioInfligido, danioRecibido: response.danioRecibido, isPowerStrike: attackType === 'power' })
      // Los destellos dependen de lo que confirmó el backend, no del botón presionado.
      if (attackType === 'power') {
        setPowerFlashId((current) => current + 1)
      }
      if (response.danioRecibido > 0) {
        setDamageFlashId((current) => current + 1)
        startEnemyAction('attack')
      }
      if (response.danioInfligido > 0) {
        setEnemyDamageFlashId((current) => current + 1)
      }
      if (response.experienciaGanada > 0) {
        setReward({
          experience: response.experienciaGanada,
          newLevel: response.estado.personajeNivel > characterLevel ? response.estado.personajeNivel : null,
        })
      }
    } catch (requestError) {
      setError(requestError.message)
      setLastAttack(null)
      if (requestError.status === 409 || !requestError.status) {
        // Tras un rechazo o un corte de red, la pantalla puede estar desactualizada: se vuelve a leer el estado real.
        try {
          setGameState(await getPartidaEstado(token, partida.id))
        } catch {
          // El error del ataque ya está a la vista y se conserva el último estado confirmado.
        }
      }
    } finally {
      requestInProgressRef.current = false
      setActiveAttack(null)
    }
  }, [characterLevel, isActive, isAdjacent, partida.id, startEnemyAction, startWarriorAction, token])

  const handleAttack = useCallback(() => executeAttack('basic', attackPartida), [executeAttack])
  const handlePowerStrike = useCallback(() => executeAttack('power', powerStrikePartida), [executeAttack])

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
  const isBusy = isMoving || activeAttack !== null || isAbandoning
  const combatPanel = gameState && (
    <CombatPanel
      enemyName={gameState.enemigoNombre}
      isActive={isActive}
      isAdjacent={isAdjacent}
      isBusy={isBusy}
      isTouchDevice={isTouchDevice}
      activeAttack={activeAttack}
      lastAttack={lastAttack}
      powerStrikeUnlocked={powerStrikeUnlocked}
      powerStrikeRequiredLevel={gameState.golpePoderosoNivelRequerido}
      onAttack={handleAttack}
      onPowerStrike={handlePowerStrike}
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
          {!gameState && !isLoading && error && (
            <div className="space-y-3">
              <FormMessage>{error}</FormMessage>
              <button className="rounded-sm border border-stone-600 px-4 py-2 font-serif font-semibold text-stone-200 transition hover:border-amber-500 hover:text-amber-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-400" type="button" onClick={() => setLoadAttempt((current) => current + 1)}>
                Reintentar
              </button>
            </div>
          )}
          {!isLoading && gameState && (
            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_18rem]">
              <div className="min-w-0 space-y-4">
                <GameScene
                  characterName={gameState.personajeNombre}
                  enemyName={gameState.enemigoNombre}
                  characterPosition={{ x: gameState.personajeX, y: gameState.personajeY }}
                  enemyPosition={{ x: gameState.enemigoX, y: gameState.enemigoY }}
                  warriorAction={warriorAction}
                  powerFlashId={powerFlashId}
                  damageFlashId={damageFlashId}
                  onWarriorActionEnd={handleWarriorActionEnd}
                  enemyAction={enemyAction}
                  enemyDamageFlashId={enemyDamageFlashId}
                  onEnemyActionEnd={handleEnemyActionEnd}
                  combatOutcome={combatOutcome}
                >
                  {!isActive && combatOutcome && !showResultDialog && (
                    <BattleEndPanel resultado={combatOutcome} onContinue={() => setShowResultDialog(true)} />
                  )}
                  {!isActive && (!combatOutcome || showResultDialog) && (
                    <GameResultPanel resultado={gameState.resultadoCombate} enemyName={gameState.enemigoNombre} reward={reward} onBackToPartidas={onBackToPartidas} />
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
                  characterMaxLevel={gameState.personajeNivelMaximo}
                  characterHealth={{ current: gameState.vidaPersonajeActual, maximum: gameState.vidaPersonajeMaxima }}
                  characterExperience={{ current: gameState.personajeExperiencia, maximum: gameState.personajeExperienciaSiguienteNivel }}
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
