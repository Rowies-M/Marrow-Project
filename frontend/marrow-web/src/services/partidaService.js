import { apiRequest } from './apiClient.js'

export function createPartida(token, personajeId) {
  return apiRequest('/api/partidas', {
    method: 'POST',
    token,
    body: JSON.stringify({ personajeId }),
  })
}

export function getPartidas(token) {
  return apiRequest('/api/partidas', { token })
}

export function getPartidaById(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}`, { token })
}

export function getPartidaEstado(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}/estado`, { token })
}

export function finalizePartida(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}/finalizar`, {
    method: 'PATCH',
    token,
  })
}
