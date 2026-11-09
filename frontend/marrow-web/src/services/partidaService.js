import { apiRequest } from './apiClient.js'

export function createPartida(token, personajeId, dificultad) {
  return apiRequest('/api/partidas', {
    method: 'POST',
    token,
    body: JSON.stringify({ personajeId, dificultad }),
  })
}

export const DEFAULT_PARTIDAS_QUERY = { estado: '', dificultad: '', orden: 'desc', page: 1 }

const PARTIDAS_PAGE_SIZE = 10

export function getPartidas(token, { estado, dificultad, orden, page }) {
  const params = new URLSearchParams({ orden, page, pageSize: PARTIDAS_PAGE_SIZE })
  if (estado) {
    params.set('estado', estado)
  }
  if (dificultad) {
    params.set('dificultad', dificultad)
  }

  return apiRequest(`/api/partidas?${params}`, { token })
}

export function getPartidaById(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}`, { token })
}

export function getPartidaEstado(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}/estado`, { token })
}

export function movePartida(token, partidaId, direccion) {
  return apiRequest(`/api/partidas/${partidaId}/movimiento`, {
    method: 'PATCH',
    token,
    body: JSON.stringify({ direccion }),
  })
}

export function attackPartida(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}/ataque`, {
    method: 'POST',
    token,
  })
}

export function powerStrikePartida(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}/golpe-poderoso`, {
    method: 'POST',
    token,
  })
}

export function finalizePartida(token, partidaId) {
  return apiRequest(`/api/partidas/${partidaId}/finalizar`, {
    method: 'PATCH',
    token,
  })
}
