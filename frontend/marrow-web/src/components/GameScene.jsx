import { useEffect, useRef } from 'react'
import attackStrip from '../assets/sprites/warrior/Hero_Knight_Attack_south_strip.png'
import idleStrip from '../assets/sprites/warrior/Hero_Knight_Idle_south_strip.png'
import walkStrip from '../assets/sprites/warrior/Hero_Knight_Walk_east_strip.png'
import goblinAttackStrip from '../assets/sprites/goblin/attack_south_strip.png'
import goblinIdleStrip from '../assets/sprites/goblin/idle_south_strip.png'
import orcAttackStrip from '../assets/sprites/orc/troll_attack_strip.png'
import orcIdleFrame from '../assets/sprites/orc/troll_idle_front.png'
import skeletonAttackSheet from '../assets/sprites/skeleton/skeleton_attack_sheet.png'
import skeletonIdleSheet from '../assets/sprites/skeleton/skeleton_idle_sheet.png'

const LAST_COLUMN = 10
const LAST_ROW = 6

function getPositionStyle(position) {
  const x = Math.max(0, Math.min(LAST_COLUMN, Number(position?.x) || 0))
  const y = Math.max(0, Math.min(LAST_ROW, Number(position?.y) || 0))

  return {
    left: `${(x / LAST_COLUMN) * 100}%`,
    top: `${(y / LAST_ROW) * 100}%`,
    transform: 'translate(-50%, -50%)',
  }
}

const WARRIOR_SPRITES = {
  idle: { image: idleStrip, className: 'warrior-sprite-idle' },
  walk: { image: walkStrip, className: 'warrior-sprite-walk' },
  attack: { image: attackStrip, className: 'warrior-sprite-attack' },
}

const POWER_FLASH = [
  { filter: 'none' },
  { filter: 'drop-shadow(0 0 8px rgb(250 204 21)) brightness(1.7)', offset: 0.3 },
  { filter: 'none' },
]

const DAMAGE_FLASH = [
  { filter: 'none' },
  { filter: 'drop-shadow(0 0 8px rgb(239 68 68)) sepia(1) saturate(6) hue-rotate(-40deg)', offset: 0.3 },
  { filter: 'none' },
]

// Efectos finales: los mismos colores que los destellos, pero fijos mientras se muestra el resultado.
const WINNER_FILTER = POWER_FLASH[1].filter
const LOSER_FILTER = DAMAGE_FLASH[1].filter

function finalFilter(outcome, wins) {
  if (!outcome) {
    return undefined
  }

  return (outcome === 'Victoria') === wins ? WINNER_FILTER : LOSER_FILTER
}

// Cada cambio de id reproduce el destello una vez, sin volver a montar el sprite.
function useFlash(ref, id, keyframes, delay) {
  useEffect(() => {
    if (id > 0) {
      ref.current?.animate(keyframes, { duration: 450, delay, easing: 'ease-out' })
    }
  }, [ref, id, keyframes, delay])
}

function WarriorSprite({ action, powerFlashId, damageFlashId, onActionEnd }) {
  const powerRef = useRef(null)
  const damageRef = useRef(null)
  const sprite = WARRIOR_SPRITES[action?.type] ?? WARRIOR_SPRITES.idle

  useFlash(powerRef, powerFlashId, POWER_FLASH, 0)
  // El contraataque llega después del golpe: el destello rojo empieza un poco más tarde.
  useFlash(damageRef, damageFlashId, DAMAGE_FLASH, 200)

  return (
    <div ref={powerRef} className="absolute inset-0">
      <div ref={damageRef} className="absolute inset-0">
        <div
          key={action?.id ?? 'idle'}
          className={`warrior-sprite ${sprite.className} absolute left-1/2 top-1/2 aspect-[80/82] h-full -translate-x-1/2 -translate-y-1/2 drop-shadow-[0_0_4px_rgba(251,191,36,0.45)]`}
          style={{ backgroundImage: `url(${sprite.image})` }}
          onAnimationEnd={() => action && onActionEnd(action.id)}
          aria-hidden="true"
        />
      </div>
    </div>
  )
}

