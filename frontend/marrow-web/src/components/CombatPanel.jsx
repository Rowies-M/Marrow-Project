const ATTACK_BUTTON_CLASS = 'w-full rounded-sm border border-amber-600 bg-amber-900/60 px-4 py-3 font-serif text-lg font-semibold text-amber-50 transition hover:bg-amber-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-300 disabled:cursor-not-allowed disabled:border-stone-700 disabled:bg-stone-900 disabled:text-stone-500'

export default function CombatPanel({
  enemyName,
  isActive,
  isAdjacent,
  isBusy,
  isTouchDevice,
  activeAttack,
  lastAttack,
  powerStrikeUnlocked,
  powerStrikeRequiredLevel,
  onAttack,
  onPowerStrike,
}) {
  const attackHint = isTouchDevice ? 'Tocá el botón para atacar.' : 'Presioná Espacio o el botón para atacar.'

  return (
    <section className="stone-panel rounded p-4" aria-label="Combate">
      <h2 className="font-serif text-lg font-semibold text-amber-100">Combate</h2>

      {isActive && (
        <>
          <button
            className={`mt-3 ${ATTACK_BUTTON_CLASS}`}
            type="button"
            onClick={onAttack}
            disabled={!isAdjacent || isBusy}
          >
            {activeAttack === 'basic' ? 'Atacando...' : 'Atacar'}
          </button>
          <button
            className={`mt-2 ${ATTACK_BUTTON_CLASS}`}
            type="button"
            onClick={onPowerStrike}
            disabled={!powerStrikeUnlocked || !isAdjacent || isBusy}
          >
            {activeAttack === 'power' ? 'Golpeando...' : 'Golpe poderoso'}
          </button>
          <p className="mt-2 text-sm text-stone-400">
            {isAdjacent ? attackHint : `Acercate a ${enemyName} para atacar.`}
          </p>
          <p className="mt-1 text-sm text-stone-400">
            {powerStrikeUnlocked
              ? 'Golpe poderoso disponible: duplica tu ataque.'
              : `Golpe poderoso bloqueado: se desbloquea en el nivel ${powerStrikeRequiredLevel}.`}
          </p>
        </>
      )}

      {lastAttack && (
        <div className="mt-3 border-t border-stone-800 pt-3 text-sm font-semibold" role="status">
          <p className="text-amber-200">
            {lastAttack.isPowerStrike
              ? `Tu Golpe poderoso le hizo ${lastAttack.danioInfligido} de daño a ${enemyName}.`
              : `Golpeaste a ${enemyName} por ${lastAttack.danioInfligido} de daño.`}
          </p>
          {lastAttack.danioRecibido > 0 && (
            <p className="mt-1 text-red-300">{enemyName} te golpeó por {lastAttack.danioRecibido} de daño.</p>
          )}
        </div>
      )}
    </section>
  )
}
