<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

const { t } = useI18n()
const { variant, set, available } = useScriptVariant()

function onChange(event: Event) {
  const target = event.target as HTMLSelectElement
  set(target.value as ScriptVariant)
}
</script>

<template>
  <label class="relative inline-flex items-center">
    <span class="sr-only">{{ t('switcher.scriptLabel') }}</span>
    <select
      :value="variant"
      :aria-label="t('switcher.scriptLabel')"
      class="cursor-pointer appearance-none rounded-md border border-[var(--color-border)] bg-[var(--color-bg-elevated)] py-1.5 pl-3 pr-7 font-sans text-xs font-medium text-[var(--color-text-muted)] transition-colors hover:border-[var(--color-border-strong)] hover:text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent-faint)]"
      @change="onChange"
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