const GOBLIN_SPRITES = {
  idle: { image: goblinIdleStrip, className: 'goblin-sprite-idle' },
  attack: { image: goblinAttackStrip, className: 'goblin-sprite-attack' },
}

// La figura ocupa unos 114 × 155 px de cada cuadro de 214 × 210: el cuadro se dibuja al 122 %
// de la casilla para que el Goblin tenga una altura similar a la del Guerrero.
function GoblinSprite({ action, damageFlashId, onActionEnd }) {
  const damageRef = useRef(null)
  const sprite = GOBLIN_SPRITES[action?.type] ?? GOBLIN_SPRITES.idle

  useFlash(damageRef, damageFlashId, DAMAGE_FLASH, 0)

  return (
    <div ref={damageRef} className="absolute inset-0">
      <div
        key={action?.id ?? 'idle'}
        className={`goblin-sprite ${sprite.className} absolute left-1/2 top-1/2 aspect-[214/210] h-[122%] -translate-x-1/2 -translate-y-1/2`}
        style={{ backgroundImage: `url(${sprite.image})` }}
        onAnimationEnd={() => action && onActionEnd(action.id)}
        aria-hidden="true"
      />
    </div>
  )
}

const ORC_SPRITES = {
  idle: { image: orcIdleFrame, className: 'orc-sprite-idle' },
  attack: { image: orcAttackStrip, className: 'orc-sprite-attack' },
}

// Dark Troll Boss: la figura ocupa unos 216 × 229 px de cada cuadro de 256 × 256. Se dibuja al 115 %
// de la casilla para que se vea como un jefe sin invadir las casillas vecinas. Idle y Attack comparten
// el tamaño de cuadro, así que no hay saltos al pasar de uno a otro.
function OrcSprite({ action, damageFlashId, onActionEnd }) {
  const damageRef = useRef(null)
  const sprite = ORC_SPRITES[action?.type] ?? ORC_SPRITES.idle

  useFlash(damageRef, damageFlashId, DAMAGE_FLASH, 0)

  return (
    <div ref={damageRef} className="absolute inset-0">
      <div
        key={action?.id ?? 'idle'}
        className={`orc-sprite ${sprite.className} absolute left-1/2 top-1/2 aspect-square h-[115%] -translate-x-1/2 -translate-y-1/2`}
        style={{ backgroundImage: `url(${sprite.image})` }}
        onAnimationEnd={() => action && onActionEnd(action.id)}
        aria-hidden="true"
      />
    </div>
  )
}

const SKELETON_SPRITES = {
  idle: { image: skeletonIdleSheet, className: 'skeleton-sprite-idle' },
  attack: { image: skeletonAttackSheet, className: 'skeleton-sprite-attack' },
}

// Cuadros de 75 × 50 px con el cuerpo a la izquierda (centro en x ≈ 28,5): el desplazamiento del 38 %
// centra el cuerpo en la casilla. Se dibuja al 85 % de alto para que la lanza del ataque, que llega
// al borde derecho del cuadro, no pase de la casilla vecina.
function SkeletonSprite({ action, damageFlashId, onActionEnd }) {
  const damageRef = useRef(null)
  const sprite = SKELETON_SPRITES[action?.type] ?? SKELETON_SPRITES.idle

  useFlash(damageRef, damageFlashId, DAMAGE_FLASH, 0)

  return (
    <div ref={damageRef} className="absolute inset-0">
      <div
        key={action?.id ?? 'idle'}
        className={`skeleton-sprite ${sprite.className} absolute left-1/2 top-1/2 aspect-[75/50] h-[85%] -translate-x-[38%] -translate-y-1/2`}
        style={{ backgroundImage: `url(${sprite.image})` }}
        onAnimationEnd={() => action && onActionEnd(action.id)}
        aria-hidden="true"
      />
    </div>
  )
}

