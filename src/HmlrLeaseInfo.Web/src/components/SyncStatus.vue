<script setup lang="ts">
defineProps<{
  status: 'processing' | 'notFound' | 'error'
  message: string | null
  lastSyncAt: string | null
}>()
</script>

<template>
  <div class="status-card" :class="status">
    <div v-if="status === 'processing'" class="processing">
      <span class="spinner"></span>
      <p>{{ message ?? 'Syncing data...' }}</p>
    </div>
    <div v-else-if="status === 'notFound'" class="not-found">
      <p>{{ message ?? 'Not found' }}</p>
      <p v-if="lastSyncAt" class="sync-time">Last synced: {{ new Date(lastSyncAt).toLocaleString() }}</p>
    </div>
    <div v-else class="error">
      <p>{{ message ?? 'An error occurred' }}</p>
    </div>
  </div>
</template>

<style scoped>
.status-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 1.5rem;
  text-align: center;
}

.processing {
  color: #4a90d9;
}

.spinner {
  display: inline-block;
  width: 24px;
  height: 24px;
  border: 3px solid #4a90d9;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.not-found {
  color: #888;
}

.sync-time {
  font-size: 0.85rem;
  color: #aaa;
}

.error {
  color: #d9534f;
}
</style>
