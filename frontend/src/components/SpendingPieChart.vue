<script setup>
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps({
  categoryBreakdown: { type: Array, default: () => [] },
  currency:          { type: String, default: 'EUR' },
})

// Deterministic palette fallback
const PALETTE = [
  '#6366f1','#ec4899','#f59e0b','#10b981',
  '#3b82f6','#8b5cf6','#e11d48','#0ea5e9',
  '#14b8a6','#f97316',
]
function fallbackColor(name, idx) {
  if (name) {
    let h = 0
    for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) & 0xffffffff
    return PALETTE[Math.abs(h) % PALETTE.length]
  }
  return PALETTE[idx % PALETTE.length]
}

const colors = computed(() =>
  props.categoryBreakdown.map((c, i) => c.color || fallbackColor(c.categoryName, i))
)

const chartData = computed(() => ({
  labels: props.categoryBreakdown.map(c => c.categoryName),
  datasets: [{
    data:            props.categoryBreakdown.map(c => c.total),
    backgroundColor: colors.value,
    borderColor:     colors.value.map(c => c + 'cc'),
    borderWidth:     1.5,
    hoverOffset:     8,
  }],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: true,
  cutout: '62%',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label(ctx) {
          const item = props.categoryBreakdown[ctx.dataIndex]
          return ` ${item.categoryName}: ${fmtCurrency(item.total)} (${item.percentage.toFixed(1)}%)`
        },
      },
    },
  },
}

function fmtCurrency(n) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: props.currency }).format(n)
}
</script>

<template>
  <div class="pie-wrap">
    <div v-if="!categoryBreakdown.length" class="pie-empty">
      <p>No category data yet.</p>
    </div>
    <template v-else>
      <div class="chart-area">
        <Doughnut :data="chartData" :options="chartOptions" />
      </div>
      <ul class="legend">
        <li
          v-for="(item, i) in categoryBreakdown"
          :key="item.categoryName"
          class="legend-item"
        >
          <span class="legend-dot" :style="{ background: colors[i] }" />
          <span class="legend-name">{{ item.categoryName }}</span>
          <span class="legend-pct">{{ item.percentage.toFixed(1) }}%</span>
          <span class="legend-amt">{{ fmtCurrency(item.total) }}</span>
        </li>
      </ul>
    </template>
  </div>
</template>

<style scoped>
.pie-wrap {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  height: 100%;
}

.chart-area {
  max-width: 220px;
  margin: 0 auto;
}

.pie-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 1;
  color: #9ca3af;
  font-size: 0.9rem;
}

/* ── Legend ──────────────────────────────────────────── */
.legend {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.legend-item {
  display: grid;
  grid-template-columns: 0.65rem 1fr auto auto;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.83rem;
}

.legend-dot {
  width: 0.65rem;
  height: 0.65rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.legend-name {
  color: #374151;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.legend-pct {
  color: #6b7280;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.legend-amt {
  color: #111827;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}
</style>
