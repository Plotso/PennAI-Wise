<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth.js'
import api from '../services/api.js'

const router = useRouter()
const auth = useAuthStore()

const form = ref({ email: '', password: '', confirmPassword: '' })
const error = ref(null)
const loading = ref(false)

// Per-field validation messages (shown after first submit attempt)
const touched = ref(false)

const passwordError = computed(() => {
  if (!touched.value) return null
  if (form.value.password.length > 0 && form.value.password.length < 6) {
    return 'Password must be at least 6 characters.'
  }
  return null
})

const confirmError = computed(() => {
  if (!touched.value) return null
  if (form.value.confirmPassword && form.value.password !== form.value.confirmPassword) {
    return 'Passwords do not match.'
  }
  return null
})

async function handleRegister() {
  touched.value = true
  error.value = null

  if (form.value.password.length < 6) {
    return
  }
  if (form.value.password !== form.value.confirmPassword) {
    return
  }

  loading.value = true
  try {
    const { data } = await api.post('/auth/register', {
      email: form.value.email,
      password: form.value.password,
    })
    auth.setAuth(data.token, { email: data.email })
    router.push({ name: 'Expenses' })
  } catch (e) {
    const detail = e.response?.data
    // Handle ASP.NET ValidationProblem shape
    if (detail?.errors) {
      const msgs = Object.values(detail.errors).flat()
      error.value = msgs[0] ?? 'Registration failed.'
    } else {
      error.value = detail?.message ?? 'Registration failed. Please try again.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <div class="auth-card">

      <!-- Brand -->
      <div class="auth-brand">
        <div class="auth-logo">💰</div>
        <h1 class="auth-title">PennAI‑Wise</h1>
        <p class="auth-subtitle">Create your free account</p>
      </div>

      <!-- Error banner -->
      <div v-if="error" class="auth-error" role="alert">
        <span>⚠</span> {{ error }}
      </div>

      <!-- Form -->
      <form @submit.prevent="handleRegister" class="auth-form" novalidate>
        <div class="field">
          <label for="email">Email address</label>
          <input
            id="email"
            v-model="form.email"
            type="email"
            autocomplete="email"
            placeholder="you@example.com"
            required
          />
        </div>

        <div class="field">
          <label for="password">Password</label>
          <input
            id="password"
            v-model="form.password"
            type="password"
            autocomplete="new-password"
            placeholder="Min. 6 characters"
            required
            :class="{ 'input-error': passwordError }"
          />
          <p v-if="passwordError" class="field-error">{{ passwordError }}</p>
        </div>

        <div class="field">
          <label for="confirmPassword">Confirm password</label>
          <input
            id="confirmPassword"
            v-model="form.confirmPassword"
            type="password"
            autocomplete="new-password"
            placeholder="••••••••"
            required
            :class="{ 'input-error': confirmError }"
          />
          <p v-if="confirmError" class="field-error">{{ confirmError }}</p>
        </div>

        <button type="submit" class="auth-btn" :disabled="loading">
          <span v-if="loading" class="spinner" />
          {{ loading ? 'Creating account…' : 'Create account' }}
        </button>
      </form>

      <!-- Footer link -->
      <p class="auth-footer">
        Already have an account?
        <RouterLink to="/login">Sign in</RouterLink>
      </p>
    </div>
  </div>
</template>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #eef2ff 0%, #f0fdf4 100%);
  padding: 1.5rem;
}

.auth-card {
  width: 100%;
  max-width: 420px;
  background: #fff;
  border-radius: 1.25rem;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08), 0 1px 4px rgba(0, 0, 0, 0.04);
  padding: 2.5rem 2rem;
}

/* ── Brand ───────────────────────────────────────────── */
.auth-brand {
  text-align: center;
  margin-bottom: 1.75rem;
}

.auth-logo {
  font-size: 2.5rem;
  line-height: 1;
  margin-bottom: 0.5rem;
}

.auth-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1e1b4b;
  margin: 0 0 0.25rem;
}

.auth-subtitle {
  font-size: 0.9rem;
  color: #6b7280;
  margin: 0;
}

/* ── Error banner ────────────────────────────────────── */
.auth-error {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  background: #fef2f2;
  color: #b91c1c;
  border: 1px solid #fecaca;
  border-radius: 0.6rem;
  padding: 0.65rem 0.9rem;
  font-size: 0.875rem;
  margin-bottom: 1.2rem;
}

/* ── Form ────────────────────────────────────────────── */
.auth-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.field label {
  font-size: 0.85rem;
  font-weight: 500;
  color: #374151;
}

.field input {
  width: 100%;
  padding: 0.6rem 0.85rem;
  border: 1.5px solid #d1d5db;
  border-radius: 0.625rem;
  font-size: 0.95rem;
  color: #111827;
  background: #f9fafb;
  transition: border-color 0.15s, box-shadow 0.15s, background 0.15s;
  outline: none;
  box-sizing: border-box;
}

.field input::placeholder {
  color: #9ca3af;
}

.field input:focus {
  border-color: #6366f1;
  background: #fff;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.field input.input-error {
  border-color: #f87171;
}

.field input.input-error:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.15);
}

/* ── Per-field error text ────────────────────────────── */
.field-error {
  font-size: 0.8rem;
  color: #dc2626;
  margin: 0;
}

/* ── Submit button ───────────────────────────────────── */
.auth-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-top: 0.5rem;
  width: 100%;
  padding: 0.7rem 1rem;
  background: #4f46e5;
  color: #fff;
  font-size: 0.95rem;
  font-weight: 600;
  border: none;
  border-radius: 0.625rem;
  cursor: pointer;
  transition: background 0.15s, transform 0.1s;
}

.auth-btn:hover:not(:disabled) {
  background: #4338ca;
}

.auth-btn:active:not(:disabled) {
  transform: scale(0.98);
}

.auth-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

/* ── Spinner ─────────────────────────────────────────── */
.spinner {
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* ── Footer link ─────────────────────────────────────── */
.auth-footer {
  text-align: center;
  font-size: 0.875rem;
  color: #6b7280;
  margin-top: 1.5rem;
  margin-bottom: 0;
}

.auth-footer a {
  color: #4f46e5;
  font-weight: 500;
  text-decoration: none;
}

.auth-footer a:hover {
  text-decoration: underline;
}
</style>

