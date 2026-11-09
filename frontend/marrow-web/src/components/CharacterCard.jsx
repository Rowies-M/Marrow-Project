export default function CharacterCard({ character, isSelected, onSelect }) {
  return (
    <button
      className={`w-full rounded-2xl border p-5 text-left transition ${isSelected
        ? 'border-violet-400 bg-violet-400/10 shadow-lg shadow-violet-950/30'
        : 'border-slate-700 bg-slate-900 hover:border-violet-400/60 hover:bg-slate-800'}`}
      type="button"
      onClick={() => onSelect(character)}
      aria-pressed={isSelected}
    >
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-xl font-bold text-white">{character.nombre}</p>
          <p className="mt-1 text-sm text-violet-300">{character.clase}</p>
        </div>
        <span className="rounded-full bg-slate-800 px-3 py-1 text-sm font-semibold text-slate-200">
          Nivel {character.nivel}/{character.nivelMaximo}
        </span>
      </div>
      <div className="mt-5 grid grid-cols-3 gap-3 text-sm">
        <div className="rounded-xl bg-slate-950/60 p-3">
          <p className="text-slate-500">Vida</p>
          <p className="mt-1 font-semibold text-white">{character.vidaActual}/{character.vidaMaxima}</p>
        </div>
        <div className="rounded-xl bg-slate-950/60 p-3">
          <p className="text-slate-500">Ataque</p>
          <p className="mt-1 font-semibold text-white">{character.ataque}</p>
        </div>
        <div className="rounded-xl bg-slate-950/60 p-3">
          <p className="text-slate-500">Defensa</p>
          <p className="mt-1 font-semibold text-white">{character.defensa}</p>
        </div>
      </div>
    </button>
  )
}
