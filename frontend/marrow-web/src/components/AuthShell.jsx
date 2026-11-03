export default function AuthShell({ eyebrow, title, description, children }) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-10">
      <section className="w-full max-w-md rounded-3xl border border-violet-400/20 bg-slate-900/90 p-8 shadow-2xl shadow-violet-950/30">
        <div className="mb-8 text-center">
          <p className="mb-3 text-xs font-semibold uppercase tracking-[0.35em] text-violet-300">{eyebrow}</p>
          <h1 className="text-3xl font-bold tracking-tight text-white">{title}</h1>
          <p className="mt-3 text-sm leading-6 text-slate-400">{description}</p>
        </div>
        {children}
      </section>
    </main>
  )
}
