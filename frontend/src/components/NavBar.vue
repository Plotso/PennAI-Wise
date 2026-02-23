<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth.js'

const router = useRouter()
const auth = useAuthStore()

function logout() {
  auth.logout()
  router.push({ name: 'Login' })
}
</script>

<template>
  <header v-if="auth.isAuthenticated" class="navbar">
    <!-- Left: brand -->
    <RouterLink to="/expenses" class="navbar-brand">
      <span class="navbar-logo">💰</span>
      <span class="navbar-name">PennAI‑Wise</span>
    </RouterLink>

    <!-- Center: nav links -->
    <nav class="navbar-links">
      <RouterLink to="/expenses" class="nav-link">Expenses</RouterLink>
      <RouterLink to="/dashboard" class="nav-link">Dashboard</RouterLink>
    </nav>

    <!-- Right: user + logout -->
    <div class="navbar-right">
      <span class="navbar-email">{{ auth.user?.email }}</span>
      <button class="logout-btn" @click="logout">Logout</button>
    </div>
  </header>
</template>

<style scoped>
.navbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 3.5rem;
  padding: 0 1.5rem;
  background: #fff;
  border-bottom: 1px solid #e5e7eb;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.05);
  position: sticky;
  top: 0;
  z-index: 100;
}

/* ── Brand ───────────────────────────────────────────── */
.navbar-brand {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  text-decoration: none;
  flex-shrink: 0;
}

.navbar-logo {
  font-size: 1.35rem;
  line-height: 1;
}

.navbar-name {
  font-size: 1rem;
  font-weight: 700;
  color: #1e1b4b;
  letter-spacing: -0.01em;
}

/* ── Nav links ───────────────────────────────────────── */
.navbar-links {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.nav-link {
  padding: 0.35rem 0.75rem;
  border-radius: 0.5rem;
  font-size: 0.9rem;
  font-weight: 500;
  color: #4b5563;
  text-decoration: none;
  transition: background 0.15s, color 0.15s;
}

.nav-link:hover {
  background: #f3f4f6;
  color: #111827;
}

.nav-link.router-link-active {
  background: #eef2ff;
  color: #4f46e5;
}

/* ── Right side ──────────────────────────────────────── */
.navbar-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-shrink: 0;
}

.navbar-email {
  font-size: 0.85rem;
  color: #6b7280;
  max-width: 14rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.logout-btn {
  padding: 0.35rem 0.85rem;
  font-size: 0.85rem;
  font-weight: 500;
  color: #4f46e5;
  background: transparent;
  border: 1.5px solid #c7d2fe;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s, color 0.15s;
}

.logout-btn:hover {
  background: #eef2ff;
  border-color: #818cf8;
}
</style>
