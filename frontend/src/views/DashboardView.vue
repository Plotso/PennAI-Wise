<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../services/api.js'
import SpendingPieChart from '../components/SpendingPieChart.vue'
import MonthlyBarChart  from '../components/MonthlyBarChart.vue'

// ── Month state ──────────────────────────────────────────────
const now      = new Date()
const month    = ref(now.getMonth() + 1)
const year     = ref(now.getFullYear())

const MONTH_NAMES = [
  'January','February','March','April','May','June',
  'July','August','September','October','November','December',
]

const monthLabel = computed(() => `${MONTH_NAMES[month.value - 1]} ${year.value}`)

function prevMonth() {
  if (month.value === 1) { month.value = 12; year.value-- }
  else month.value--
  loadDashboard()
}
function nextMonth() {
  if (month.value === 12) { month.value = 1; year.value++ }
  else month.value++
  loadDashboard()
}

// ── Data ─────────────────────────────────────────────────────
const data    = ref(null)
const loading = ref(false)
const error   = ref(null)

async function loadDashboard() {
  loading.value = true
  error.value   = null
  try {
    const { data: d } = await api.get('/dashboard', {
      params: { month: month.value, year: year.value },
    })
    data.value = d
  } catch {
    error.value = 'Failed to load dashboard data.'
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)

// ── Formatting helpers ───────────────────────────────────────
function fmtCurrency(n) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n ?? 0)
}
function fmtDate(iso) {
  if (!iso) return '—'
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' }).format(new Date(iso))
}
</script>

<template>
  <div class="dash-page">
    <div class="dash-inner">

      <!-- ── Page header + month selector ─────────────────── -->
      <div class="dash-header">
        <div>
          <h1 class="dash-title">Dashboard</h1>
          <p class="dash-sub">Your spending overview at a glance.</p>
        </div>

        <div class="month-nav">
          <button class="month-arrow" @click="prevMonth" aria-label="Previous month">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="16" height="16">
              <path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd"/>
            </svg>
          </button>
          <span class="month-label">{{ monthLabel }}</span>
          <button class="month-arrow" @click="nextMonth" aria-label="Next month">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="16" height="16">
              <path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 0 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- ── Error ─────────────────────────────────────────── -->
      <div v-if="error" class="dash-error">⚠ {{ error }}</div>

      <!-- ── Loading skeletons ─────────────────────────────── -->
      <template v-if="loading">
        <!-- Stat cards skeleton -->
        <div class="stats-grid">
          <div v-for="n in 4" :key="n" class="stat-card sk-block" style="height: 100px;" />
        </div>
        <!-- Chart row skeleton -->
        <div class="charts-row">
          <div class="chart-card sk-block" style="height: 360px;" />
          <div class="chart-card sk-block" style="height: 360px;" />
        </div>
      </template>

      <!-- ── Content ───────────────────────────────────────── -->
      <template v-else-if="data">

        <!-- ── Summary stat cards ────────────────────────── -->
        <div class="stats-grid">

          <!-- Total Spent -->
          <div class="stat-card stat-indigo">
            <div class="stat-icon">💸</div>
            <div class="stat-body">
              <p class="stat-label">Total Spent</p>
              <p class="stat-value">{{ fmtCurrency(data.totalSpent) }}</p>
            </div>
          </div>

          <!-- Transactions -->
          <div class="stat-card stat-violet">
            <div class="stat-icon">🧾</div>
            <div class="stat-body">
              <p class="stat-label">Transactions</p>
              <p class="stat-value">{{ data.transactionCount }}</p>
            </div>
          </div>

          <!-- Highest Expense -->
          <div class="stat-card stat-rose">
            <div class="stat-icon">📈</div>
            <div class="stat-body">
              <p class="stat-label">Highest Expense</p>
              <p class="stat-value">
                {{ data.highestExpense ? fmtCurrency(data.highestExpense.amount) : '—' }}
              </p>
              <p v-if="data.highestExpense" class="stat-meta">
                {{ data.highestExpense.description }} · {{ fmtDate(data.highestExpense.date) }}
              </p>
            </div>
          </div>

          <!-- Top Category -->
          <div class="stat-card stat-emerald">
            <div class="stat-icon">🏷️</div>
            <div class="stat-body">
              <p class="stat-label">Top Category</p>
              <p class="stat-value">{{ data.topCategory ?? '—' }}</p>
            </div>
          </div>
        </div>

        <!-- ── Charts row ─────────────────────────────────── -->
        <div class="charts-row">

          <!-- Doughnut — category breakdown -->
          <div class="chart-card">
            <h2 class="chart-title">Spending by Category</h2>
            <SpendingPieChart :category-breakdown="data.categoryBreakdown" />
          </div>

          <!-- Bar — daily spending -->
          <div class="chart-card chart-card-bar">
            <h2 class="chart-title">Daily Spending</h2>
            <MonthlyBarChart
              :daily-spending="data.dailySpending"
              :month="month"
              :year="year"
            />
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.dash-page {
  min-height: 100vh;
  background: linear-gradient(160deg, #f5f3ff 0%, #f0fdf4 100%);
  padding-bottom: 3rem;
}

.dash-inner {
  max-width: 960px;
  margin: 0 auto;
  padding: 2rem 1.5rem 0;
}

/* ── Header ──────────────────────────────────────────── */
.dash-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1.75rem;
}

