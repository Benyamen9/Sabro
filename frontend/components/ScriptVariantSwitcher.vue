<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

const { t } = useI18n()
const { variant, set, available } = useScriptVariant()
const { persist } = useProfile()

// Bind the native <select> with v-model so it reflects the persisted variant on
// load — a plain :value attribute can't drive a <select>'s selection across SSR
// hydration. The select sits transparent over a styled icon facade: the facade
// gives the compact icon-button look, the real <select> underneath keeps the
// native dropdown, keyboard support, and accessibility.
const selected = computed<ScriptVariant>({
  get: () => variant.value,
  set: (value) => {
    set(value)
    persist()
  },
})
</script>

<template>
  <label
    class="group relative inline-flex items-center rounded-md focus-within:ring-2 focus-within:ring-[var(--color-accent-faint)]"
  >
    <span
      aria-hidden="true"
      class="flex h-8 items-center justify-center rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2.5 font-syriac text-lg leading-none text-[var(--color-text-muted)] transition-colors group-hover:text-[var(--color-text)]"
    >ܐ</span>
    <select
      v-model="selected"
      :aria-label="t('switcher.scriptLabel')"
      :title="t('switcher.scriptLabel')"
      class="absolute inset-0 size-full cursor-pointer appearance-none opacity-0"
    >
      <option
        v-for="value in available"
        :key="value"
        :value="value"
      >{{ t(`switcher.script.${value}`) }}</option>
    </select>
  </label>
</template>