function EnemyFigure() {
  return (
    <svg className="h-full w-full drop-shadow-[0_0_6px_rgba(239,68,68,0.65)]" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 2c0 4 1.2 6.5 3.6 8M19 2c0 4-1.2 6.5-3.6 8" fill="none" stroke="#fca5a5" strokeLinecap="round" strokeWidth="1.8" />
      <path d="M12 6c4.4 0 7 3 7 6.8 0 2.6-1.4 4.4-3 5.3V21H8v-2.9c-1.6-.9-3-2.7-3-5.3C5 9 7.6 6 12 6z" fill="#450a0a" stroke="#ef4444" strokeWidth="1.2" />
      <circle cx="9" cy="13" r="1.6" fill="#fecaca" />
      <circle cx="15" cy="13" r="1.6" fill="#fecaca" />
    </svg>
  )
}

function Actor({ label, name, position, nameColor, children }) {
  const namePosition = position?.y === 0 ? 'top-full mt-1' : 'bottom-full mb-1'

  return (
    <div
      className="absolute aspect-square w-[8%] transition-[left,top] duration-150 ease-out motion-reduce:transition-none"
      style={getPositionStyle(position)}
      role="img"
      aria-label={label}
    >
      {children}
      <p className={`absolute left-1/2 hidden max-w-28 -translate-x-1/2 truncate rounded bg-black/75 px-2 py-0.5 font-serif text-xs ${nameColor} sm:block ${namePosition}`}>
        {name}
      </p>
    </div>
  )
}

export default function GameScene({
  characterName,
  enemyName,
  characterPosition,
  enemyPosition,
  warriorAction,
  powerFlashId = 0,
  damageFlashId = 0,
  onWarriorActionEnd = () => {},
  enemyAction,
  enemyDamageFlashId = 0,
  onEnemyActionEnd = () => {},
  combatOutcome = null,
  children,
}) {
  // Derivado solo del resultado de la partida actual: sin resultado, ningún personaje queda teñido.
  const warriorFilter = finalFilter(combatOutcome, true)
  const enemyFilter = finalFilter(combatOutcome, false)

  return (
    <section className="dungeon-floor relative mx-auto aspect-[11/7] w-full max-w-[calc(60svh*11/7)] rounded border-4 border-stone-700" aria-label="Escenario de juego">
      <div className="absolute inset-x-[calc(100%/22)] inset-y-[calc(100%/14)]">
        <Actor label={enemyName} name={enemyName} position={enemyPosition} nameColor="text-red-200">
          <div className="absolute inset-0 transition-[filter] duration-300" style={{ filter: enemyFilter }}>
            {/* Goblin, Esqueleto y Orco tienen sprites; el SVG queda solo para un nombre desconocido. */}
            {enemyName === 'Goblin' && <GoblinSprite action={enemyAction} damageFlashId={enemyDamageFlashId} onActionEnd={onEnemyActionEnd} />}
            {enemyName === 'Esqueleto' && <SkeletonSprite action={enemyAction} damageFlashId={enemyDamageFlashId} onActionEnd={onEnemyActionEnd} />}
            {enemyName === 'Orco' && <OrcSprite action={enemyAction} damageFlashId={enemyDamageFlashId} onActionEnd={onEnemyActionEnd} />}
            {enemyName !== 'Goblin' && enemyName !== 'Esqueleto' && enemyName !== 'Orco' && <EnemyFigure />}
          </div>
        </Actor>
        <Actor label={`Guerrero ${characterName}`} name={characterName} position={characterPosition} nameColor="text-amber-100">
          <div className="absolute inset-0 transition-[filter] duration-300" style={{ filter: warriorFilter }}>
            <WarriorSprite action={warriorAction} powerFlashId={powerFlashId} damageFlashId={damageFlashId} onActionEnd={onWarriorActionEnd} />
          </div>
        </Actor>
      </div>
      {children}
    </section>
  )
}
