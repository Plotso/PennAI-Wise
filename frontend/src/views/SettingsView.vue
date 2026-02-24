<script setup>
import { ref, onMounted, computed } from 'vue'
import { useSettingsStore } from '../stores/settings.js'
import {
  getExchangeRates,
  createExchangeRate,
  updateExchangeRate,
  deleteExchangeRate,
} from '../services/exchangeRateApi.js'

const settings = useSettingsStore()

// ── Preferences ─────────────────────────────────────────────────────
const selectedCurrency = ref('')
const savingPref       = ref(false)
const prefSaved        = ref(false)

async function savePref() {
  savingPref.value = true
  prefSaved.value  = false
  try {
    await settings.setDefaultCurrency(selectedCurrency.value)
    prefSaved.value = true
    setTimeout(() => (prefSaved.value = false), 2000)
  } finally {
    savingPref.value = false
  }
}

// ── Exchange rates ──────────────────────────────────────────────────
const rates      = ref([])
const ratesLoading = ref(false)
const rateError  = ref(null)

const rateForm    = ref(defaultRateForm())
const showForm    = ref(false)
const editingId   = ref(null)     // null = add mode, number = rate id being edited
const saving      = ref(false)
const confirmDeleteId = ref(null)

function defaultRateForm() {
  return { fromCurrencyCode: '', toCurrencyCode: '', rate: '', effectiveDate: todayStr() }
}

function todayStr() { return new Date().toISOString().slice(0, 10) }

async function loadRates() {
  ratesLoading.value = true
  try {
    const { data } = await getExchangeRates()
    rates.value = data
  } finally {
    ratesLoading.value = false
  }
}

function startAdd() {
  editingId.value = null
  showForm.value  = true
  rateForm.value  = defaultRateForm()
  rateError.value = null
}

function startEdit(r) {
  editingId.value = r.id
  showForm.value  = true
  rateForm.value  = {
    fromCurrencyCode: r.fromCurrencyCode,
    toCurrencyCode:   r.toCurrencyCode,
    rate:             r.rate,
    effectiveDate:    r.effectiveDate.slice(0, 10),
  }
  rateError.value = null
}

function cancelEdit() {
  editingId.value = null
  showForm.value  = false
  rateForm.value  = defaultRateForm()
  rateError.value = null
}

async function saveRate() {
  rateError.value = null
  saving.value    = true
  try {
    if (editingId.value) {
      await updateExchangeRate(editingId.value, {
        rate:          Number(rateForm.value.rate),
        effectiveDate: rateForm.value.effectiveDate + 'T00:00:00',
      })
    } else {
      await createExchangeRate({
        fromCurrencyCode: rateForm.value.fromCurrencyCode,
        toCurrencyCode:   rateForm.value.toCurrencyCode,
        rate:             Number(rateForm.value.rate),
        effectiveDate:    rateForm.value.effectiveDate + 'T00:00:00',
      })
    }
    cancelEdit()
    await loadRates()
  } catch (e) {
    rateError.value = e.response?.data?.message ?? 'Failed to save exchange rate.'
  } finally {
    saving.value = false
  }
}

async function confirmDelete(id) {
  try {
    await deleteExchangeRate(id)
    confirmDeleteId.value = null
    await loadRates()
  } catch {
    rateError.value = 'Failed to delete exchange rate.'
  }
}

const formValid = computed(() => {
  const f = rateForm.value
  if (editingId.value) return f.rate > 0 && f.effectiveDate
  return f.fromCurrencyCode && f.toCurrencyCode && f.fromCurrencyCode !== f.toCurrencyCode && f.rate > 0 && f.effectiveDate
})

function fmtDate(iso) {
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(iso))
}

function symbolFor(code) {
  return settings.currencyMap[code]?.symbol ?? code
}

// ── Init ────────────────────────────────────────────────────────────
onMounted(async () => {
  await settings.load()
  selectedCurrency.value = settings.defaultCurrencyCode ?? ''
  await loadRates()
})
</script>

