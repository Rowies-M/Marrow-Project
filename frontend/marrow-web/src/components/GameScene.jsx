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

function WarriorFigure() {
  return (
    <svg className="h-full w-full drop-shadow-[0_0_6px_rgba(251,191,36,0.55)]" viewBox="0 0 24 24" aria-hidden="true">
      <path d="M12 2 20 5v6c0 5-3.4 9-8 11-4.6-2-8-6-8-11V5z" fill="#57534e" stroke="#fbbf24" strokeWidth="1.2" />
      <path d="M12 6v12M8.5 10h7" fill="none" stroke="#fef3c7" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
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

export default function GameScene({ characterName, enemyName, characterPosition, enemyPosition, children }) {
  return (
    <section className="dungeon-floor relative mx-auto aspect-[11/7] w-full max-w-[calc(60svh*11/7)] rounded border-4 border-stone-700" aria-label="Escenario de juego">
      <div className="absolute inset-x-[calc(100%/22)] inset-y-[calc(100%/14)]">
        <Actor label={enemyName} name={enemyName} position={enemyPosition} nameColor="text-red-200">
          <EnemyFigure />
        </Actor>
        <Actor label={`Guerrero ${characterName}`} name={characterName} position={characterPosition} nameColor="text-amber-100">
          <WarriorFigure />
        </Actor>
      </div>
      {children}
    </section>
  )
}
