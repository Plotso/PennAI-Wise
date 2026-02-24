import api from './api.js'

export function getSettings() {
  return api.get('/settings')
}

export function updateSettings(dto) {
  return api.put('/settings', dto)
}
