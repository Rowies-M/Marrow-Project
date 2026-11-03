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
    body: JSON.stringify({
      nombre,
      nivel: 1,
      experiencia: 0,
      vidaActual: 100,
      vidaMaxima: 100,
      ataque: 10,
      defensa: 5,
      clase: 0,
    }),
  })
}
