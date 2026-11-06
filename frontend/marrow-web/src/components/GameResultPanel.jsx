function getResult(resultado, enemyName) {
  if (resultado === 'Victoria') {
    return { title: 'Victoria', message: `Derrotaste a ${enemyName}.`, color: 'text-amber-200' }
  }

  if (resultado === 'Derrota') {
    return { title: 'Derrota', message: `${enemyName} acabó con tu guerrero.`, color: 'text-red-400' }
  }

  return { title: 'Partida finalizada', message: 'Esta partida terminó sin un resultado de combate.', color: 'text-stone-200' }
}

export default function GameResultPanel({ resultado, enemyName, reward, onBackToPartidas }) {
  const { title, message, color } = getResult(resultado, enemyName)

  return (
    <div className="absolute inset-0 z-20 flex items-center justify-center bg-black/75 p-4" role="group" aria-label="Resultado de la partida">
      <div className="stone-panel w-full max-w-sm rounded p-4 text-center sm:p-6">
        <h2 className={`font-serif text-2xl font-bold sm:text-4xl ${color}`}>{title}</h2>
        <p className="mt-2 text-sm text-stone-300">{message}</p>
        {reward && (
          <p className="mt-1 text-sm font-semibold text-amber-300">
            +{reward.experience} de experiencia{reward.newLevel ? ` · ¡Nivel ${reward.newLevel}!` : ''}
          </p>
        )}
        <button
          className="mt-4 rounded-sm border border-stone-500 px-4 py-2 font-serif font-semibold text-stone-100 transition hover:border-amber-500 hover:text-amber-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-400"
          type="button"
          onClick={onBackToPartidas}
        >
          Volver al listado de partidas
        </button>
      </div>
    </div>
  )
}
