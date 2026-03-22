import { ref, type Ref } from 'vue'
import type { ParsedNoticeOfLease, LeaseResponse, QueryStatus } from '../types/lease'

const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000'
const POLL_INTERVAL = 2000

export function useLeaseQuery() {
  const data: Ref<ParsedNoticeOfLease | null> = ref(null)
  const status: Ref<QueryStatus> = ref('idle')
  const message: Ref<string | null> = ref(null)
  const lastSyncAt: Ref<string | null> = ref(null)

  let pollTimer: ReturnType<typeof setTimeout> | null = null

  function stopPolling() {
    if (pollTimer) {
      clearTimeout(pollTimer)
      pollTimer = null
    }
  }

  async function search(titleNumber: string) {
    stopPolling()
    data.value = null
    message.value = null
    lastSyncAt.value = null
    status.value = 'loading'

    await fetchLease(titleNumber)
  }

  async function fetchLease(titleNumber: string) {
    try {
      const response = await fetch(`${API_BASE}/${titleNumber}`)

      if (response.status === 200) {
        data.value = await response.json()
        status.value = 'found'
      } else if (response.status === 202) {
        const body: LeaseResponse = await response.json()
        message.value = body.message
        status.value = 'processing'
        pollTimer = setTimeout(() => fetchLease(titleNumber), POLL_INTERVAL)
      } else if (response.status === 404) {
        const body: LeaseResponse = await response.json()
        message.value = body.message
        lastSyncAt.value = body.lastSyncAt
        status.value = 'notFound'
      } else {
        message.value = `Unexpected response: ${response.status}`
        status.value = 'error'
      }
    } catch (err) {
      message.value = err instanceof Error ? err.message : 'Network error'
      status.value = 'error'
    }
  }

  return { data, status, message, lastSyncAt, search, stopPolling }
}
