<script setup>
import { ref } from 'vue'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  BarElement,
  CategoryScale,
  LinearScale,
} from 'chart.js'
import { useAuthStore } from '../stores/auth.js'
import { useRouter } from 'vue-router'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale)

const auth = useAuthStore()
const router = useRouter()

function handleLogout() {
  auth.logout()
  router.push({ name: 'Login' })
}

const chartData = ref({
  labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
  datasets: [
    {
      label: 'Monthly Expenses ($)',
      data: [0, 0, 0, 0, 0, 0],
      backgroundColor: '#6366f1',
      borderRadius: 6,
    },
  ],
})

const chartOptions = ref({
  responsive: true,
  plugins: {
    legend: { position: 'top' },
    title: { display: false },
  },
})
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Nav -->
    <nav class="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
      <div class="flex items-center gap-6">
        <span class="text-lg font-bold text-indigo-600">PennAI Wise</span>
        <RouterLink to="/expenses" class="text-sm font-medium text-gray-700 hover:text-indigo-600">Expenses</RouterLink>
        <RouterLink to="/dashboard" class="text-sm font-medium text-gray-700 hover:text-indigo-600">Dashboard</RouterLink>
      </div>
      <button @click="handleLogout" class="text-sm text-gray-500 hover:text-red-600 transition">
        Sign out
      </button>
    </nav>

    <!-- Content -->
    <main class="max-w-4xl mx-auto px-6 py-8">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Dashboard</h1>

      <div class="bg-white rounded-2xl shadow p-6 mb-6">
        <h2 class="text-lg font-semibold text-gray-700 mb-4">Monthly Spending</h2>
        <Bar :data="chartData" :options="chartOptions" />
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div class="bg-white rounded-2xl shadow p-6 text-center">
          <p class="text-sm text-gray-500">Total This Month</p>
          <p class="text-3xl font-bold text-indigo-600 mt-1">$0.00</p>
        </div>
        <div class="bg-white rounded-2xl shadow p-6 text-center">
          <p class="text-sm text-gray-500">Transactions</p>
          <p class="text-3xl font-bold text-indigo-600 mt-1">0</p>
        </div>
      </div>
    </main>
  </div>
</template>
