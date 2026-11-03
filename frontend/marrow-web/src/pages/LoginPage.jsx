import { useState } from 'react'
import AuthShell from '../components/AuthShell.jsx'
import FormField from '../components/FormField.jsx'
import FormMessage from '../components/FormMessage.jsx'
import { useAuth } from '../context/useAuth.js'

export default function LoginPage({ onSwitchToRegister }) {
  const { login } = useAuth()
  const [form, setForm] = useState({ usuario: '', password: '' })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleChange(event) {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')

    if (!form.usuario.trim() || !form.password) {
      setError('Ingresá tu usuario o email y tu contraseña.')
      return
    }

    setIsSubmitting(true)
    try {
      await login({ usuario: form.usuario.trim(), password: form.password })
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthShell eyebrow="Marrow" title="Entrá a tu aventura" description="Iniciá sesión para continuar con tus personajes y partidas.">
      <form className="space-y-5" onSubmit={handleSubmit}>
        <FormField label="Usuario o email" name="usuario" value={form.usuario} onChange={handleChange} autoComplete="username" />
        <FormField label="Contraseña" name="password" type="password" value={form.password} onChange={handleChange} autoComplete="current-password" />
        <FormMessage>{error}</FormMessage>
        <button className="w-full rounded-xl bg-violet-500 px-4 py-3 font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-60" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Ingresando...' : 'Iniciar sesión'}
        </button>
      </form>
      <p className="mt-6 text-center text-sm text-slate-400">
        ¿Todavía no tenés una cuenta?{' '}
        <button className="font-semibold text-violet-300 hover:text-violet-200" type="button" onClick={onSwitchToRegister}>Registrate</button>
      </p>
    </AuthShell>
  )
}
