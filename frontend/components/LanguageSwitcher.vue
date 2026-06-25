<script setup lang="ts">
const { locale, locales, setLocale, t } = useI18n()
const { persist } = useProfile()

const localeOptions = computed(() =>
  (locales.value as Array<{ code: string, name?: string }>).map(l => ({
    code: l.code,
    label: l.name ?? l.code,
  })),
)

const currentCode = computed(() => locale.value.toUpperCase())

// v-model so the control reflects the active locale on load; a plain :value
// attribute leaves a <select> stuck on its first option after SSR hydration.
// i18n persists the locale cookie itself; mirror the choice to the profile.
// The transparent <select> overlays a compact code facade (EN / FR / NL) while
// the dropdown keeps the full language names.
const selected = computed<string>({
  get: () => locale.value,
  set: (value) => {
    setLocale(value as typeof locale.value).then(() => persist())
  },
})
</script>

<template>
  <label
    class="group relative inline-flex items-center rounded-md focus-within:ring-2 focus-within:ring-[var(--color-accent-faint)]"
  >
    <span
      aria-hidden="true"
      class="flex h-8 items-center justify-center rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2.5 font-sans text-xs font-semibold text-[var(--color-text-muted)] transition-colors group-hover:text-[var(--color-text)]"
    >{{ currentCode }}</span>
    <select
      v-model="selected"
      :aria-label="t('switcher.languageLabel')"
      :title="t('switcher.languageLabel')"
      class="absolute inset-0 size-full cursor-pointer appearance-none opacity-0"
    >
      <option
        v-for="option in localeOptions"
        :key="option.code"
        :value="option.code"
      >{{ option.label }}</option>
    </select>
  </label>
</template>
