<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  expenses:   { type: Array,   default: () => [] },
  loading:    { type: Boolean, default: false },
  page:       { type: Number,  default: 1 },
  pageSize:   { type: Number,  default: 20 },
  totalCount: { type: Number,  default: 0 },
})

const emit = defineEmits(['edit', 'delete', 'page-change'])

const confirmId = ref(null)

const totalPages = computed(() => Math.max(1, Math.ceil(props.totalCount / props.pageSize)))

const pageNumbers = computed(() => {
  const total = totalPages.value
  const cur   = props.page
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)
  const pages = new Set([1, total, cur])
  if (cur > 1) pages.add(cur - 1)
  if (cur < total) pages.add(cur + 1)
  const sorted = [...pages].sort((a, b) => a - b)
  // Insert ellipsis markers
  const result = []
  for (let i = 0; i < sorted.length; i++) {
    if (i > 0 && sorted[i] - sorted[i - 1] > 1) result.push('…')
    result.push(sorted[i])
  }
  return result
})

function startConfirm(id) { confirmId.value = id }
function cancelConfirm()  { confirmId.value = null }
function confirmDelete(id) {
  confirmId.value = null
  emit('delete', id)
}

function fmtDate(iso) {
  const d = new Date(iso)
  return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(d)
}

function fmtAmount(n) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n)
}

// Generate a deterministic fallback color from a string
function fallbackColor(name = '') {
  const palette = ['#6366f1','#ec4899','#f59e0b','#10b981','#3b82f6','#8b5cf6','#e11d48','#0ea5e9']
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) & 0xffffffff
  return palette[Math.abs(h) % palette.length]
}
</script>

<template>
  <div class="expense-list-wrap">
    <!-- ── Loading skeleton ──────────────────────────────── -->
    <div v-if="loading" class="skeleton-wrap">
      <div v-for="n in 5" :key="n" class="skeleton-row">
        <span class="sk sk-date"  />
        <span class="sk sk-desc"  />
        <span class="sk sk-cat"   />
        <span class="sk sk-amt"   />
        <span class="sk sk-btns"  />
      </div>
    </div>

    <!-- ── Empty state ───────────────────────────────────── -->
    <div v-else-if="!expenses.length" class="empty-state">
      <div class="empty-icon">🧾</div>
      <p class="empty-title">No expenses found</p>
      <p class="empty-sub">Add your first expense or adjust the filters above.</p>
    </div>

    <!-- ── Table ─────────────────────────────────────────── -->
    <div v-else class="table-wrap">
      <table class="expense-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Description</th>
            <th>Category</th>
            <th class="th-right">Amount</th>
            <th class="th-center">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="exp in expenses" :key="exp.id" class="expense-row">
            <td class="td-date">{{ fmtDate(exp.date) }}</td>
            <td class="td-desc">{{ exp.description }}</td>
            <td class="td-cat">
              <span
                class="cat-dot"
                :style="{ background: exp.categoryColor || fallbackColor(exp.categoryName) }"
              />
              {{ exp.categoryName }}
            </td>
            <td class="td-amt">{{ fmtAmount(exp.amount) }}</td>
            <td class="td-actions">
              <!-- Normal action buttons -->
              <template v-if="confirmId !== exp.id">
                <button class="action-btn edit-btn" title="Edit" @click="$emit('edit', exp)">
                  <!-- Pencil icon -->
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="15" height="15">
                    <path d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z"/>
                  </svg>
                </button>
                <button class="action-btn delete-btn" title="Delete" @click="startConfirm(exp.id)">
                  <!-- Trash icon -->
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" width="15" height="15">
                    <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd"/>
                  </svg>
                </button>
              </template>

              <!-- Inline confirm delete -->
              <template v-else>
                <span class="confirm-label">Delete?</span>
                <button class="action-btn confirm-yes" @click="confirmDelete(exp.id)">Yes</button>
                <button class="action-btn confirm-no"  @click="cancelConfirm">No</button>
              </template>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ── Pagination ─────────────────────────────────────── -->
    <div v-if="!loading && totalPages > 1" class="pagination">
      <button
        class="page-btn"
        :disabled="page <= 1"
        @click="$emit('page-change', page - 1)"
      >‹ Prev</button>

      <template v-for="p in pageNumbers" :key="p">
        <span v-if="p === '…'" class="page-ellipsis">…</span>
        <button
          v-else
          class="page-btn"
          :class="{ active: p === page }"
          @click="$emit('page-change', p)"
        >{{ p }}</button>
      </template>

      <button
        class="page-btn"
        :disabled="page >= totalPages"
        @click="$emit('page-change', page + 1)"
      >Next ›</button>

      <span class="page-info">
        {{ (page - 1) * pageSize + 1 }}–{{ Math.min(page * pageSize, totalCount) }}
        of {{ totalCount }}
      </span>
    </div>
  </div>
