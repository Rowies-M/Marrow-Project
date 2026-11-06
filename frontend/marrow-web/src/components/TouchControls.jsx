const CONTROLS = [
  { direction: 'arriba', label: 'Mover arriba', rotation: '', position: 'col-start-2 row-start-1' },
  { direction: 'izquierda', label: 'Mover a la izquierda', rotation: '-rotate-90', position: 'col-start-1 row-start-2' },
  { direction: 'derecha', label: 'Mover a la derecha', rotation: 'rotate-90', position: 'col-start-3 row-start-2' },
  { direction: 'abajo', label: 'Mover abajo', rotation: 'rotate-180', position: 'col-start-2 row-start-3' },
]

export default function TouchControls({ isActive, disabled, onDirection }) {
  return (
    <section className={`grid w-fit grid-cols-[repeat(3,3rem)] grid-rows-[repeat(3,3rem)] gap-1 self-center ${isActive ? '' : 'opacity-40'}`} aria-label="Controles táctiles">
      {CONTROLS.map(({ direction, label, rotation, position }) => (
        <button
          key={direction}
          className={`stone-panel flex touch-manipulation items-center justify-center rounded text-amber-100 active:bg-stone-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-400 ${position}`}
          type="button"
          aria-label={label}
          disabled={disabled}
          onClick={() => onDirection(direction)}
        >
          <svg className={`h-5 w-5 ${rotation}`} viewBox="0 0 24 24" aria-hidden="true">
            <path d="M12 5 21 19H3z" fill="currentColor" />
          </svg>
        </button>
      ))}
    </section>
  )
}
