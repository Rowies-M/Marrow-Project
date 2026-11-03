import { apiRequest } from './apiClient.js'

export function registerUser({ nombreUsuario, email, password }) {
  return apiRequest('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ nombreUsuario, email, password }),
  })
}

export function loginUser({ usuario, password }) {
  return apiRequest('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ usuario, password }),
  })
}
