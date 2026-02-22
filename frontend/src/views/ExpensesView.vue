<script setup>
import { useAuthStore } from '../stores/auth.js'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

function handleLogout() {
  auth.logout()
  router.push({ name: 'Login' })
}
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
      <h1 class="text-2xl font-bold text-gray-800 mb-6">Expenses</h1>

      <div class="bg-white rounded-2xl shadow p-6 text-gray-500 text-center">
        <p class="text-lg">No expenses yet.</p>
        <p class="text-sm mt-1">This view will show a list of your tracked expenses.</p>
      </div>
    </main>
  </div>
</template>
