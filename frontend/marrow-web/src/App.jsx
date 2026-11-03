import { useState } from 'react'
import { useAuth } from './context/useAuth.js'
import LoginPage from './pages/LoginPage.jsx'
import RegisterPage from './pages/RegisterPage.jsx'

function AuthenticatedHome() {
  const { user, logout } = useAuth()

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-10">
      <section className="w-full max-w-xl rounded-3xl border border-violet-400/20 bg-slate-900 p-8 text-center shadow-2xl shadow-violet-950/30">
        <p className="mb-3 text-xs font-semibold uppercase tracking-[0.35em] text-violet-300">Marrow</p>
        <h1 className="text-4xl font-bold tracking-tight text-white">Bienvenido, {user.nombreUsuario}</h1>
        <p className="mt-4 text-slate-400">Tu sesión está activa. El mundo de Marrow te espera.</p>
        <button className="mt-8 rounded-xl border border-slate-700 px-5 py-3 font-semibold text-slate-200 transition hover:border-violet-400 hover:text-white" type="button" onClick={logout}>
          Cerrar sesión
        </button>
      </section>
    </main>
  )
}

function App() {
  const { isAuthenticated, isRestoring } = useAuth()
  const [authMode, setAuthMode] = useState('login')
  const [registrationNotice, setRegistrationNotice] = useState('')

  if (isRestoring) {
    return <main className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-300">Restaurando sesión...</main>
  }

  if (isAuthenticated) {
    return <AuthenticatedHome />
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
      {registrationNotice && (
        <div className="fixed bottom-5 left-1/2 w-[calc(100%-2rem)] max-w-md -translate-x-1/2">
          <p className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 px-4 py-3 text-center text-sm text-emerald-200" role="status">
            {registrationNotice}
          </p>
        </div>
      )}
    </div>
  )
}

export default App
