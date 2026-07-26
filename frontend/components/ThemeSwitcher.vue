<script setup lang="ts">
import type { Theme } from '~/composables/useTheme'

const { t } = useI18n()
const { theme, set, available } = useTheme()

const options = computed(() =>
  available.map(value => ({ value, label: t(`switcher.theme.${value}`) })),
)
</script>

<template>
  <!-- The trigger icon previews the active choice: sun/moon for a forced
       light/dark palette, a half-filled circle for "system". -->
  <SelectMenu
    :model-value="theme"
    :options="options"
    :menu-label="t('switcher.themeLabel')"
    @update:model-value="(value) => set(value as Theme)"
  >
    <template #trigger>
      <svg
        v-if="theme === 'light'"
        class="size-4"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.75"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="4.5" />
        <path d="M12 3v2M12 19v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M3 12h2M19 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4" />
      </svg>
      <svg
        v-else-if="theme === 'dark'"
        class="size-4"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.75"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
      >
        <path d="M20 14.5A8.5 8.5 0 0 1 9.5 4a7 7 0 1 0 10.5 10.5Z" />
      </svg>
      <svg
        v-else
        class="size-4"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.75"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="8.5" />
        <path d="M12 3.5a8.5 8.5 0 0 0 0 17Z" fill="currentColor" stroke="none" />
      </svg>
    </template>
    <template #option="{ option }">
      <span class="flex-1">{{ option.label }}</span>
    </template>
  </SelectMenu>
</template>
