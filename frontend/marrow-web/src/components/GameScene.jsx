export default function GameScene({ characterName, enemyName = 'Goblin' }) {
  return (
    <section
      className="relative min-h-[28rem] overflow-hidden rounded-3xl border border-amber-200/10 bg-emerald-950 p-6 shadow-2xl shadow-emerald-950/30"
      aria-label="Escenario de juego"
    >
      <div className="absolute inset-0 opacity-40 [background-image:linear-gradient(rgba(255,255,255,0.06)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.06)_1px,transparent_1px)] [background-size:3rem_3rem]" />
      <div className="absolute left-8 top-8 h-12 w-12 rounded-lg border border-emerald-300/20 bg-emerald-900/80" />
      <div className="absolute bottom-10 right-10 h-16 w-16 rounded-full border border-emerald-300/20 bg-emerald-900/80" />

      <div className="relative flex min-h-[24rem] items-center justify-between gap-8 px-4 sm:px-14">
        <div className="flex flex-col items-center gap-3" aria-label={`Guerrero ${characterName}`}>
          <div className="flex h-24 w-24 items-center justify-center rounded-2xl border-2 border-violet-300 bg-violet-500 text-4xl shadow-lg shadow-violet-950/40" aria-hidden="true">⚔️</div>
          <p className="rounded-full bg-slate-950/80 px-4 py-2 text-sm font-semibold text-white">{characterName}</p>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-violet-200">Guerrero</p>
        </div>

        <div className="rounded-full border border-amber-200/20 bg-slate-950/70 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-amber-200">Escenario inicial</div>

        <div className="flex flex-col items-center gap-3" aria-label={enemyName}>
          <div className="flex h-24 w-24 items-center justify-center rounded-2xl border-2 border-rose-300 bg-rose-700 text-4xl shadow-lg shadow-rose-950/40" aria-hidden="true">👹</div>
          <p className="rounded-full bg-slate-950/80 px-4 py-2 text-sm font-semibold text-white">{enemyName}</p>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-rose-200">Enemigo</p>
        </div>
      </div>
    </section>
  )
}
