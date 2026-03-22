<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{ search: [titleNumber: string] }>()
const titleNumber = ref('')

function onSubmit() {
  const trimmed = titleNumber.value.trim()
  if (trimmed) {
    emit('search', trimmed)
  }
}
</script>

<template>
  <form @submit.prevent="onSubmit" class="search-form">
    <input
      v-model="titleNumber"
      type="text"
      placeholder="Enter title number (e.g. EGL557357)"
      aria-label="Title number"
    />
    <button type="submit" :disabled="!titleNumber.trim()">Search</button>
  </form>
</template>

<style scoped>
.search-form {
  display: flex;
  gap: 0.5rem;
}

input {
  flex: 1;
  padding: 0.5rem;
  font-size: 1rem;
  border: 1px solid #ccc;
  border-radius: 4px;
}

button {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  background: #4a90d9;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
