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
  <label class="script-variant-switcher">
    <span class="visually-hidden">{{ t('switcher.scriptLabel') }}</span>
    <select
      :value="variant"
      :aria-label="t('switcher.scriptLabel')"
      @change="onChange"
    >
      <option
        v-for="value in available"
        :key="value"
        :value="value"
      >{{ t(`switcher.script.${value}`) }}</option>
    </select>
  </label>
</template>

<style scoped>
.script-variant-switcher select {
  padding: 0.25rem 0.5rem;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>
