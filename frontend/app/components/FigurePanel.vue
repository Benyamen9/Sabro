<script setup lang="ts">
import type { GalleryFigure } from '~/utils/gallery'

/** The figure's page beside the collection, on a computer. */
defineProps<{ figure: GalleryFigure }>()
defineEmits<{ (e: 'close'): void }>()

const { t } = useI18n()
</script>

<template>
  <article
    class="rounded-[18px] border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4 shadow-[var(--shadow-soft)]"
    :aria-label="figure.name"
  >
    <div class="flex items-start justify-between gap-3">
      <div
        class="w-full max-w-[220px] overflow-hidden rounded-2xl"
        :class="figure.tier === 'found' ? 'border-2 border-[var(--color-shmo)]' : 'border border-[var(--color-border)]'"
      >
        <FigureArt :figure="figure" :tier="figure.tier" :illuminated="figure.illuminated" />
      </div>
      <button
        type="button"
        class="grid size-8 shrink-0 place-items-center rounded-full text-lg text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)] hover:text-[var(--color-text)]"
        :aria-label="t('bethSalme.detail.close')"
        @click="$emit('close')"
      >×</button>
    </div>
    <p class="mt-4 font-sans text-[11px] font-semibold uppercase tracking-[0.12em]" :class="figure.tier === 'found' ? 'text-[var(--color-shmo)]' : 'text-[var(--color-text-muted)]'">
      {{ t('bethSalme.card.number', { no: figure.no }) }} · {{ t(`bethSalme.status.${figure.tier}`) }}
      <template v-if="figure.tier === 'found' && figure.illuminated"> · ✦ {{ t('bethSalme.status.illuminated') }}</template>
    </p>
    <h2 class="mt-1 font-serif text-[26px] font-semibold leading-tight text-[var(--color-text)]">{{ figure.name }}</h2>
    <p
      v-if="figure.syriacName"
      dir="rtl"
      lang="syc"
      class="mt-1 text-left font-syriac text-[21px] text-[var(--color-shmo)]"
    >{{ figure.syriacName }}</p>
    <FigureDetails class="mt-3" :figure="figure" />
    <p v-if="figure.tier !== 'found'" class="mt-4 rounded-xl bg-[var(--color-bg-subtle)] px-3 py-2.5 font-sans text-[13px] leading-snug text-[var(--color-text-muted)]">
      {{ t(figure.tier === 'met' ? 'bethSalme.detail.findToColour' : 'bethSalme.detail.lockedHint', { name: figure.name }) }}
    </p>
  </article>
</template>
