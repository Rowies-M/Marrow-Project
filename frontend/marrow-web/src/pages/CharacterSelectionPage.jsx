import { useEffect, useState } from 'react'
import CharacterCard from '../components/CharacterCard.jsx'
import CreateCharacterForm from '../components/CreateCharacterForm.jsx'
import FormMessage from '../components/FormMessage.jsx'
import { getCharacters } from '../services/personajeService.js'

const ACTIVE_CHARACTER_STORAGE_KEY = 'marrow.active.character'

function readActiveCharacterId(userId, characters) {
  try {
    const stored = JSON.parse(sessionStorage.getItem(ACTIVE_CHARACTER_STORAGE_KEY))
    if (stored?.userId !== userId) return null
    return characters.some((character) => character.id === stored.characterId) ? stored.characterId : null
  } catch {
    sessionStorage.removeItem(ACTIVE_CHARACTER_STORAGE_KEY)
    return null
  }
}

export default function CharacterSelectionPage({ token, user, activeCharacter, onCharacterSelected, onLogout }) {
  const [characters, setCharacters] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [loadAttempt, setLoadAttempt] = useState(0)

  useEffect(() => {
    let cancelled = false

    async function loadCharacters() {
      setIsLoading(true)
      setError('')

      try {
        const response = await getCharacters(token)
        if (cancelled) return

        const loadedCharacters = Array.isArray(response) ? response : []
        setCharacters(loadedCharacters)
        const storedCharacterId = readActiveCharacterId(user.id, loadedCharacters)
        const storedCharacter = loadedCharacters.find((character) => character.id === storedCharacterId)

        if (storedCharacter) {
          onCharacterSelected(storedCharacter)
        }
      } catch (requestError) {
        if (!cancelled) setError(requestError.message)
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    loadCharacters()
    return () => { cancelled = true }
  }, [loadAttempt, onCharacterSelected, token, user.id])

  function handleCharacterSelected(character) {
    sessionStorage.setItem(ACTIVE_CHARACTER_STORAGE_KEY, JSON.stringify({
      userId: user.id,
      characterId: character.id,
    }))
    onCharacterSelected(character)
  }

  function handleCharacterCreated(character) {
    setCharacters((current) => [...current, character])
    setShowCreateForm(false)
    handleCharacterSelected(character)
  }

  if (isLoading) {
    return <main className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-300">Cargando tus personajes...</main>
  }

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-10">
      <section className="mx-auto max-w-5xl">
        <div className="flex flex-col justify-between gap-5 sm:flex-row sm:items-end">
          <div>
            <p className="mb-3 text-xs font-semibold uppercase tracking-[0.35em] text-violet-300">Marrow</p>
            <h1 className="text-4xl font-bold tracking-tight text-white">Elegí tu personaje</h1>
            <p className="mt-3 text-slate-400">Hola, {user.nombreUsuario}. Seleccioná quién entrará en la aventura.</p>
          </div>
          <div className="flex flex-col gap-3 sm:flex-row">
            <button className="rounded-xl bg-violet-500 px-4 py-3 font-semibold text-white transition hover:bg-violet-400" type="button" onClick={() => setShowCreateForm((current) => !current)}>
              {showCreateForm ? 'Cerrar formulario' : 'Crear personaje'}
            </button>
            <button className="rounded-xl border border-slate-700 px-4 py-3 font-semibold text-slate-200 transition hover:border-violet-400 hover:text-white" type="button" onClick={onLogout}>
              Cerrar sesión
            </button>
          </div>
        </div>

        <div className="mt-8 space-y-5">
          {showCreateForm && <CreateCharacterForm token={token} onCreated={handleCharacterCreated} onCancel={() => setShowCreateForm(false)} />}
          {error ? (
            <div className="space-y-3">
              <FormMessage>{error}</FormMessage>
              <button className="rounded-xl border border-slate-700 px-4 py-2 font-semibold text-slate-200 transition hover:border-violet-400 hover:text-white" type="button" onClick={() => setLoadAttempt((current) => current + 1)}>
                Reintentar
              </button>
            </div>
          ) : characters.length === 0 && !showCreateForm ? (
            <div className="rounded-2xl border border-dashed border-slate-700 bg-slate-900/70 p-10 text-center">
              <h2 className="text-2xl font-bold text-white">Todavía no tenés personajes</h2>
              <p className="mt-3 text-slate-400">Creá un Guerrero para comenzar tu historia.</p>
              <button className="mt-6 rounded-xl bg-violet-500 px-5 py-3 font-semibold text-white transition hover:bg-violet-400" type="button" onClick={() => setShowCreateForm(true)}>Crear mi primer personaje</button>
            </div>
          ) : (
            <div className="grid gap-5 md:grid-cols-2">
              {characters.map((character) => (
                <CharacterCard key={character.id} character={character} isSelected={activeCharacter?.id === character.id} onSelect={handleCharacterSelected} />
              ))}
            </div>
          )}
        </div>
      </section>
    </main>
  )
}
