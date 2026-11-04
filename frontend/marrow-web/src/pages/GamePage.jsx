import GameHud from '../components/GameHud.jsx'
import GameScene from '../components/GameScene.jsx'

export default function GamePage({ partida, personaje, onBackToPartidas }) {
  return (
    <main className="min-h-screen bg-slate-950 px-4 py-8 sm:px-8">
      <section className="mx-auto max-w-6xl">
        <header className="flex flex-col gap-4 border-b border-slate-800 pb-6 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.35em] text-violet-300">Marrow · Partida #{partida.id}</p>
            <h1 className="mt-2 text-4xl font-bold tracking-tight text-white">La entrada a la mazmorra</h1>
            <p className="mt-2 text-slate-400">{personaje.nombre} se prepara para avanzar.</p>
          </div>
          <button className="rounded-xl border border-slate-700 px-5 py-3 font-semibold text-slate-200 transition hover:border-violet-400 hover:text-white" type="button" onClick={onBackToPartidas}>Volver a partidas</button>
        </header>

        <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
          <GameScene characterName={personaje.nombre} />
          <aside className="space-y-6">
            <GameHud character={personaje} />
            <section className="rounded-2xl border border-rose-400/20 bg-slate-900 p-5" aria-label="Información del enemigo">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-rose-300">Enemigo visible</p>
              <h2 className="mt-2 text-2xl font-bold text-white">Goblin</h2>
              <p className="mt-2 text-sm text-slate-400">Enemigo inicial del escenario. El combate se habilitará en un bloque posterior.</p>
            </section>
          </aside>
        </div>
      </section>
    </main>
  )
}
