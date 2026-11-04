export default function GameHud({ character }) {
  const currentHealth = Number.isFinite(character.vidaActual) ? character.vidaActual : 0
  const maximumHealth = Number.isFinite(character.vidaMaxima) ? character.vidaMaxima : 0
  const healthPercentage = maximumHealth > 0 ? Math.max(0, Math.min(100, (currentHealth / maximumHealth) * 100)) : 0

  return (
    <section className="rounded-2xl border border-violet-400/20 bg-slate-900 p-5" aria-label="HUD del Guerrero">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-violet-300">Guerrero</p>
          <h2 className="mt-1 text-xl font-bold text-white">{character.nombre}</h2>
        </div>
        <p className="text-sm font-semibold text-slate-300">Vida: {currentHealth}/{maximumHealth}</p>
      </div>
      <div className="mt-4 h-3 overflow-hidden rounded-full bg-slate-950" role="progressbar" aria-label="Vida del Guerrero" aria-valuemin="0" aria-valuemax={maximumHealth} aria-valuenow={currentHealth}>
        <div className="h-full rounded-full bg-emerald-400 transition-[width]" style={{ width: `${healthPercentage}%` }} />
      </div>
    </section>
  )
}
