import { apiRequest } from './apiClient.js'

export function getCharacters(token) {
  return apiRequest('/api/personajes', { token })
}

export function getCharacterById(token, characterId) {
  return apiRequest(`/api/personajes/${characterId}`, { token })
}

export function createCharacter(token, nombre) {
  return apiRequest('/api/personajes', {
    method: 'POST',
    token,
    body: JSON.stringify({ nombre }),
  })
}
