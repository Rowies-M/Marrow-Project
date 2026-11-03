import { useState } from 'react'
import AuthShell from '../components/AuthShell.jsx'
import FormField from '../components/FormField.jsx'
import FormMessage from '../components/FormMessage.jsx'
import { useAuth } from '../context/useAuth.js'

const initialForm = { nombreUsuario: '', email: '', password: '' }

export default function RegisterPage({ onSwitchToLogin }) {
  const { register } = useAuth()
  const [form, setForm] = useState(initialForm)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleChange(event) {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }))
  }

  function validateForm() {
    if (form.nombreUsuario.trim().length < 3) return 'El nombre de usuario debe tener al menos 3 caracteres.'
    if (!/^\S+@\S+\.\S+$/.test(form.email)) return 'Ingresá un email válido.'
    if (form.password.length < 8) return 'La contraseña debe tener al menos 8 caracteres.'
    return ''
  }

  async function handleSubmit(event) {
    event.preventDefault()
    const validationError = validateForm()
    setError(validationError)
    if (validationError) return

    setIsSubmitting(true)
    try {
      await register({ nombreUsuario: form.nombreUsuario.trim(), email: form.email.trim(), password: form.password })
      onSwitchToLogin('Cuenta creada correctamente. Ahora iniciá sesión.')
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthShell eyebrow="Primer paso" title="Creá tu cuenta" description="Registrate para guardar tus personajes y aventuras en Marrow.">
      <form className="space-y-5" onSubmit={handleSubmit}>
        <FormField label="Nombre de usuario" name="nombreUsuario" value={form.nombreUsuario} onChange={handleChange} autoComplete="username" />
        <FormField label="Email" name="email" type="email" value={form.email} onChange={handleChange} autoComplete="email" />
        <FormField label="Contraseña" name="password" type="password" value={form.password} onChange={handleChange} autoComplete="new-password" />
        <FormMessage>{error}</FormMessage>
        <button className="w-full rounded-xl bg-violet-500 px-4 py-3 font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-60" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creando cuenta...' : 'Crear cuenta'}
        </button>
      </form>
      <p className="mt-6 text-center text-sm text-slate-400">
        ¿Ya tenés una cuenta?{' '}
        <button className="font-semibold text-violet-300 hover:text-violet-200" type="button" onClick={() => onSwitchToLogin()}>Iniciá sesión</button>
      </p>
    </AuthShell>
  )
}
