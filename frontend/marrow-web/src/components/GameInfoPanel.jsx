import { formatDate } from '../utils/formatDate.js'

const DIFFICULTY_LABELS = { Normal: 'Normal', Dificil: 'Difícil' }

function InfoRow({ label, children }) {
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-stone-800 py-2 last:border-b-0">
      <dt className="text-sm text-stone-400">{label}</dt>
      <dd className="text-right text-sm font-semibold text-stone-100">{children}</dd>
    </div>
  )
}

export default function GameInfoPanel({ gameState, isMoving, isBusy, isAbandoning, isTouchDevice, onAbandon }) {
  const isActive = gameState.estado === 'Activa'

  return (
    <section className="stone-panel rounded p-4" aria-label="Información de la partida">
      <h2 className="font-serif text-lg font-semibold text-amber-100">Crónica de la partida</h2>
      <dl className="mt-2">
        <InfoRow label="Partida">#{gameState.partidaId}</InfoRow>
        <InfoRow label="Estado">{gameState.estado}</InfoRow>
        <InfoRow label="Dificultad">
          <span className="rounded-sm border border-amber-500/60 px-2 py-0.5 text-amber-200">
            {DIFFICULTY_LABELS[gameState.dificultad] ?? gameState.dificultad}
          </span>
        </InfoRow>
        <InfoRow label="Iniciada">{formatDate(gameState.fechaCreacion)}</InfoRow>
        {gameState.resultadoCombate && <InfoRow label="Resultado">{gameState.resultadoCombate}</InfoRow>}
      </dl>

      {isActive && (
        <p className="mt-3 text-sm text-stone-400">
          {isTouchDevice ? 'Movete con las flechas de la pantalla.' : 'Movete con W, A, S, D o con las flechas.'}
        </p>
      )}
      <p className="mt-1 min-h-5 text-sm text-stone-400" role="status">{isMoving ? 'Guardando movimiento...' : ''}</p>

      {isActive && (
        <button
          className="mt-3 w-full rounded-sm border border-red-800 bg-red-950/60 px-4 py-2 font-serif font-semibold text-red-100 transition hover:bg-red-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-300 disabled:cursor-wait"
          type="button"
          onClick={onAbandon}
          disabled={isBusy}
        >
          {isAbandoning ? 'Abandonando...' : 'Abandonar partida'}
        </button>
      )}
    </section>
  )
}
