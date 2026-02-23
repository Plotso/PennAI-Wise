<script setup>
import { ref, computed, watch } from 'vue'
import api from '../services/api.js'

const props = defineProps({
  expense:    { type: Object, default: null },   // null = add mode
  categories: { type: Array,  default: () => [] },
})

const emit = defineEmits(['saved', 'close'])

// ── Helpers ─────────────────────────────────────────────────
function todayStr() {
  return new Date().toISOString().slice(0, 10)
}

function toDateInput(isoStr) {
  if (!isoStr) return todayStr()
  return new Date(isoStr).toISOString().slice(0, 10)
}

// ── Form state ───────────────────────────────────────────────
const isEdit = computed(() => !!props.expense)

const defaults = () => ({
  amount:      '',
  description: '',
  date:        todayStr(),
  categoryId:  '',
})

const form     = ref(defaults())
const errors   = ref({})
const loading  = ref(false)
const apiError = ref(null)

// Populate form when editing
watch(() => props.expense, (exp) => {
  if (exp) {
    form.value = {
      amount:      exp.amount,
      description: exp.description,
      date:        toDateInput(exp.date),
      categoryId:  exp.categoryId,
    }
  } else {
    form.value = defaults()
  }
  errors.value  = {}
  apiError.value = null
}, { immediate: true })

// ── Validation ───────────────────────────────────────────────
function validate() {
  const e = {}
  if (!form.value.amount || Number(form.value.amount) <= 0)
    e.amount = 'Amount must be greater than 0.'
  if (!form.value.description.trim())
    e.description = 'Description is required.'
  if (!form.value.date)
    e.date = 'Date is required.'
  if (!form.value.categoryId)
    e.categoryId = 'Please select a category.'
  errors.value = e
  return Object.keys(e).length === 0
}

// ── Submit ───────────────────────────────────────────────────
async function handleSubmit() {
  if (!validate()) return
  loading.value  = true
  apiError.value = null

  const payload = {
    amount:      Number(form.value.amount),
    description: form.value.description.trim(),
    date:        form.value.date + 'T00:00:00',
    categoryId:  Number(form.value.categoryId),
  }

  try {
    if (isEdit.value) {
      const { data } = await api.put(`/expenses/${props.expense.id}`, payload)
      emit('saved', data)
    } else {
      const { data } = await api.post('/expenses', payload)
      emit('saved', data)
    }
    emit('close')
  } catch (e) {
    const detail = e.response?.data
    if (detail?.errors) {
      const msgs = Object.values(detail.errors).flat()
      apiError.value = msgs[0] ?? 'Something went wrong.'
    } else {
      apiError.value = detail?.message ?? 'Something went wrong. Please try again.'
    }
  } finally {
    loading.value = false
  }
}

// Close on backdrop click
function onBackdrop(e) {
  if (e.target === e.currentTarget) emit('close')
}
</script>

<template>
  <!-- Backdrop -->
  <div class="modal-backdrop" @click="onBackdrop">
    <div class="modal-card" role="dialog" aria-modal="true">

      <!-- Header -->
      <div class="modal-header">
        <h2 class="modal-title">{{ isEdit ? 'Edit Expense' : 'Add Expense' }}</h2>
        <button class="modal-close" @click="$emit('close')" aria-label="Close">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="18" height="18">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>

      <!-- API error banner -->
      <div v-if="apiError" class="form-api-error">
        <span>⚠</span> {{ apiError }}
      </div>

      <!-- Form body -->
      <form class="modal-body" @submit.prevent="handleSubmit" novalidate>

        <!-- Amount -->
        <div class="field">
          <label for="ef-amount">Amount</label>
          <div class="input-prefix-wrap">
            <span class="input-prefix">$</span>
            <input
              id="ef-amount"
              v-model="form.amount"
              type="number"
              min="0.01"
              step="0.01"
              placeholder="0.00"
              :class="['prefix-input', { 'input-err': errors.amount }]"
            />
          </div>
          <p v-if="errors.amount" class="field-error">{{ errors.amount }}</p>
        </div>

        <!-- Description -->
        <div class="field">
          <label for="ef-desc">Description</label>
          <input
            id="ef-desc"
            v-model="form.description"
            type="text"
            maxlength="200"
            placeholder="e.g. Grocery shopping"
            :class="{ 'input-err': errors.description }"
          />
          <p v-if="errors.description" class="field-error">{{ errors.description }}</p>
        </div>

        <!-- Date -->
        <div class="field">
          <label for="ef-date">Date</label>
          <input
            id="ef-date"
            v-model="form.date"
            type="date"
            :class="{ 'input-err': errors.date }"
          />
          <p v-if="errors.date" class="field-error">{{ errors.date }}</p>
        </div>

        <!-- Category -->
        <div class="field">
          <label for="ef-cat">Category</label>
          <select
            id="ef-cat"
            v-model="form.categoryId"
            :class="{ 'input-err': errors.categoryId }"
          >
            <option value="" disabled>Select a category…</option>
            <option
              v-for="cat in categories"
              :key="cat.id"
              :value="cat.id"
            >{{ cat.name }}</option>
          </select>
          <p v-if="errors.categoryId" class="field-error">{{ errors.categoryId }}</p>
        </div>

        <!-- Footer buttons -->
        <div class="modal-footer">
          <button type="button" class="btn-cancel" @click="$emit('close')">Cancel</button>
          <button type="submit" class="btn-submit" :disabled="loading">
            <span v-if="loading" class="spinner" />
            {{ loading ? 'Saving…' : (isEdit ? 'Save Changes' : 'Add Expense') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
/* ── Backdrop ────────────────────────────────────────── */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(17, 24, 39, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
  padding: 1rem;
  backdrop-filter: blur(2px);
}

/* ── Card ────────────────────────────────────────────── */
.modal-card {
  background: #fff;
  border-radius: 1.125rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.18);
  width: 100%;
  max-width: 440px;
  overflow: hidden;
}

/* ── Header ──────────────────────────────────────────── */
.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem 0;
}

