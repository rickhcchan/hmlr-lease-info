<script setup lang="ts">
import type { ParsedNoticeOfLease } from '../types/lease'

defineProps<{ lease: ParsedNoticeOfLease }>()
</script>

<template>
  <div class="lease-card">
    <h2>{{ lease.lesseesTitle }}</h2>
    <table>
      <tr>
        <th>Entry Number</th>
        <td>{{ lease.entryNumber }}</td>
      </tr>
      <tr v-if="lease.entryDate">
        <th>Entry Date</th>
        <td>{{ lease.entryDate }}</td>
      </tr>
      <tr>
        <th>Registration Date &amp; Plan Ref</th>
        <td>{{ lease.registrationDateAndPlanRef }}</td>
      </tr>
      <tr>
        <th>Property Description</th>
        <td>{{ lease.propertyDescription }}</td>
      </tr>
      <tr>
        <th>Date of Lease &amp; Term</th>
        <td>{{ lease.dateOfLeaseAndTerm }}</td>
      </tr>
    </table>
    <div v-if="lease.notes.length" class="notes">
      <h3>Notes</h3>
      <ul>
        <li v-for="(note, i) in lease.notes" :key="i">{{ note }}</li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.lease-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 1.5rem;
  background: #fafafa;
}

h2 {
  margin: 0 0 1rem;
  color: #333;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th {
  text-align: left;
  padding: 0.4rem 1rem 0.4rem 0;
  color: #666;
  width: 200px;
}

td {
  padding: 0.4rem 0;
}

.notes {
  margin-top: 1rem;
  border-top: 1px solid #eee;
  padding-top: 0.5rem;
}

.notes h3 {
  margin: 0 0 0.5rem;
  font-size: 0.9rem;
  color: #666;
}

.notes ul {
  margin: 0;
  padding-left: 1.2rem;
}

.notes li {
  margin-bottom: 0.25rem;
}
</style>
