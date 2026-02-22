import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

const TOKEN_KEY = 'pennaiwise_token'

export const useAuthStore = defineStore('auth', () => {
  // ── State ──────────────────────────────────────────────────────────
  const token = ref(localStorage.getItem(TOKEN_KEY) ?? null)
  const user = ref(null)

  // ── Getters ────────────────────────────────────────────────────────
  const isAuthenticated = computed(() => !!token.value)

  // ── Actions ────────────────────────────────────────────────────────
  function setAuth(newToken, newUser) {
    token.value = newToken
    user.value = newUser
    localStorage.setItem(TOKEN_KEY, newToken)
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem(TOKEN_KEY)
  }

  return { token, user, isAuthenticated, setAuth, logout }
})
