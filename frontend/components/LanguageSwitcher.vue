<script setup lang="ts">
const { locale, locales, setLocale, t } = useI18n()

const localeOptions = computed(() =>
  (locales.value as Array<{ code: string, name?: string }>).map(l => ({
    code: l.code,
    label: l.name ?? l.code,
  })),
)

async function onChange(event: Event) {
  const target = event.target as HTMLSelectElement
  await setLocale(target.value as typeof locale.value)
}
</script>

<template>
  <label class="language-switcher">
    <span class="visually-hidden">{{ t('switcher.languageLabel') }}</span>
    <select
      :value="locale"
      :aria-label="t('switcher.languageLabel')"
      @change="onChange"
    >
      <option
        v-for="option in localeOptions"
        :key="option.code"
        :value="option.code"
      >{{ option.label }}</option>
    </select>
  </label>
</template>

<style scoped>
.language-switcher select {
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
