export default function FormMessage({ children, type = 'error' }) {
  if (!children) return null

  const styles = type === 'success'
    ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200'
    : 'border-rose-400/30 bg-rose-400/10 text-rose-200'

  return <p className={`rounded-xl border px-4 py-3 text-sm ${styles}`} role="alert">{children}</p>
}