.dash-title {
  font-size: 1.6rem;
  font-weight: 700;
  color: #1e1b4b;
  margin: 0 0 0.2rem;
}

.dash-sub {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

/* ── Month selector ──────────────────────────────────── */
.month-nav {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: #fff;
  border: 1.5px solid #e5e7eb;
  border-radius: 0.75rem;
  padding: 0.35rem 0.6rem;
  box-shadow: 0 1px 4px rgba(0,0,0,0.05);
}

.month-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1.8rem;
  height: 1.8rem;
  background: transparent;
  border: none;
  border-radius: 0.4rem;
  cursor: pointer;
  color: #6b7280;
  transition: background 0.12s, color 0.12s;
}
.month-arrow:hover { background: #f3f4f6; color: #111827; }

.month-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: #1e1b4b;
  min-width: 9rem;
  text-align: center;
  user-select: none;
}

/* ── Error ───────────────────────────────────────────── */
.dash-error {
  background: #fef2f2;
  color: #b91c1c;
  border: 1px solid #fecaca;
  border-radius: 0.75rem;
  padding: 0.75rem 1rem;
  font-size: 0.875rem;
  margin-bottom: 1.25rem;
}

/* ── Skeleton ────────────────────────────────────────── */
.sk-block {
  background: linear-gradient(90deg, #f3f4f6 25%, #e9eaed 50%, #f3f4f6 75%);
  background-size: 200% 100%;
  animation: shimmer 1.4s infinite;
  border-radius: 1rem;
}
@keyframes shimmer { to { background-position: -200% 0; } }

/* ── Stat cards ──────────────────────────────────────── */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  margin-bottom: 1.25rem;
}

@media (max-width: 720px) {
  .stats-grid { grid-template-columns: repeat(2, 1fr); }
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.1rem 1.25rem;
  border-radius: 1rem;
  box-shadow: 0 1px 6px rgba(0,0,0,0.07);
}

.stat-icon {
  font-size: 1.6rem;
  line-height: 1;
  flex-shrink: 0;
}

.stat-body {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.stat-label {
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin: 0 0 0.2rem;
  opacity: 0.72;
}

.stat-value {
  font-size: 1.3rem;
  font-weight: 700;
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.stat-meta {
  font-size: 0.72rem;
  margin: 0.2rem 0 0;
  opacity: 0.65;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Card color themes */
.stat-indigo  { background: #eef2ff; color: #3730a3; }
.stat-violet  { background: #f5f3ff; color: #5b21b6; }
.stat-rose    { background: #fff1f2; color: #9f1239; }
.stat-emerald { background: #ecfdf5; color: #065f46; }

/* ── Charts row ──────────────────────────────────────── */
.charts-row {
  display: grid;
  grid-template-columns: 1fr 1.6fr;
  gap: 1.25rem;
  align-items: stretch;
}

@media (max-width: 680px) {
  .charts-row { grid-template-columns: 1fr; }
}

.chart-card {
  background: #fff;
  border-radius: 1rem;
  box-shadow: 0 1px 6px rgba(0,0,0,0.06);
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.chart-card-bar {
  min-height: 360px;
}

.chart-title {
  font-size: 0.9rem;
  font-weight: 700;
  color: #374151;
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
</style>
