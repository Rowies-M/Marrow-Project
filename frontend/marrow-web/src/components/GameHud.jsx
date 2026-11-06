function getHealth({ current, maximum }) {
  const currentHealth = Number.isFinite(current) ? current : 0
  const maximumHealth = Number.isFinite(maximum) ? maximum : 0
  const percentage = maximumHealth > 0 ? Math.max(0, Math.min(100, (currentHealth / maximumHealth) * 100)) : 0

  return { currentHealth, maximumHealth, percentage }
}

function HealthOrb({ label, level, health, experience }) {
  const { currentHealth, maximumHealth, percentage } = getHealth(health)
  const { percentage: experiencePercentage } = getHealth(experience)

  return (
    <div className="flex items-center gap-4">
      <div
        className="relative h-20 w-20 shrink-0 overflow-hidden rounded-full border-4 border-stone-600 bg-black shadow-[inset_0_0_14px_#000]"
        role="progressbar"
        aria-label={`Vida de ${label}`}
        aria-valuemin="0"
        aria-valuemax={maximumHealth}
        aria-valuenow={currentHealth}
      >
        <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-red-950 to-red-600 transition-[height]" style={{ height: `${percentage}%` }} />
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate font-serif text-lg font-semibold text-amber-100">{label}</p>
        <p className="text-sm font-semibold text-red-200">Vida: {currentHealth}/{maximumHealth}</p>
        <p className="text-sm text-stone-400">Nivel {level} · XP {experience.current}/{experience.maximum}</p>
        <div
          className="mt-1 h-1.5 overflow-hidden rounded-sm border border-stone-700 bg-black"
          role="progressbar"
          aria-label={`Experiencia de ${label}`}
          aria-valuemin="0"
          aria-valuemax={experience.maximum}
          aria-valuenow={experience.current}
        >
          <div className="h-full bg-amber-500 transition-[width]" style={{ width: `${experiencePercentage}%` }} />
        </div>
      </div>
    </div>
  )
}

function HealthBar({ label, level, health }) {
  const { currentHealth, maximumHealth, percentage } = getHealth(health)

  return (
    <div className="min-w-0">
      <div className="flex items-baseline justify-between gap-3">
        <p className="truncate font-serif text-lg font-semibold text-red-200">{label}</p>
        <p className="shrink-0 text-sm text-stone-400">Nivel {level}</p>
      </div>
      <div
        className="mt-2 h-4 overflow-hidden rounded-sm border border-stone-600 bg-black"
        role="progressbar"
        aria-label={`Vida de ${label}`}
        aria-valuemin="0"
        aria-valuemax={maximumHealth}
        aria-valuenow={currentHealth}
      >
        <div className="h-full bg-gradient-to-r from-red-950 to-red-600 transition-[width]" style={{ width: `${percentage}%` }} />
      </div>
      <p className="mt-1 text-right text-sm font-semibold text-red-200">Vida: {currentHealth}/{maximumHealth}</p>
    </div>
  )
}

export default function GameHud({ characterName, characterLevel, characterHealth, characterExperience, enemyName, enemyLevel, enemyHealth }) {
  return (
    <section className="stone-panel grid gap-5 rounded p-4 sm:grid-cols-2 sm:items-center" aria-label="HUD de la partida">
      <HealthOrb label={`Guerrero ${characterName}`} level={characterLevel} health={characterHealth} experience={characterExperience} />
      <HealthBar label={enemyName} level={enemyLevel} health={enemyHealth} />
    </section>
  )
}
