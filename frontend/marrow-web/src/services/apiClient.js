// Único lugar con la URL de la API. El valor local solo aplica en desarrollo: un build de producción debe definirla.
const apiUrl = (import.meta.env.VITE_API_URL || (import.meta.env.DEV ? 'http://localhost:5137' : '')).replace(/\/$/, '')

const REQUEST_TIMEOUT_MS = 15000

// Títulos que ASP.NET Core genera por defecto: no sirven como mensaje para el jugador.
const DEFAULT_TITLES = new Set(['Bad Request', 'Unauthorized', 'Forbidden', 'Not Found', 'Conflict', 'One or more validation errors occurred.'])

const STATUS_MESSAGES = {
  400: 'Los datos enviados no son válidos.',
  401: 'No tenés autorización para realizar esta acción.',
  403: 'No tenés permiso para realizar esta acción.',
  404: 'No se encontró lo que estabas buscando.',
  409: 'La acción no se pudo completar por un conflicto. Intentá de nuevo.',
}

let handleUnauthorized = null

export function setUnauthorizedHandler(handler) {
  handleUnauthorized = handler
}

function getErrorMessage(payload, status) {
  if (payload?.detail) {
    return payload.detail
  }

  if (payload?.errors) {
    return Object.values(payload.errors).flat().join(' ')
  }

  if (payload?.title && !DEFAULT_TITLES.has(payload.title)) {
    return payload.title
  }

  return STATUS_MESSAGES[status] || `La solicitud no pudo completarse (${status}). Intentá de nuevo.`
}

function getConnectionErrorMessage(error) {
  return error?.name === 'TimeoutError'
    ? 'El servidor tardó demasiado en responder. Intentá de nuevo.'
    : 'No se pudo conectar con el servidor. Revisá tu conexión e intentá de nuevo.'
}

export async function apiRequest(path, { token, ...options } = {}) {
  if (!apiUrl) {
    throw new Error('Falta configurar VITE_API_URL al construir el frontend.')
  }

  const headers = new Headers(options.headers)

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  let response
  try {
    response = await fetch(`${apiUrl}${path}`, {
      ...options,
      headers,
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    })
  } catch (error) {
    throw new Error(getConnectionErrorMessage(error))
  }

  const contentType = response.headers.get('content-type') || ''
  const payload = contentType.includes('json')
    ? await response.json().catch(() => null)
    : null

  if (!response.ok) {
    // Un 401 con token significa sesión vencida o inválida; sin token, son credenciales incorrectas en el login.
    if (response.status === 401 && token) {
      handleUnauthorized?.()
    }

    const error = new Error(getErrorMessage(payload, response.status))
    error.status = response.status
    error.payload = payload
    throw error
  }

  return payload
}
