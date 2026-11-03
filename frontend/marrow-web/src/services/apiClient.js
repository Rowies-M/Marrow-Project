const apiUrl = (import.meta.env.VITE_API_URL || 'http://localhost:5137').replace(/\/$/, '')

function getErrorMessage(payload, status) {
  if (payload?.detail) {
    return payload.detail
  }

  if (payload?.title) {
    return payload.title
  }

  if (payload?.errors) {
    return Object.values(payload.errors).flat().join(' ')
  }

  return `La solicitud no pudo completarse (${status}).`
}

export async function apiRequest(path, { token, ...options } = {}) {
  const headers = new Headers(options.headers)

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiUrl}${path}`, {
    ...options,
    headers,
  })

  const contentType = response.headers.get('content-type') || ''
  const payload = contentType.includes('application/json')
    ? await response.json()
    : null

  if (!response.ok) {
    const error = new Error(getErrorMessage(payload, response.status))
    error.status = response.status
    error.payload = payload
    throw error
  }

  return payload
}
