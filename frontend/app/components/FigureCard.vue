<script setup lang="ts">
import type { GalleryFigure } from '~/utils/gallery'
import { formatEra } from '~/utils/historicalFigures'

/** One card in the Beth Ṣalme grid. A button: it opens the figure. */
const props = withDefaults(defineProps<{ figure: GalleryFigure, selected?: boolean }>(), { selected: false })
defineEmits<{ (e: 'open', figure: GalleryFigure): void }>()

const { t } = useI18n()

const caption = computed(() =>
  props.figure.tier === 'locked' ? t('bethSalme.card.notMetYet') : formatEra(props.figure.era, t))

const status = computed(() => {
  if (props.figure.tier === 'locked') return t('bethSalme.status.locked')
  if (props.figure.tier === 'met') return t('bethSalme.status.met')
  return props.figure.illuminated ? t('bethSalme.status.illuminated') : t('bethSalme.status.found')
})
</script>

<template>
  <button
    type="button"
    class="figure-card group relative flex min-w-0 flex-col overflow-hidden rounded-[13px] border text-left shadow-[var(--shadow-soft)] transition-[transform,box-shadow] duration-200 hover:-translate-y-1 hover:-rotate-[0.6deg] hover:shadow-[0_10px_24px_rgb(0_0_0/0.14)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--color-shmo)] motion-reduce:transition-none motion-reduce:hover:translate-y-0 motion-reduce:hover:rotate-0"
    :class="[
      figure.tier === 'found' ? 'border-2 border-[var(--color-shmo)] bg-[var(--color-bg-elevated)]' : '',
      figure.tier === 'met' ? 'border-[var(--color-border)] bg-[var(--color-bg-elevated)]' : '',
      figure.tier === 'locked' ? 'border-[var(--figure-locked-bg)] bg-[var(--figure-locked-bg)]' : '',
      selected ? 'outline-3 outline-offset-2 outline-[color-mix(in_oklab,var(--color-shmo)_55%,transparent)]' : '',
    ]"
    :aria-label="`${figure.name}, ${status}`"
    :aria-pressed="selected"
    @click="$emit('open', figure)"
  >
    <FigureArt :figure="figure" :tier="figure.tier" :illuminated="figure.illuminated" />
    <span class="block min-h-[46px] px-2 pt-1.5 pb-2">
      <span
        class="line-clamp-2 font-serif text-[12.5px] font-semibold leading-tight"
        :class="figure.tier === 'locked' ? 'text-[var(--figure-locked-text)]' : 'text-[var(--color-text)]'"
      >{{ figure.name }}</span>
      <span
        class="mt-0.5 block font-sans text-[10.5px]"
        :class="figure.tier === 'locked' ? 'text-[var(--figure-locked-ink)]' : 'text-[var(--color-text-muted)]'"
      >{{ caption }}</span>
    </span>
    <span
      v-if="figure.tier === 'found'"
      aria-hidden="true"
      class="absolute top-1.5 right-1.5 grid size-5 place-items-center rounded-full font-sans text-[11px] text-white shadow-[0_0_0_2px_var(--color-bg-elevated)]"
      :class="figure.illuminated ? 'illuminated-badge' : 'bg-[var(--color-shmo)]'"
    >{{ figure.illuminated ? '✦' : '✓' }}</span>
  </button>
</template>

<style scoped>
.illuminated-badge {
  background: conic-gradient(from 30deg, #ff4fb0, #ffd400, #29e3c1, #3a8bff, #ff4fb0);
  text-shadow: 0 0 2px rgb(0 0 0 / 0.5);
}
</style>
