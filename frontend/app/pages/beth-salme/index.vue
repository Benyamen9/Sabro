<script setup lang="ts">
import { matchesGallery, type GalleryFigure, type GalleryFilter } from '~/utils/gallery'

/**
 * Beth Ṣalme (ܒܝܬ ܨܠܡ̈ܐ, "house of images"): the prosopography of Shmo's roster
 * as a card collection (Owner, 2026-10-25). A figure is met when the player
 * guesses it in Shmo and found when they solve it as the day's answer; finding
 * it colours the card in and reveals its story. Cards run A–Z and are numbered
 * through time. On a computer the figure's page sits beside the collection; on a
 * phone the card lifts out over it.
 */
const { t } = useI18n()
const config = useRuntimeConfig()
const { signIn } = useAuth()
const { figures, pending, rosterFailed, collectionFailed, signedIn, canSignIn } = useFigureGallery()

useSeoMeta({
  title: () => t('seo.bethSalme.title'),
  description: () => t('seo.bethSalme.description'),
  ogTitle: () => t('seo.bethSalme.title'),
  ogDescription: () => t('seo.bethSalme.description'),
})

const filter = ref<GalleryFilter>('all')
const query = ref('')
const filters: GalleryFilter[] = ['all', 'found', 'illuminated', 'met', 'locked']

const shown = computed(() => figures.value.filter(f => matchesGallery(f, filter.value, query.value)))

/** A–Z with a row for each first letter. */
const rows = computed(() => {
  const out: ({ letter: string } | { figure: GalleryFigure })[] = []
  let last = ''
  for (const figure of shown.value) {
    const letter = figure.name.charAt(0).toLocaleUpperCase()
    if (letter !== last) {
      out.push({ letter })
      last = letter
    }
    out.push({ figure })
  }
  return out
})

const counts = computed(() => {
  const all = figures.value
  const found = all.filter(f => f.tier === 'found').length
  const met = all.filter(f => f.tier === 'met').length
  return {
    total: all.length,
    found,
    met,
    locked: all.length - found - met,
    illuminated: all.filter(f => f.illuminated).length,
  }
})

/**
 * The open figure. On a computer it shows in the side column and stays open
 * while browsing; on a phone it lifts out over the grid. Decided at the moment of
 * opening, from the same breakpoint the side column appears at.
 */
const selectedId = ref<string | null>(null)
const lifted = ref(false)
const selected = computed(() => figures.value.find(f => f.id === selectedId.value) ?? null)

function open(figure: GalleryFigure) {
  lifted.value = !window.matchMedia('(min-width: 1024px)').matches
  selectedId.value = figure.id
}

function close() {
  selectedId.value = null
}
</script>

