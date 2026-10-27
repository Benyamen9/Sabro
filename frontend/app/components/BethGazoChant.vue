<script setup lang="ts">
import type { BethGazoBookChantDto } from '~/types/api'

/**
 * One chant of a melody in the book: a play button, the mode it is sung in (or
 * nothing, in a section without modes), its variant, and its incipit once the
 * Owner has read it from the book.
 */
const props = defineProps<{
  chant: BethGazoBookChantDto
  melody: string
  modeName: string | null
  modeLabel: string | null
  playing: boolean
}>()

const emit = defineEmits<{ toggle: [] }>()
const { t } = useI18n()

const variant = computed(() => props.chant.variantKind === 'None'
  ? null
  : t(`bethGazo.variant.${props.chant.variantKind}`, { number: props.chant.variantNumber ?? '' }).trim())
const incipit = computed(() => props.chant.syriacIncipitVocalized ?? props.chant.syriacIncipit)
</script>

<template>
  <button
    type="button"
    class="flex w-full min-w-0 items-center gap-2.5 rounded-xl border px-2.5 py-2 text-left transition-colors"
    :class="playing
      ? 'border-[var(--color-nahlo)] bg-[var(--color-nahlo-faint)]'
      : 'border-[var(--color-border)] bg-[var(--color-bg-elevated)] hover:border-[var(--color-border-strong)]'"
    :aria-label="t(playing ? 'bethGazo.pauseChant' : 'bethGazo.playChant', { chant: [melody, modeName, variant].filter(Boolean).join(', ') })"
    @click="emit('toggle')"
  >
    <span aria-hidden="true" class="grid size-8 shrink-0 place-items-center rounded-full bg-[var(--color-nahlo)] text-[var(--color-bg)]">
      <svg v-if="playing" viewBox="0 0 12 14" class="size-2.5"><path d="M1 0h3v14H1zM8 0h3v14H8z" fill="currentColor" /></svg>
      <svg v-else viewBox="0 0 12 14" class="ml-0.5 size-2.5"><path d="M0 0l12 7-12 7z" fill="currentColor" /></svg>
    </span>
    <span class="min-w-0 flex-1">
      <span class="block truncate font-sans text-sm font-semibold text-[var(--color-text)]">
        <template v-if="modeName">{{ modeName }}<span v-if="modeLabel" class="ml-1.5 font-normal text-[var(--color-text-muted)]">{{ modeLabel }}</span></template>
        <template v-else>{{ melody }}</template>
        <span v-if="variant" class="ml-1.5 font-normal text-[var(--color-text-muted)]">· {{ variant }}</span>
      </span>
      <span v-if="incipit" dir="rtl" lang="syc" class="block truncate text-left font-syriac text-[15px] text-[var(--color-text-muted)]">{{ incipit }}</span>
      <span v-else class="block truncate font-serif text-[12.5px] italic text-[var(--color-text-faint)]">{{ t('bethGazo.noIncipit') }}</span>
    </span>
  </button>
</template>
