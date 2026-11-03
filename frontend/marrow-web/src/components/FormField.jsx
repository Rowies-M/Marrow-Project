export default function FormField({ label, name, type = 'text', value, onChange, autoComplete }) {
  return (
    <label className="block space-y-2 text-sm font-medium text-slate-200" htmlFor={name}>
      <span>{label}</span>
      <input
        id={name}
        name={name}
        type={type}
        value={value}
        onChange={onChange}
        autoComplete={autoComplete}
        required
        className="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-4 py-3 text-white outline-none transition placeholder:text-slate-600 focus:border-violet-400 focus:ring-2 focus:ring-violet-400/20"
      />
    </label>
  )
}
