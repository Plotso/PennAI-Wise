import api from './api.js'

export function getExchangeRates() {
  return api.get('/exchange-rates')
}

export function createExchangeRate(dto) {
  return api.post('/exchange-rates', dto)
}

export function updateExchangeRate(id, dto) {
  return api.put(`/exchange-rates/${id}`, dto)
}

export function deleteExchangeRate(id) {
  return api.delete(`/exchange-rates/${id}`)
}
