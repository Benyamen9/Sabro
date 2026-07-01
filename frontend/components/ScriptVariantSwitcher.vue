<script setup lang="ts">
import type { ScriptVariant } from '~/composables/useScriptVariant'

const { t } = useI18n()
const { variant, set, available } = useScriptVariant()
const { persist } = useProfile()

const options = computed(() =>
  available.map(value => ({ value, label: t(`switcher.script.${value}`) })),
)

// The shared cookie updates instantly (and propagates to the other apps in this
// browser); mirror the choice to the signed-in user's profile for cross-device.
function onSelect(value: string) {
  set(value as ScriptVariant)
  persist()
}
</script>

<template>
  <!-- The trigger glyph renders in the active variant's font, so it previews the
       current script; each menu row shows the same glyph in its own script. -->
  <SelectMenu
    :model-value="variant"
    :options="options"
    :menu-label="t('switcher.scriptLabel')"
    @update:model-value="onSelect"
  >
    <template #trigger>
      <SyriacText text="ܐ" aria-hidden="true" class="!text-lg leading-none" />
    </template>
    <template #option="{ option }">
      <span class="flex-1">{{ option.label }}</span>
      <SyriacText
        text="ܐ"
        aria-hidden="true"
        :variant="(option.value as ScriptVariant)"
        class="!text-lg leading-none"
      />
    </template>
  </SelectMenu>
</template>