</template>

<style scoped>
.expense-list-wrap {
  display: flex;
  flex-direction: column;
  gap: 0;
}

/* ── Skeleton ────────────────────────────────────────── */
.skeleton-wrap { display: flex; flex-direction: column; gap: 1px; }
.skeleton-row  {
  display: grid;
  grid-template-columns: 110px 1fr 150px 100px 90px;
  gap: 1rem;
  align-items: center;
  padding: 0.85rem 1rem;
  background: #fff;
  border-bottom: 1px solid #f3f4f6;
}
.sk {
  height: 0.85rem;
  border-radius: 0.3rem;
  background: linear-gradient(90deg, #f3f4f6 25%, #e9eaed 50%, #f3f4f6 75%);
  background-size: 200% 100%;
  animation: shimmer 1.4s infinite;
}
.sk-date { width: 80%; }
.sk-desc { width: 70%; }
.sk-cat  { width: 60%; }
.sk-amt  { width: 65%; margin-left: auto; }
.sk-btns { width: 50%; }
@keyframes shimmer { to { background-position: -200% 0; } }

/* ── Empty state ─────────────────────────────────────── */
.empty-state {
  text-align: center;
  padding: 4rem 2rem;
}
.empty-icon  { font-size: 2.5rem; margin-bottom: 0.75rem; }
.empty-title { font-size: 1rem; font-weight: 600; color: #374151; margin: 0 0 0.3rem; }
.empty-sub   { font-size: 0.875rem; color: #9ca3af; margin: 0; }

/* ── Table ───────────────────────────────────────────── */
.table-wrap { overflow-x: auto; }

.expense-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.expense-table th {
  padding: 0.65rem 1rem;
  text-align: left;
  font-size: 0.75rem;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  background: #f9fafb;
  border-bottom: 1px solid #e5e7eb;
  white-space: nowrap;
}
.th-right  { text-align: right; }
.th-center { text-align: center; }

.expense-row {
  border-bottom: 1px solid #f3f4f6;
  transition: background 0.1s;
}
.expense-row:last-child { border-bottom: none; }
.expense-row:hover { background: #fafafa; }

.expense-table td {
  padding: 0.75rem 1rem;
  vertical-align: middle;
  color: #111827;
}

.td-date { white-space: nowrap; color: #6b7280; font-size: 0.85rem; }
.td-desc { max-width: 260px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.td-cat  { display: flex; align-items: center; gap: 0.5rem; white-space: nowrap; }
.td-amt  { text-align: right; font-weight: 600; font-variant-numeric: tabular-nums; white-space: nowrap; }
.td-actions { text-align: center; white-space: nowrap; }

.cat-dot {
  display: inline-block;
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 50%;
  flex-shrink: 0;
}

/* ── Action buttons ─────────────────────────────────── */
.action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.3rem;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  background: transparent;
  line-height: 1;
}

.edit-btn   { color: #6b7280; }
.edit-btn:hover { background: #eef2ff; color: #4f46e5; }

.delete-btn { color: #6b7280; margin-left: 0.25rem; }
.delete-btn:hover { background: #fef2f2; color: #dc2626; }

.confirm-label { font-size: 0.8rem; color: #dc2626; font-weight: 500; margin-right: 0.4rem; }
.confirm-yes {
  font-size: 0.78rem;
  font-weight: 600;
  color: #fff;
  background: #dc2626;
  border-radius: 0.375rem;
  padding: 0.2rem 0.55rem;
  border: none;
  cursor: pointer;
}
.confirm-yes:hover { background: #b91c1c; }
.confirm-no {
  font-size: 0.78rem;
  font-weight: 600;
  color: #374151;
  background: #f3f4f6;
  border-radius: 0.375rem;
  padding: 0.2rem 0.55rem;
  border: none;
  cursor: pointer;
  margin-left: 0.25rem;
}
.confirm-no:hover { background: #e5e7eb; }

/* ── Pagination ──────────────────────────────────────── */
.pagination {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 1rem 1rem 0.5rem;
  flex-wrap: wrap;
}
.page-btn {
  min-width: 2.1rem;
  height: 2.1rem;
  padding: 0 0.5rem;
  border: 1.5px solid #e5e7eb;
  border-radius: 0.5rem;
  background: #fff;
  font-size: 0.85rem;
  font-weight: 500;
  color: #374151;
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s, color 0.12s;
}
.page-btn:hover:not(:disabled) { background: #eef2ff; border-color: #c7d2fe; color: #4f46e5; }
.page-btn.active { background: #4f46e5; border-color: #4f46e5; color: #fff; }
.page-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.page-ellipsis { font-size: 0.85rem; color: #9ca3af; padding: 0 0.15rem; }
.page-info { margin-left: auto; font-size: 0.8rem; color: #9ca3af; white-space: nowrap; }
</style>
