// Panel lateral al terminar el combate: ocupa el borde izquierdo de la escena, lejos de las columnas
// centrales donde quedan el Guerrero y el enemigo, para que los dos sigan a la vista.
export default function BattleEndPanel({ resultado, onContinue }) {
  const isVictory = resultado === 'Victoria'

  return (
    <div
      className="stone-panel absolute left-[2%] top-1/2 z-20 w-[31%] -translate-y-1/2 rounded p-2 text-center sm:p-3"
      role="group"
      aria-label="Fin del combate"
    >
      <p className={`font-serif text-sm font-bold sm:text-xl ${isVictory ? 'text-amber-200' : 'text-red-400'}`}>
        {isVictory ? '¡Victoria!' : '¡Derrota!'}
      </p>
      <p className="mt-1 text-[0.65rem] text-stone-300 sm:text-sm">¿Deseás continuar?</p>
      <button
        className="mt-2 w-full rounded-sm border border-stone-500 px-2 py-1 font-serif text-xs font-semibold text-stone-100 transition hover:border-amber-500 hover:text-amber-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-400 sm:text-sm"
        type="button"
        onClick={onContinue}
      >
        Continuar
      </button>
    </div>
  )
}
