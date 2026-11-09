import { useCallback, useEffect, useState } from 'react'
import { useAuth } from './context/useAuth.js'
import { DEFAULT_PARTIDAS_QUERY } from './services/partidaService.js'
import { getCharacterById } from './services/personajeService.js'
import CharacterSelectionPage from './pages/CharacterSelectionPage.jsx'
import GamePage from './pages/GamePage.jsx'
import PartidasPanel from './components/PartidasPanel.jsx'
import LoginPage from './pages/LoginPage.jsx'
import RegisterPage from './pages/RegisterPage.jsx'

const ACTIVE_CHARACTER_STORAGE_KEY = 'marrow.active.character'

function ActiveCharacterHome({ character: selectedCharacter, partidasQuery, onPartidasQueryChange, onBackToSelection, onPartidaSelected, onLogout }) {
  const { token, user } = useAuth()
  const [character, setCharacter] = useState(selectedCharacter)
  const characterId = selectedCharacter.id

  // Al volver de una partida, el nivel y la experiencia pueden haber cambiado en el backend.
  useEffect(() => {
    let cancelled = false

    getCharacterById(token, characterId)
      .then((response) => {
        if (!cancelled) {
          setCharacter(response)
        }
      })
      .catch(() => {
        // Si falla la lectura, se conservan los datos del personaje seleccionado.
      })

    return () => { cancelled = true }
  }, [characterId, token])

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-10">
      <section className="w-full max-w-xl rounded-3xl border border-violet-400/20 bg-slate-900 p-8 text-center shadow-2xl shadow-violet-950/30">
        <p className="mb-3 text-xs font-semibold uppercase tracking-[0.35em] text-violet-300">Marrow</p>
        <h1 className="text-4xl font-bold tracking-tight text-white">Bienvenido, {user.nombreUsuario}</h1>
        <p className="mt-4 text-slate-400">Tu personaje activo es {character.nombre}.</p>
        <div className="mt-8 grid grid-cols-2 gap-3 text-left text-sm sm:grid-cols-4">
          <div className="rounded-xl bg-slate-950/60 p-3"><p className="text-slate-500">Clase</p><p className="mt-1 font-semibold text-white">Guerrero</p></div>
          <div className="rounded-xl bg-slate-950/60 p-3"><p className="text-slate-500">Nivel</p><p className="mt-1 font-semibold text-white">{character.nivel}/{character.nivelMaximo} · {character.experiencia} XP</p></div>
          <div className="rounded-xl bg-slate-950/60 p-3"><p className="text-slate-500">Vida</p><p className="mt-1 font-semibold text-white">{character.vidaActual}/{character.vidaMaxima}</p></div>
          <div className="rounded-xl bg-slate-950/60 p-3"><p className="text-slate-500">Ataque</p><p className="mt-1 font-semibold text-white">{character.ataque}</p></div>
        </div>
        <PartidasPanel token={token} personajeId={character.id} isHardUnlocked={character.dificultadDesbloqueada === 'Dificil'} query={partidasQuery} onQueryChange={onPartidasQueryChange} onPartidaSelected={onPartidaSelected} />
        <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
          <button className="rounded-xl bg-violet-500 px-5 py-3 font-semibold text-white transition hover:bg-violet-400" type="button" onClick={onBackToSelection}>Volver a selección</button>
          <button className="rounded-xl border border-slate-700 px-5 py-3 font-semibold text-slate-200 transition hover:border-violet-400 hover:text-white" type="button" onClick={onLogout}>Cerrar sesión</button>
        </div>
      </section>
    </main>
  )
}

function App() {
  const { isAuthenticated, isRestoring, sessionExpired, token, user, logout } = useAuth()
  const [authMode, setAuthMode] = useState('login')
  const [registrationNotice, setRegistrationNotice] = useState('')
  const [activeCharacter, setActiveCharacter] = useState(null)
  const [selectedPartida, setSelectedPartida] = useState(null)
  // Vive aquí para conservar filtros y página al entrar y volver de una partida.
  const [partidasQuery, setPartidasQuery] = useState(DEFAULT_PARTIDAS_QUERY)
  const userId = user?.id
  const loginNotice = registrationNotice || (sessionExpired ? 'Tu sesión expiró. Volvé a iniciar sesión.' : '')

  // Cubre también la sesión vencida: al volver a entrar no se reabre la partida anterior.
  const [wasAuthenticated, setWasAuthenticated] = useState(isAuthenticated)
  if (wasAuthenticated !== isAuthenticated) {
    setWasAuthenticated(isAuthenticated)
    if (!isAuthenticated) {
      setSelectedPartida(null)
      setPartidasQuery(DEFAULT_PARTIDAS_QUERY)
    }
  }

  const handleCharacterSelected = useCallback((character) => {
    if (!userId) {
      return
    }

    setActiveCharacter({ userId, character })
    setSelectedPartida(null)
  }, [userId])

  const handleBackToSelection = useCallback(() => {
    sessionStorage.removeItem(ACTIVE_CHARACTER_STORAGE_KEY)
    setActiveCharacter(null)
    setSelectedPartida(null)
    setPartidasQuery(DEFAULT_PARTIDAS_QUERY)
  }, [])

  const handleLogout = useCallback(() => {
    setSelectedPartida(null)
    setPartidasQuery(DEFAULT_PARTIDAS_QUERY)
    logout()
  }, [logout])

  if (isRestoring) {
    return <main className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-300">Restaurando sesión...</main>
  }

  if (isAuthenticated) {
    const selectedCharacter = activeCharacter?.userId === user.id ? activeCharacter.character : null

    if (selectedCharacter) {
      if (selectedPartida) {
        return <GamePage partida={selectedPartida} personaje={selectedCharacter} onBackToPartidas={() => setSelectedPartida(null)} />
      }

      return <ActiveCharacterHome character={selectedCharacter} partidasQuery={partidasQuery} onPartidasQueryChange={setPartidasQuery} onBackToSelection={handleBackToSelection} onPartidaSelected={setSelectedPartida} onLogout={handleLogout} />
    }

    return <CharacterSelectionPage token={token} user={user} activeCharacter={selectedCharacter} onCharacterSelected={handleCharacterSelected} onLogout={handleLogout} />
  }

  if (authMode === 'register') {
    return (
      <RegisterPage
        onSwitchToLogin={(notice = '') => {
          setRegistrationNotice(notice)
          setAuthMode('login')
        }}
      />
    )
  }

  return (
    <div>
      <LoginPage onSwitchToRegister={() => {
        setRegistrationNotice('')
        setAuthMode('register')
      }} />
      {loginNotice && (
        <div className="fixed bottom-5 left-1/2 w-[calc(100%-2rem)] max-w-md -translate-x-1/2">
          <p className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 px-4 py-3 text-center text-sm text-emerald-200" role="status">
            {loginNotice}
          </p>
        </div>
      )}
    </div>
  )
}

export default App
