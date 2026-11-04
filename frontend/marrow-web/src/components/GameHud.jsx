function HealthBar({ label, current, maximum, color }) {
  const currentHealth = Number.isFinite(current) ? current : 0
  const maximumHealth = Number.isFinite(maximum) ? maximum : 0
  const healthPercentage = maximumHealth > 0 ? Math.max(0, Math.min(100, (currentHealth / maximumHealth) * 100)) : 0

  return (
    <div>
      <div className="flex items-center justify-between gap-3">
        <p className="font-semibold text-white">{label}</p>
        <p className="text-sm font-semibold text-slate-300">Vida: {currentHealth}/{maximumHealth}</p>
      </div>
      <div className="mt-2 h-3 overflow-hidden rounded-full bg-slate-950" role="progressbar" aria-label={`Vida de ${label}`} aria-valuemin="0" aria-valuemax={maximumHealth} aria-valuenow={currentHealth}>
        <div className={`h-full rounded-full ${color} transition-[width]`} style={{ width: `${healthPercentage}%` }} />
      </div>
    </div>
  )
}

export default function GameHud({ characterName, characterHealth, enemyName, enemyHealth }) {
  return (
    <section className="rounded-2xl border border-violet-400/20 bg-slate-900 p-5" aria-label="HUD de la partida">
      <p className="text-xs font-semibold uppercase tracking-[0.2em] text-violet-300">Estado del combate</p>
      <div className="mt-4 space-y-5">
        <HealthBar label={`Guerrero ${characterName}`} current={characterHealth.current} maximum={characterHealth.maximum} color="bg-emerald-400" />
        <HealthBar label={enemyName} current={enemyHealth.current} maximum={enemyHealth.maximum} color="bg-rose-400" />
      </div>
    </section>
  )
}