.modal-title {
  font-size: 1.05rem;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.modal-close {
  background: transparent;
  border: none;
  cursor: pointer;
  color: #9ca3af;
  border-radius: 0.375rem;
  padding: 0.2rem;
  display: flex;
  align-items: center;
  transition: color 0.12s, background 0.12s;
}
.modal-close:hover { color: #374151; background: #f3f4f6; }

/* ── API error ───────────────────────────────────────── */
.form-api-error {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  background: #fef2f2;
  color: #b91c1c;
  border: 1px solid #fecaca;
  border-radius: 0.5rem;
  padding: 0.6rem 0.85rem;
  font-size: 0.85rem;
  margin: 1rem 1.5rem 0;
}

/* ── Body / Form ─────────────────────────────────────── */
.modal-body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.25rem 1.5rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.field label {
  font-size: 0.83rem;
  font-weight: 500;
  color: #374151;
}

.field input,
.field select {
  width: 100%;
  padding: 0.58rem 0.8rem;
  border: 1.5px solid #d1d5db;
  border-radius: 0.6rem;
  font-size: 0.92rem;
  color: #111827;
  background: #f9fafb;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s, background 0.15s;
  box-sizing: border-box;
}

.field input:focus,
.field select:focus {
  border-color: #6366f1;
  background: #fff;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.field input.input-err,
.field select.input-err {
  border-color: #f87171;
}
.field input.input-err:focus,
.field select.input-err:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.12);
}

/* Amount prefix "$" */
.input-prefix-wrap {
  display: flex;
  align-items: stretch;
  border: 1.5px solid #d1d5db;
  border-radius: 0.6rem;
  overflow: hidden;
  background: #f9fafb;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.input-prefix-wrap:focus-within {
  border-color: #6366f1;
  background: #fff;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}
.input-prefix {
  padding: 0 0.65rem;
  background: #f3f4f6;
  border-right: 1.5px solid #d1d5db;
  font-size: 0.92rem;
  color: #6b7280;
  display: flex;
  align-items: center;
  user-select: none;
}
.prefix-input {
  flex: 1;
  border: none !important;
  border-radius: 0 !important;
  box-shadow: none !important;
  background: transparent !important;
  padding: 0.58rem 0.8rem;
  outline: none;
  font-size: 0.92rem;
  color: #111827;
}

.field-error {
  font-size: 0.78rem;
  color: #dc2626;
  margin: 0;
}

/* ── Footer ──────────────────────────────────────────── */
.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.65rem;
  padding-top: 0.25rem;
}

.btn-cancel {
  padding: 0.55rem 1.1rem;
  font-size: 0.9rem;
  font-weight: 500;
  color: #374151;
  background: #f3f4f6;
  border: none;
  border-radius: 0.6rem;
  cursor: pointer;
  transition: background 0.12s;
}
.btn-cancel:hover { background: #e5e7eb; }

.btn-submit {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.55rem 1.4rem;
  font-size: 0.9rem;
  font-weight: 600;
  color: #fff;
  background: #4f46e5;
  border: none;
  border-radius: 0.6rem;
  cursor: pointer;
  transition: background 0.12s, transform 0.1s;
}
.btn-submit:hover:not(:disabled) { background: #4338ca; }
.btn-submit:active:not(:disabled) { transform: scale(0.98); }
.btn-submit:disabled { opacity: 0.55; cursor: not-allowed; }

/* ── Spinner ─────────────────────────────────────────── */
.spinner {
  width: 0.9rem;
  height: 0.9rem;
  border: 2px solid rgba(255,255,255,0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>
