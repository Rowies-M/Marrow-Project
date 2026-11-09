import { useEffect, useMemo, useState } from 'react'
import { setUnauthorizedHandler } from '../services/apiClient.js'
import { loginUser, registerUser } from '../services/authService.js'
import { AuthContext } from './AuthContext.js'

const SESSION_STORAGE_KEY = 'marrow.auth.session'
const ACTIVE_CHARACTER_STORAGE_KEY = 'marrow.active.character'

function readStoredSession() {
  try {
    const storedSession = sessionStorage.getItem(SESSION_STORAGE_KEY)

    if (!storedSession) {
      return null
    }

    const session = JSON.parse(storedSession)
    return session?.token && session?.user ? session : null
  } catch {
    sessionStorage.removeItem(SESSION_STORAGE_KEY)
    return null
  }
}

function persistSession(session) {
  sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
}

function clearStoredSession() {
  sessionStorage.removeItem(SESSION_STORAGE_KEY)
  sessionStorage.removeItem(ACTIVE_CHARACTER_STORAGE_KEY)
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(readStoredSession)
  const [sessionExpired, setSessionExpired] = useState(false)

  useEffect(() => {
    setUnauthorizedHandler(() => {
      clearStoredSession()
      setSession(null)
      setSessionExpired(true)
    })
    return () => setUnauthorizedHandler(null)
  }, [])

  const value = useMemo(() => ({
    token: session?.token || null,
    user: session?.user || null,
    isAuthenticated: Boolean(session?.token && session?.user),
    isRestoring: false,
    sessionExpired,
    register: registerUser,
    login: async (credentials) => {
      const response = await loginUser(credentials)
      const nextSession = { token: response.token, user: response.usuario }
      persistSession(nextSession)
      setSessionExpired(false)
      setSession(nextSession)
      return response
    },
    logout: () => {
      clearStoredSession()
      setSessionExpired(false)
      setSession(null)
    },
  }), [session, sessionExpired])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
