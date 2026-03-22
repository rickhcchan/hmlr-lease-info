<script setup lang="ts">
import { onUnmounted } from 'vue'
import LeaseSearch from './components/LeaseSearch.vue'
import LeaseResult from './components/LeaseResult.vue'
import SyncStatus from './components/SyncStatus.vue'
import { useLeaseQuery } from './composables/useLeaseQuery'

const { data, status, message, lastSyncAt, search, stopPolling } = useLeaseQuery()

onUnmounted(stopPolling)
</script>

<template>
  <div class="app">
    <h1>HMLR Lease Info</h1>
    <LeaseSearch @search="search" />

    <div class="result" v-if="status === 'loading'">
      <p>Loading...</p>
    </div>

    <LeaseResult v-else-if="status === 'found' && data" :lease="data" />

    <SyncStatus
      v-else-if="status === 'processing' || status === 'notFound' || status === 'error'"
      :status="status"
      :message="message"
      :last-sync-at="lastSyncAt"
    />
  </div>
</template>

<style scoped>
.app {
  max-width: 700px;
  margin: 2rem auto;
  padding: 0 1rem;
  font-family: system-ui, sans-serif;
}

h1 {
  margin-bottom: 1rem;
  color: #333;
}

.result {
  margin-top: 1rem;
  text-align: center;
  color: #888;
}
</style>
