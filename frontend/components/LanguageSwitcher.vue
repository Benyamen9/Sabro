<script setup lang="ts">
const { locale, locales, setLocale, t } = useI18n()
const { persist } = useProfile()

const localeOptions = computed(() =>
  (locales.value as Array<{ code: string, name?: string }>).map(l => ({
    code: l.code,
    label: l.name ?? l.code,
  })),
)

// v-model so the control reflects the active locale on load; a plain :value
// attribute leaves a <select> stuck on its first option after SSR hydration.
// i18n persists the locale cookie itself; mirror the choice to the profile.
const selected = computed<string>({
  get: () => locale.value,
  set: (value) => {
    setLocale(value as typeof locale.value).then(() => persist())
  },
})
</script>

<template>
  <label class="relative inline-flex items-center">
    <span class="sr-only">{{ t('switcher.languageLabel') }}</span>
    <select
      v-model="selected"
      :aria-label="t('switcher.languageLabel')"
      class="cursor-pointer appearance-none rounded-md border border-[var(--color-border)] bg-[var(--color-bg-elevated)] py-1.5 pl-3 pr-7 font-sans text-xs font-medium text-[var(--color-text-muted)] transition-colors hover:border-[var(--color-border-strong)] hover:text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent-faint)]"
    >
      <option
        v-for="option in localeOptions"
        :key="option.code"
        :value="option.code"
      >{{ option.label }}</option>
    </select>
    <span
      aria-hidden="true"
      class="pointer-events-none absolute right-2 text-[var(--color-text-faint)]"
    >▾</span>
  </label>
</template>
