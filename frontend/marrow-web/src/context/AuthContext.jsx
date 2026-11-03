import { useMemo, useState } from 'react'
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

export function AuthProvider({ children }) {
  const [session, setSession] = useState(readStoredSession)

  const value = useMemo(() => ({
    token: session?.token || null,
    user: session?.user || null,
    isAuthenticated: Boolean(session?.token && session?.user),
    isRestoring: false,
    register: registerUser,
    login: async (credentials) => {
      const response = await loginUser(credentials)
      const nextSession = { token: response.token, user: response.usuario }
      persistSession(nextSession)
      setSession(nextSession)
      return response
    },
    logout: () => {
      sessionStorage.removeItem(SESSION_STORAGE_KEY)
      sessionStorage.removeItem(ACTIVE_CHARACTER_STORAGE_KEY)
      setSession(null)
    },
  }), [session])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
