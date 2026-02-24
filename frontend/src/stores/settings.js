import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getCurrencies } from '../services/currencyApi.js'
import { getSettings, updateSettings } from '../services/settingsApi.js'

export const useSettingsStore = defineStore('settings', () => {
  // ── State ──────────────────────────────────────────────────────────
  const currencies = ref([])
  const defaultCurrencyCode = ref(null)
  const loaded = ref(false)

  // ── Getters ────────────────────────────────────────────────────────
  const currencyMap = computed(() => {
    const map = {}
    for (const c of currencies.value) map[c.code] = c
    return map
  })

  /** Resolved display currency: user default → EUR fallback */
  const displayCurrency = computed(() => defaultCurrencyCode.value ?? 'EUR')

  const displayCurrencySymbol = computed(() => currencyMap.value[displayCurrency.value]?.symbol ?? '€')

  // ── Actions ────────────────────────────────────────────────────────
  async function load() {
    if (loaded.value) return
    const [currResp, settResp] = await Promise.all([getCurrencies(), getSettings()])
    currencies.value = currResp.data
    defaultCurrencyCode.value = settResp.data.defaultCurrencyCode
    loaded.value = true
  }

  async function setDefaultCurrency(code) {
    await updateSettings({ defaultCurrencyCode: code || null })
    defaultCurrencyCode.value = code || null
  }

  function $reset() {
    currencies.value = []
    defaultCurrencyCode.value = null
    loaded.value = false
  }

  return {
    currencies,
    defaultCurrencyCode,
    loaded,
    currencyMap,
    displayCurrency,
    displayCurrencySymbol,
    load,
    setDefaultCurrency,
    $reset,
  }
})