<template>
  <section class="mx-auto max-w-6xl" :style="{ '--color-accent': 'var(--color-shmo)', '--color-accent-faint': 'var(--color-shmo-faint)' }">
    <header class="mb-6 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-shmo)]">
        {{ t('bethSalme.title') }}
        <span dir="rtl" lang="syc" class="ml-1 font-syriac text-[15px] normal-case tracking-normal">ܒܝܬ ܨܠܡ̈ܐ</span>
      </p>
      <h1 class="mt-3 font-serif text-4xl font-semibold tracking-[-0.02em] text-balance sm:text-[2.75rem]">{{ t('bethSalme.heading') }}</h1>
      <p class="mt-3 max-w-2xl font-serif text-[17px] text-[var(--color-text-muted)]">
        {{ t('bethSalme.lede', { count: counts.total || 289 }) }}
      </p>
      <a
        :href="config.public.shmoUrl"
        class="mt-4 inline-flex items-center gap-2 rounded-[10px] bg-[var(--color-shmo)] px-3.5 py-2 font-sans text-[13.5px] font-semibold text-white no-underline hover:bg-[var(--color-shmo-dark)]"
      ><span aria-hidden="true" class="font-syriac text-[15px] leading-none">ܫ</span>{{ t('bethSalme.play') }}</a>
    </header>

    <div
      v-if="!signedIn && canSignIn"
      class="mb-5 flex flex-wrap items-center gap-3 rounded-2xl border border-[color-mix(in_oklab,var(--color-shmo)_25%,var(--color-border))] bg-[var(--color-shmo-faint)] px-4 py-3"
    >
      <p class="min-w-0 flex-1 font-serif text-[15px] text-[var(--color-text)]">{{ t('bethSalme.signInLine') }}</p>
      <button
        type="button"
        class="shrink-0 rounded-[10px] bg-[var(--color-shmo)] px-3 py-2 font-sans text-[13px] font-semibold text-white hover:bg-[var(--color-shmo-dark)]"
        @click="signIn()"
      >{{ t('bethSalme.signIn') }}</button>
    </div>

    <div v-if="signedIn" class="mb-5" aria-live="polite">
      <div class="flex h-2 overflow-hidden rounded-full border border-[var(--color-border)] bg-[var(--color-bg-subtle)]" role="img" :aria-label="t('bethSalme.progress', counts)">
        <span class="h-full bg-[var(--color-shmo)]" :style="{ width: `${(counts.found / (counts.total || 1)) * 100}%` }" />
        <span class="h-full bg-[color-mix(in_oklab,var(--color-shmo)_30%,var(--color-bg-elevated))]" :style="{ width: `${(counts.met / (counts.total || 1)) * 100}%` }" />
      </div>
      <p class="mt-2 flex flex-wrap gap-x-4 gap-y-1 font-sans text-[12.5px] text-[var(--color-text-muted)]">
        <span><span aria-hidden="true" class="mr-1.5 inline-block size-2.5 rounded-[3px] bg-[var(--color-shmo)]" />{{ t('bethSalme.counts.found', { count: counts.found }) }}</span>
        <span><span aria-hidden="true" class="mr-1.5 inline-block size-2.5 rounded-[3px] bg-[color-mix(in_oklab,var(--color-shmo)_30%,var(--color-bg-elevated))]" />{{ t('bethSalme.counts.met', { count: counts.met }) }}</span>
        <span><span aria-hidden="true" class="mr-1.5 inline-block size-2.5 rounded-[3px] bg-[var(--figure-locked-mid)]" />{{ t('bethSalme.counts.locked', { count: counts.locked }) }}</span>
        <span class="illuminated-count font-semibold">✦ {{ t('bethSalme.counts.illuminated', { count: counts.illuminated }) }}</span>
      </p>
      <p v-if="collectionFailed" role="alert" class="mt-2 font-sans text-[13px] text-[var(--color-text-muted)]">{{ t('bethSalme.collectionFailed') }}</p>
    </div>

    <div class="mb-4 flex flex-wrap items-center gap-2.5">
      <label class="flex min-w-0 flex-[1_1_240px] items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2">
        <svg aria-hidden="true" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" class="size-4 shrink-0 text-[var(--color-text-faint)]"><circle cx="9" cy="9" r="6" /><path d="m17 17-3.5-3.5" stroke-linecap="round" /></svg>
        <span class="sr-only">{{ t('bethSalme.search') }}</span>
        <input id="beth-salme-search" v-model="query" type="search" :placeholder="t('bethSalme.search')" class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none">
      </label>
      <div v-if="signedIn" class="flex max-w-full min-w-0 gap-1.5 overflow-x-auto [scrollbar-width:none]" role="group" :aria-label="t('bethSalme.filterLabel')">
        <button
          v-for="value in filters"
          :key="value"
          type="button"
          class="whitespace-nowrap rounded-full border px-3 py-1.5 font-sans text-[13.5px]"
          :class="filter === value ? 'border-[var(--color-shmo)] bg-[var(--color-shmo-faint)] font-medium text-[var(--color-shmo)]' : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)]'"
          :aria-pressed="filter === value"
          @click="filter = value"
        >{{ t(`bethSalme.filters.${value}`) }}</button>
      </div>
    </div>

    <p v-if="rosterFailed" role="alert" class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('bethSalme.loadFailed') }}</p>
    <p v-else-if="pending && !figures.length" class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('common.loading') }}</p>

    <div v-else class="lg:grid lg:grid-cols-[minmax(0,1fr)_360px] lg:items-start lg:gap-7">
      <div>
        <p v-if="!shown.length" class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('bethSalme.empty') }}</p>
        <div class="grid grid-cols-3 gap-2.5 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-[repeat(auto-fill,minmax(118px,1fr))] lg:gap-3">
          <template v-for="row in rows" :key="'letter' in row ? `l-${row.letter}` : row.figure.id">
            <h2 v-if="'letter' in row" class="col-span-full border-b border-[var(--color-border)] pt-2 pb-1 font-sans text-xs font-semibold text-[var(--color-text-muted)]">{{ row.letter }}</h2>
            <FigureCard v-else :figure="row.figure" :selected="!lifted && selectedId === row.figure.id" @open="open" />
          </template>
        </div>
      </div>
      <aside class="hidden lg:sticky lg:top-24 lg:block">
        <FigurePanel v-if="selected && !lifted" :figure="selected" @close="close" />
        <p v-else class="rounded-[18px] border border-dashed border-[var(--color-border-strong)] p-5 font-serif text-[15px] text-[var(--color-text-muted)]">
          {{ t('bethSalme.pickOne') }}
        </p>
      </aside>
    </div>

    <FigurePopup v-if="selected && lifted" :figure="selected" @close="close" />
  </section>
</template>

<style scoped>
.illuminated-count {
  background: linear-gradient(90deg, #d0368a, #c79300, #0f9e88, #2f6fd6);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}
</style>
