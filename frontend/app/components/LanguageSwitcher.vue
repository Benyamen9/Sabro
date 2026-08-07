<script setup lang="ts">
const { locale, locales, setLocale, t } = useI18n()
const { persist } = useProfile()

const options = computed(() =>
  (locales.value as Array<{ code: string, name?: string }>).map(l => ({
    value: l.code,
    label: l.name ?? l.code,
  })),
)

const currentCode = computed(() => locale.value.toUpperCase())

// i18n persists the locale cookie itself; mirror the choice to the profile.
function onSelect(value: string) {
  setLocale(value as typeof locale.value).then(() => persist())
}
</script>

<template>
  <SelectMenu
    :model-value="locale"
    :options="options"
    :menu-label="t('switcher.languageLabel')"
    @update:model-value="onSelect"
  >
    <template #trigger>
      <span class="font-sans text-xs font-semibold">{{ currentCode }}</span>
    </template>
    <template #option="{ option }">
      <span class="inline-flex min-w-[1.9rem] justify-center rounded border border-[var(--color-border-strong)] px-1 py-0.5 text-[0.6rem] font-bold uppercase tracking-wide text-[var(--color-text-faint)]">{{ option.value.toUpperCase() }}</span>
      <span class="flex-1">{{ option.label }}</span>
    </template>
  </SelectMenu>
</template>
