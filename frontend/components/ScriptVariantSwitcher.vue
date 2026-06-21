<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

const { t } = useI18n()
const { variant, set, available } = useScriptVariant()
const { persist } = useProfile()

// Bind the native <select> with v-model so it reflects the persisted variant on
// load — a plain :value attribute can't drive a <select>'s selection across SSR
// hydration, leaving the control stuck on its first option. The setter updates
// the cookie and mirrors the choice to the profile when signed in.
const selected = computed<ScriptVariant>({
  get: () => variant.value,
  set: (value) => {
    set(value)
    persist()
  },
})
</script>

<template>
  <label class="relative inline-flex items-center">
    <span class="sr-only">{{ t('switcher.scriptLabel') }}</span>
    <select
      v-model="selected"
      :aria-label="t('switcher.scriptLabel')"
      class="cursor-pointer appearance-none rounded-md border border-[var(--color-border)] bg-[var(--color-bg-elevated)] py-1.5 pl-3 pr-7 font-sans text-xs font-medium text-[var(--color-text-muted)] transition-colors hover:border-[var(--color-border-strong)] hover:text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent-faint)]"
    >
      <option
        v-for="value in available"
        :key="value"
        :value="value"
      >{{ t(`switcher.script.${value}`) }}</option>
    </select>
    <span
      aria-hidden="true"
      class="pointer-events-none absolute right-2 text-[var(--color-text-faint)]"
    >▾</span>
  </label>
</template>
