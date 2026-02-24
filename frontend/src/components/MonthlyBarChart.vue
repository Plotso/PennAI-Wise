<script setup>
import { computed } from 'vue'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  BarElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend)

const props = defineProps({
  dailySpending:  { type: Array,  default: () => [] },
  month:          { type: Number, required: true },
  year:           { type: Number, required: true },
  currencySymbol: { type: String, default: '€' },
})

// Build a days-in-month array (1 … N) and map API data onto it
const chartData = computed(() => {
  const daysInMonth = new Date(props.year, props.month, 0).getDate()
  const labels = Array.from({ length: daysInMonth }, (_, i) => String(i + 1))

  // Map day number → total
  const dayMap = {}
  for (const entry of props.dailySpending) {
    const d = new Date(entry.date)
    dayMap[d.getDate()] = entry.total
  }

  const data = labels.map((_, i) => dayMap[i + 1] ?? 0)

  return {
    labels,
    datasets: [{
      label:           'Daily Spending',
      data,
      backgroundColor: 'rgba(99, 102, 241, 0.75)',
      hoverBackgroundColor: '#4f46e5',
      borderRadius:    5,
      borderSkipped:   false,
    }],
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        title(ctx) {
          const day  = Number(ctx[0].label)
          const date = new Date(props.year, props.month - 1, day)
          return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(date)
        },
        label(ctx) {
          return ` ${props.currencySymbol}${Number(ctx.parsed.y).toFixed(2)}`
        },
      },
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: {
        font: { size: 11 },
        color: '#9ca3af',
        // Show only every 5th tick on small screens
        maxTicksLimit: 10,
      },
    },
    y: {
      border: { display: false },
      grid: { color: '#f3f4f6' },
      ticks: {
        font: { size: 11 },
        color: '#9ca3af',
        callback(v) { return props.currencySymbol + v },
      },
      beginAtZero: true,
    },
  },
}))
</script>

<template>
  <div class="bar-wrap">
    <div v-if="!dailySpending.length" class="bar-empty">
      <p>No spending data for this month.</p>
    </div>
    <div v-else class="chart-area">
      <Bar :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>

<style scoped>
.bar-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.bar-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #9ca3af;
  font-size: 0.9rem;
}

.chart-area {
  flex: 1;
  min-height: 220px;
  position: relative;
}
</style>
