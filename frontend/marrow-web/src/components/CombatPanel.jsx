export default function CombatPanel({ enemyName, isActive, isAdjacent, isAttacking, isBusy, isTouchDevice, lastAttack, onAttack }) {
  const attackHint = isTouchDevice ? 'Tocá el botón para atacar.' : 'Presioná Espacio o el botón para atacar.'

  return (
    <section className="stone-panel rounded p-4" aria-label="Combate">
      <h2 className="font-serif text-lg font-semibold text-amber-100">Combate</h2>

      {isActive && (
        <>
          <button
            className="mt-3 w-full rounded-sm border border-amber-600 bg-amber-900/60 px-4 py-3 font-serif text-lg font-semibold text-amber-50 transition hover:bg-amber-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-300 disabled:cursor-not-allowed disabled:border-stone-700 disabled:bg-stone-900 disabled:text-stone-500"
            type="button"
            onClick={onAttack}
            disabled={!isAdjacent || isBusy}
          >
            {isAttacking ? 'Atacando...' : 'Atacar'}
          </button>
          <p className="mt-2 text-sm text-stone-400">
            {isAdjacent ? attackHint : `Acercate a ${enemyName} para atacar.`}
          </p>
        </>
      )}

      {lastAttack && (
        <div className="mt-3 border-t border-stone-800 pt-3 text-sm font-semibold" role="status">
          <p className="text-amber-200">Golpeaste a {enemyName} por {lastAttack.danioInfligido} de daño.</p>
          {lastAttack.danioRecibido > 0 && (
            <p className="mt-1 text-red-300">{enemyName} te golpeó por {lastAttack.danioRecibido} de daño.</p>
          )}
        </div>
      )}
    </section>
  )
}
