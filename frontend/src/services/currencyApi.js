import api from './api.js'

export function getCurrencies() {
  return api.get('/currencies')
}
