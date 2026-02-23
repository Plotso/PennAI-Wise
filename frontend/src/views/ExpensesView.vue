<script setup>
import { ref, reactive, onMounted } from 'vue'
import api from '../services/api.js'
import ExpenseList from '../components/ExpenseList.vue'
import ExpenseForm from '../components/ExpenseForm.vue'

// ── State ────────────────────────────────────────────────────
const expenses   = ref([])
const categories = ref([])
const loading    = ref(false)
const totalCount = ref(0)
const page       = ref(1)
const pageSize   = 20

// Filter bar state
const filters = reactive({
  startDate:  '',
  endDate:    '',
  categoryId: '',
})

// Applied filters (only updated when "Apply" is clicked)
const applied = reactive({
  startDate:  '',
  endDate:    '',
  categoryId: '',
})

// Form modal
const showForm     = ref(false)
const editingExpense = ref(null)

// ── API calls ────────────────────────────────────────────────
async function loadCategories() {
  try {
    const { data } = await api.get('/categories')
    categories.value = data
  } catch {
    // non-critical
  }
}

async function loadExpenses() {
  loading.value = true
  try {
    const params = { page: page.value, pageSize }
    if (applied.startDate)  params.startDate  = applied.startDate  + 'T00:00:00'
    if (applied.endDate)    params.endDate    = applied.endDate    + 'T23:59:59'
    if (applied.categoryId) params.categoryId = applied.categoryId

    const { data } = await api.get('/expenses', { params })
    expenses.value  = data.items
    totalCount.value = data.totalCount
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadCategories()
  loadExpenses()
})

// ── Filter actions ───────────────────────────────────────────
function applyFilters() {
  applied.startDate  = filters.startDate
  applied.endDate    = filters.endDate
  applied.categoryId = filters.categoryId
  page.value = 1
  loadExpenses()
}

function clearFilters() {
  filters.startDate  = ''
  filters.endDate    = ''
  filters.categoryId = ''
  applied.startDate  = ''
  applied.endDate    = ''
  applied.categoryId = ''
  page.value = 1
  loadExpenses()
}

// ── Form actions ─────────────────────────────────────────────
function openAdd() {
  editingExpense.value = null
  showForm.value = true
}

function openEdit(exp) {
  editingExpense.value = exp
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  editingExpense.value = null
}

function onSaved() {
  page.value = 1
  loadExpenses()
}

// ── Delete ───────────────────────────────────────────────────
async function handleDelete(id) {
  try {
    await api.delete(`/expenses/${id}`)
    // If last item on a page > 1, go back one page
    if (expenses.value.length === 1 && page.value > 1) {
      page.value--
    }
    loadExpenses()
  } catch {
    // Could surface a toast here
  }
}

// ── Pagination ───────────────────────────────────────────────
function onPageChange(p) {
  page.value = p
  loadExpenses()
}
</script>

<template>
  <div class="expenses-page">
    <div class="page-inner">

      <!-- ── Page header ───────────────────────────────────── -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Expenses</h1>
          <p class="page-sub">Track and manage all your spending.</p>
        </div>
        <button class="btn-add" @click="openAdd">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="16" height="16">
            <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
          </svg>
          Add Expense
        </button>
      </div>

      <!-- ── Filter bar ─────────────────────────────────────── -->
      <div class="filter-card">
        <div class="filter-row">
          <div class="filter-field">
            <label>From</label>
            <input v-model="filters.startDate" type="date" />
          </div>
          <div class="filter-field">
            <label>To</label>
            <input v-model="filters.endDate" type="date" />
          </div>
          <div class="filter-field filter-cat">
            <label>Category</label>
            <select v-model="filters.categoryId">
              <option value="">All categories</option>
              <option v-for="cat in categories" :key="cat.id" :value="cat.id">
                {{ cat.name }}
              </option>
            </select>
          </div>
          <div class="filter-actions">
            <button class="btn-apply" @click="applyFilters">Apply</button>
            <button class="btn-clear" @click="clearFilters">Clear</button>
          </div>
        </div>
      </div>

      <!-- ── Expense list card ─────────────────────────────── -->
      <div class="list-card">
        <ExpenseList
          :expenses="expenses"
          :loading="loading"
          :page="page"
          :page-size="pageSize"
          :total-count="totalCount"
          @edit="openEdit"
          @delete="handleDelete"
          @page-change="onPageChange"
        />
      </div>
    </div>

    <!-- ── Expense form modal ────────────────────────────── -->
    <ExpenseForm
      v-if="showForm"
      :expense="editingExpense"
      :categories="categories"
      @saved="onSaved"
      @close="closeForm"
    />
  </div>
