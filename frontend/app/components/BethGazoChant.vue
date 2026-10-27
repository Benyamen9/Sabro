<script setup lang="ts">
import type { BethGazoBookChantDto } from '~/types/api'

/**
 * One chant as a line in an unfolded melody: a play button, the mode it is sung
 * in (none in a section without modes), its shuḥlofo or ḥrino, and its incipit
 * once the Owner has read it from the book. In a section without modes the
 * incipit is what tells one chant from another.
 */
const props = defineProps<{
  chant: BethGazoBookChantDto
  melody: string
  modeName: string | null
  modeLabel: string | null
  playing: boolean
  current: boolean
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
    class="flex w-full min-w-0 items-center gap-2.5 rounded-lg px-1.5 py-1 text-left"
    :class="current ? 'bg-[var(--color-nahlo-faint)]' : 'hover:bg-[var(--color-bg-subtle)]'"
    :aria-label="t(playing ? 'bethGazo.pauseChant' : 'bethGazo.playChant', { chant: [melody, modeName, variant].filter(Boolean).join(', ') })"
    @click="emit('toggle')"
  >
    <span aria-hidden="true" class="grid size-7 shrink-0 place-items-center rounded-full bg-[var(--color-nahlo)] text-[var(--color-bg)]">
      <svg v-if="playing" viewBox="0 0 12 14" class="size-2.5"><path d="M1 0h3v14H1zM8 0h3v14H8z" fill="currentColor" /></svg>
      <svg v-else viewBox="0 0 12 14" class="ml-0.5 size-2.5"><path d="M0 0l12 7-12 7z" fill="currentColor" /></svg>
    </span>
    <span v-if="modeName || variant" class="w-32 shrink-0 font-sans text-[13.5px] font-semibold text-[var(--color-text)]">
      <template v-if="modeName"><span class="inline-block w-5 tabular-nums text-[var(--color-nahlo)]">{{ modeLabel }}</span>{{ modeName }}</template>
      <span v-if="variant" class="block text-xs font-normal text-[var(--color-text-muted)]" :class="modeName ? 'ml-5' : ''">{{ variant }}</span>
    </span>
    <span v-if="incipit" dir="rtl" lang="syc" class="min-w-0 flex-1 truncate text-left font-syriac text-base text-[var(--color-text-muted)]">{{ incipit }}</span>
    <span v-else class="min-w-0 flex-1 truncate font-serif text-[13px] italic text-[var(--color-text-faint)]">{{ t('bethGazo.noIncipit') }}</span>
  </button>
</template>
