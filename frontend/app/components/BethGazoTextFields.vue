<script setup lang="ts">
import { BETH_GAZO_LANGUAGES } from '~/utils/bethGazo'

/**
 * The Syriac name and the one line per language a section or a mode carries
 * for the hub's Beth Gazo (Owner, 2026-10-25). All optional: an empty box is
 * simply not shown on the page.
 */
const props = defineProps<{ hint: string }>()
const syriacName = defineModel<string>('syriacName', { required: true })
const lines = defineModel<Record<string, string>>('lines', { required: true })

const { t } = useI18n()
const id = useId()

/** Matches BethGazoDescription.MaxTextLength on the backend. */
const maxLength = 300

const fieldClass
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none'
</script>

<template>
  <fieldset class="flex flex-col gap-2">
    <legend class="font-sans text-xs font-medium text-[var(--color-text-faint)]">{{ t('admin.chantSections.bookText') }}</legend>
    <p class="font-sans text-xs text-[var(--color-text-faint)]">{{ props.hint }}</p>
    <label :for="`${id}-syriac`" class="font-sans text-xs font-medium text-[var(--color-text-muted)]">
      {{ t('admin.chantSections.syriacName') }}
    </label>
    <input
      :id="`${id}-syriac`"
      v-model="syriacName"
      dir="rtl"
      lang="syc"
      maxlength="64"
      :class="[fieldClass, 'max-w-xs text-right text-lg']"
    >
    <div v-for="language in BETH_GAZO_LANGUAGES" :key="language" class="flex flex-col gap-1">
      <label :for="`${id}-${language}`" class="font-sans text-[11px] font-medium uppercase tracking-wider text-[var(--color-text-muted)]">
        {{ t(`admin.historicalFigures.description.${language}`) }}
      </label>
      <textarea
        :id="`${id}-${language}`"
        v-model="lines[language]"
        rows="2"
        :maxlength="maxLength"
        :class="[fieldClass, 'resize-y']"
      />
    </div>
  </fieldset>
</template>
