import { useState } from 'react'
import FormMessage from './FormMessage.jsx'
import { createCharacter } from '../services/personajeService.js'

export default function CreateCharacterForm({ token, onCreated, onCancel }) {
  const [nombre, setNombre] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    const trimmedName = nombre.trim()

    if (!trimmedName) {
      setError('Ingresá un nombre para tu personaje.')
      return
    }

    if (trimmedName.length > 100) {
      setError('El nombre no puede superar los 100 caracteres.')
      return
    }

    setError('')
    setIsSubmitting(true)

    try {
      const character = await createCharacter(token, trimmedName)
      setNombre('')
      onCreated(character)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className="rounded-2xl border border-violet-400/20 bg-slate-900 p-5" onSubmit={handleSubmit}>
      <div className="mb-5">
        <h2 className="text-xl font-bold text-white">Nuevo personaje</h2>
        <p className="mt-1 text-sm text-slate-400">Comenzará como Guerrero con sus valores iniciales.</p>
      </div>
      <label className="block space-y-2 text-sm font-medium text-slate-200" htmlFor="character-name">
        <span>Nombre</span>
        <input
          id="character-name"
          name="nombre"
          type="text"
          value={nombre}
          onChange={(event) => setNombre(event.target.value)}
          maxLength={100}
          autoComplete="off"
          required
          className="w-full rounded-xl border border-slate-700 bg-slate-950/80 px-4 py-3 text-white outline-none transition focus:border-violet-400 focus:ring-2 focus:ring-violet-400/20"
        />
      </label>
      <FormMessage>{error}</FormMessage>
      <div className="mt-5 flex gap-3">
        <button
          className="flex-1 rounded-xl bg-violet-500 px-4 py-3 font-semibold text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-60"
          type="submit"
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Creando...' : 'Crear personaje'}
        </button>
        <button className="rounded-xl border border-slate-700 px-4 py-3 font-semibold text-slate-300 transition hover:border-slate-500 hover:text-white" type="button" onClick={onCancel} disabled={isSubmitting}>
          Cancelar
        </button>
      </div>
    </form>
  )
}