<template>
  <div class="settings-page">
    <div class="page-inner">
      <div class="page-header">
        <h1 class="page-title">Settings</h1>
        <p class="page-sub">Manage your currency preferences and exchange rates.</p>
      </div>

      <!-- ── DEFAULT CURRENCY ─────────────────────────────────── -->
      <section class="section-card">
        <h2 class="section-title">Default Currency</h2>
        <p class="section-desc">Choose the currency used by default on the dashboard and for new expenses.</p>

        <div class="pref-row">
          <select v-model="selectedCurrency" class="pref-select">
            <option value="">None (EUR fallback)</option>
            <option v-for="c in settings.currencies" :key="c.code" :value="c.code">
              {{ c.symbol }} {{ c.code }} — {{ c.name }}
            </option>
          </select>
          <button class="btn-primary btn-sm" :disabled="savingPref" @click="savePref">
            {{ savingPref ? 'Saving…' : 'Save' }}
          </button>
          <span v-if="prefSaved" class="saved-badge">✓ Saved</span>
        </div>
      </section>

      <!-- ── EXCHANGE RATES ───────────────────────────────────── -->
      <section class="section-card">
        <div class="section-header-row">
          <div>
            <h2 class="section-title">Exchange Rates</h2>
            <p class="section-desc">Add rates used to convert expenses on the dashboard. The rate closest to (but not after) each expense date is used.</p>
          </div>
          <button v-if="!showForm" class="btn-primary btn-sm" @click="startAdd">+ Add Rate</button>
        </div>

        <!-- Rate form -->
        <div v-if="showForm" class="rate-form">
          <div v-if="rateError" class="form-error"><span>⚠</span> {{ rateError }}</div>

          <div class="rate-form-grid">
            <div class="field" v-if="!editingId">
              <label>From</label>
              <select v-model="rateForm.fromCurrencyCode">
                <option value="" disabled>Select…</option>
                <option v-for="c in settings.currencies" :key="c.code" :value="c.code">{{ c.code }}</option>
              </select>
            </div>
            <div class="field" v-if="!editingId">
              <label>To</label>
              <select v-model="rateForm.toCurrencyCode">
                <option value="" disabled>Select…</option>
                <option v-for="c in settings.currencies" :key="c.code" :value="c.code">{{ c.code }}</option>
              </select>
            </div>
            <div class="field">
              <label>Rate</label>
              <input v-model="rateForm.rate" type="number" min="0.000001" step="0.0001" placeholder="1.1234" />
            </div>
            <div class="field">
              <label>Effective Date</label>
              <input v-model="rateForm.effectiveDate" type="date" />
            </div>
          </div>

          <div class="rate-form-actions">
            <button class="btn-cancel" @click="cancelEdit">Cancel</button>
            <button class="btn-primary" :disabled="saving || !formValid" @click="saveRate">
              {{ saving ? 'Saving…' : (editingId ? 'Update' : 'Add Rate') }}
            </button>
          </div>
        </div>

        <!-- Rates table -->
        <div v-if="ratesLoading" class="rates-loading">Loading…</div>
        <div v-else-if="rates.length === 0 && !showForm" class="rates-empty">
          <p>No exchange rates yet. Click <strong>+ Add Rate</strong> to create one.</p>
        </div>
        <div v-else-if="rates.length" class="table-wrap">
          <table class="rate-table">
            <thead>
              <tr><th>From</th><th>To</th><th class="th-right">Rate</th><th>Effective Date</th><th class="th-center">Actions</th></tr>
            </thead>
            <tbody>
              <tr v-for="r in rates" :key="r.id" class="rate-row">
                <td>{{ symbolFor(r.fromCurrencyCode) }} {{ r.fromCurrencyCode }}</td>
                <td>{{ symbolFor(r.toCurrencyCode) }} {{ r.toCurrencyCode }}</td>
                <td class="td-right">{{ r.rate }}</td>
                <td>{{ fmtDate(r.effectiveDate) }}</td>
                <td class="td-actions">
                  <template v-if="confirmDeleteId !== r.id">
                    <button class="action-btn edit-btn" title="Edit" @click="startEdit(r)">
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="15" height="15">
                        <path d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z"/>
                      </svg>
                    </button>
                    <button class="action-btn delete-btn" title="Delete" @click="confirmDeleteId = r.id">
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="15" height="15">
                        <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd"/>
                      </svg>
                    </button>
                  </template>
                  <template v-else>
                    <span class="confirm-label">Delete?</span>
                    <button class="action-btn confirm-yes" @click="confirmDelete(r.id)">Yes</button>
                    <button class="action-btn confirm-no" @click="confirmDeleteId = null">No</button>
                  </template>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.settings-page { min-height: 100vh; background: linear-gradient(160deg, #f5f3ff 0%, #f0fdf4 100%); padding-bottom: 3rem; }
.page-inner { max-width: 840px; margin: 0 auto; padding: 2rem 1.5rem 0; }
.page-header { margin-bottom: 1.75rem; }
.page-title { font-size: 1.6rem; font-weight: 700; color: #1e1b4b; margin: 0 0 0.2rem; }
.page-sub { font-size: 0.875rem; color: #6b7280; margin: 0; }

.section-card { background: #fff; border-radius: 1rem; box-shadow: 0 1px 6px rgba(0,0,0,0.06); padding: 1.5rem; margin-bottom: 1.25rem; }
.section-header-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.section-title { font-size: 1.05rem; font-weight: 700; color: #111827; margin: 0 0 0.25rem; }
.section-desc { font-size: 0.85rem; color: #6b7280; margin: 0 0 1rem; }

.pref-row { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
.pref-select { padding: 0.55rem 0.8rem; border: 1.5px solid #d1d5db; border-radius: 0.6rem; font-size: 0.9rem; color: #111827; background: #f9fafb; outline: none; min-width: 200px; }
.pref-select:focus { border-color: #6366f1; background: #fff; box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15); }
.saved-badge { font-size: 0.82rem; color: #059669; font-weight: 600; }

.btn-primary { padding: 0.5rem 1.1rem; font-size: 0.88rem; font-weight: 600; color: #fff; background: #4f46e5; border: none; border-radius: 0.55rem; cursor: pointer; transition: background 0.12s; }
.btn-primary:hover:not(:disabled) { background: #4338ca; }
.btn-primary:disabled { opacity: 0.55; cursor: not-allowed; }
.btn-sm { padding: 0.4rem 0.9rem; font-size: 0.84rem; }
.btn-cancel { padding: 0.5rem 1.1rem; font-size: 0.88rem; font-weight: 500; color: #374151; background: #f3f4f6; border: none; border-radius: 0.55rem; cursor: pointer; transition: background 0.12s; }
.btn-cancel:hover { background: #e5e7eb; }

.rate-form { background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1.25rem; margin-top: 1rem; }
.rate-form-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 0.75rem; }
.rate-form-actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 1rem; }
.form-error { display: flex; align-items: center; gap: 0.4rem; background: #fef2f2; color: #b91c1c; border: 1px solid #fecaca; border-radius: 0.5rem; padding: 0.5rem 0.75rem; font-size: 0.82rem; margin-bottom: 0.75rem; }

.field { display: flex; flex-direction: column; gap: 0.3rem; }
.field label { font-size: 0.78rem; font-weight: 500; color: #6b7280; text-transform: uppercase; letter-spacing: 0.03em; }
.field input, .field select { padding: 0.5rem 0.75rem; border: 1.5px solid #d1d5db; border-radius: 0.55rem; font-size: 0.9rem; color: #111827; background: #fff; outline: none; transition: border-color 0.15s; }
.field input:focus, .field select:focus { border-color: #6366f1; box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15); }

.rates-loading { padding: 2rem; text-align: center; color: #9ca3af; font-size: 0.9rem; }
.rates-empty { text-align: center; padding: 2rem; color: #9ca3af; font-size: 0.9rem; }

.table-wrap { overflow-x: auto; margin-top: 1rem; }
.rate-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.rate-table th { padding: 0.6rem 0.85rem; text-align: left; font-size: 0.73rem; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: 0.04em; background: #f9fafb; border-bottom: 1px solid #e5e7eb; white-space: nowrap; }
.th-right { text-align: right; }
.th-center { text-align: center; }
.rate-row { border-bottom: 1px solid #f3f4f6; transition: background 0.1s; }
.rate-row:hover { background: #fafafa; }
.rate-table td { padding: 0.65rem 0.85rem; vertical-align: middle; color: #111827; }
.td-right { text-align: right; font-weight: 600; font-variant-numeric: tabular-nums; }
.td-actions { text-align: center; white-space: nowrap; }

.action-btn { display: inline-flex; align-items: center; justify-content: center; padding: 0.3rem; border: none; border-radius: 0.375rem; cursor: pointer; transition: background 0.15s, color 0.15s; background: transparent; line-height: 1; }
.edit-btn { color: #6b7280; } .edit-btn:hover { background: #eef2ff; color: #4f46e5; }
.delete-btn { color: #6b7280; margin-left: 0.25rem; } .delete-btn:hover { background: #fef2f2; color: #dc2626; }
.confirm-label { font-size: 0.8rem; color: #dc2626; font-weight: 500; margin-right: 0.4rem; }
.confirm-yes { font-size: 0.78rem; font-weight: 600; color: #fff; background: #dc2626; border-radius: 0.375rem; padding: 0.2rem 0.55rem; border: none; cursor: pointer; }
.confirm-yes:hover { background: #b91c1c; }
.confirm-no { font-size: 0.78rem; font-weight: 600; color: #374151; background: #f3f4f6; border-radius: 0.375rem; padding: 0.2rem 0.55rem; border: none; cursor: pointer; margin-left: 0.25rem; }
.confirm-no:hover { background: #e5e7eb; }
</style>
