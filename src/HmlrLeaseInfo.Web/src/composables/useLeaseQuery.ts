import { ref, type Ref } from 'vue'
import type { ParsedNoticeOfLease, LeaseResponse, QueryStatus } from '../types/lease'

const API_BASE = import.meta.env.VITE_API_BASE ?? '/api'
const API_USERNAME = import.meta.env.VITE_API_USERNAME ?? 'username'
const API_PASSWORD = import.meta.env.VITE_API_PASSWORD ?? 'password'
const POLL_INTERVAL = 2000
const MAX_POLL_ATTEMPTS = 15

export function useLeaseQuery() {
  const data: Ref<ParsedNoticeOfLease | null> = ref(null)
  const status: Ref<QueryStatus> = ref('idle')
  const message: Ref<string | null> = ref(null)
  const lastSyncAt: Ref<string | null> = ref(null)

  let pollTimer: ReturnType<typeof setTimeout> | null = null
  let pollCount = 0
  let abortController: AbortController | null = null

  function stopPolling() {
    if (pollTimer) {
      clearTimeout(pollTimer)
      pollTimer = null
    }
    abortController?.abort()
    abortController = null
  }

  async function search(titleNumber: string) {
    stopPolling()
    data.value = null
    message.value = null
    lastSyncAt.value = null
    status.value = 'loading'
    pollCount = 0

    abortController = new AbortController()
    await fetchLease(titleNumber, abortController.signal)
  }

  async function fetchLease(titleNumber: string, signal: AbortSignal) {
    try {
      const response = await fetch(
        `${API_BASE}/${encodeURIComponent(titleNumber)}`,
        {
          signal,
          headers: {
            Authorization: `Basic ${btoa(`${API_USERNAME}:${API_PASSWORD}`)}`,
          },
        },
      )

      if (response.status === 200) {
        data.value = await response.json()
        status.value = 'found'
      } else if (response.status === 202) {
        const body: LeaseResponse = await response.json()
        message.value = body.message
        status.value = 'processing'
        pollCount++
        if (pollCount < MAX_POLL_ATTEMPTS) {
          pollTimer = setTimeout(() => fetchLease(titleNumber, signal), POLL_INTERVAL)
        } else {
          message.value = 'Sync is taking longer than expected. Please try again later.'
          status.value = 'error'
        }
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
      if (err instanceof DOMException && err.name === 'AbortError') return
      message.value = err instanceof Error ? err.message : 'Network error'
      status.value = 'error'
    }
  }

  return { data, status, message, lastSyncAt, search, stopPolling }
}