</template>

<style scoped>
.expenses-page {
  min-height: 100vh;
  background: linear-gradient(160deg, #f5f3ff 0%, #f0fdf4 100%);
  padding-bottom: 3rem;
}

.page-inner {
  max-width: 960px;
  margin: 0 auto;
  padding: 2rem 1.5rem 0;
}

/* ── Page header ─────────────────────────────────────── */
.page-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  margin-bottom: 1.5rem;
  gap: 1rem;
  flex-wrap: wrap;
}

.page-title {
  font-size: 1.6rem;
  font-weight: 700;
  color: #1e1b4b;
  margin: 0 0 0.2rem;
}

.page-sub {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.btn-add {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.6rem 1.25rem;
  font-size: 0.9rem;
  font-weight: 600;
  color: #fff;
  background: #4f46e5;
  border: none;
  border-radius: 0.65rem;
  cursor: pointer;
  transition: background 0.15s, transform 0.1s;
  flex-shrink: 0;
}
.btn-add:hover  { background: #4338ca; }
.btn-add:active { transform: scale(0.97); }

/* ── Filter bar ──────────────────────────────────────── */
.filter-card {
  background: #fff;
  border-radius: 1rem;
  box-shadow: 0 1px 6px rgba(0,0,0,0.06);
  padding: 1.1rem 1.25rem;
  margin-bottom: 1.25rem;
}

.filter-row {
  display: flex;
  align-items: flex-end;
  gap: 1rem;
  flex-wrap: wrap;
}

.filter-field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  min-width: 140px;
}
.filter-cat { flex: 1; min-width: 160px; }

.filter-field label {
  font-size: 0.78rem;
  font-weight: 500;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.filter-field input,
.filter-field select {
  padding: 0.5rem 0.75rem;
  border: 1.5px solid #d1d5db;
  border-radius: 0.55rem;
  font-size: 0.9rem;
  color: #111827;
  background: #f9fafb;
  outline: none;
  transition: border-color 0.15s;
}
.filter-field input:focus,
.filter-field select:focus {
  border-color: #6366f1;
  background: #fff;
}

.filter-actions {
  display: flex;
  gap: 0.5rem;
  align-items: flex-end;
  margin-left: auto;
}

.btn-apply {
  padding: 0.5rem 1.1rem;
  font-size: 0.88rem;
  font-weight: 600;
  color: #fff;
  background: #4f46e5;
  border: none;
  border-radius: 0.55rem;
  cursor: pointer;
  transition: background 0.12s;
}
.btn-apply:hover { background: #4338ca; }

.btn-clear {
  padding: 0.5rem 1.1rem;
  font-size: 0.88rem;
  font-weight: 500;
  color: #374151;
  background: #f3f4f6;
  border: none;
  border-radius: 0.55rem;
  cursor: pointer;
  transition: background 0.12s;
}
.btn-clear:hover { background: #e5e7eb; }

/* ── List card ───────────────────────────────────────── */
.list-card {
  background: #fff;
  border-radius: 1rem;
  box-shadow: 0 1px 6px rgba(0,0,0,0.06);
  overflow: hidden;
}
</style>

