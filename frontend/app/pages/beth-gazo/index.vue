<script setup lang="ts">
import type { BethGazoBookChantDto, BethGazoBookSectionDto } from '~/types/api'
import { chantCount, filterBook, lineFor, modeLabels } from '~/utils/bethGazo'

/**
 * Beth Gazo (ܒܝܬ ܓܙܐ), the treasury of chants, as an open book (Owner,
 * 2026-10-25). Every published, recorded chant is here and playable, with its
 * melody, mode, section and incipit, so a player can look up today's Nahlo: the
 * Owner ruled the treasury open to anyone who wants to learn or listen, knowing
 * that. One view, no tabs: the sections in the book's order, a search, and mode
 * pills; choosing a mode keeps only its chants and opens a card about it. One
 * player bar at the bottom.
 */
const { t, locale } = useI18n()
const config = useRuntimeConfig()
const mediaUrl = useMediaUrl()
const { book, pending, failed } = useBethGazoBook()

useSeoMeta({
  title: () => t('seo.bethGazo.title'),
  description: () => t('seo.bethGazo.description'),
  ogTitle: () => t('seo.bethGazo.title'),
  ogDescription: () => t('seo.bethGazo.description'),
})

const query = ref('')
const modeId = ref<string | null>(null)

const modes = computed(() => [...(book.value?.modes ?? [])].sort((a, b) => Number(a.position) - Number(b.position)))
const labels = computed(() => (book.value ? modeLabels(book.value) : new Map<string, string>()))
const modeById = computed(() => new Map(modes.value.map(m => [m.id, m])))
const sections = computed(() => (book.value ? filterBook(book.value, query.value, modeId.value) : []))
/** Each section's number in the book, counted rather than read from its position, which can skip. */
const ordinals = computed(() => new Map((book.value?.sections ?? []).map((s, i) => [s.id, i + 1])))

const totals = computed(() => {
  const all = book.value?.sections ?? []
  return {
    chants: chantCount(all),
    melodies: all.reduce((n, s) => n + s.melodies.length, 0),
  }
})

const chosenMode = computed(() => (modeId.value ? modeById.value.get(modeId.value) ?? null : null))
const chosenModeCount = computed(() => (modeId.value ? chantCount(sections.value) : 0))

function chooseMode(id: string | null) {
  modeId.value = modeId.value === id ? null : id
}

function sectionMeta(section: BethGazoBookSectionDto) {
  if (!section.melodies.length) return t('bethGazo.notRecorded')
  return t('bethGazo.sectionCounts', {
    melodies: t('bethGazo.melodies', section.melodies.length),
    chants: t('bethGazo.chants', chantCount([section])),
  })
}

/** The chosen chant, with what the player bar says about it. */
const current = ref<{ chant: BethGazoBookChantDto, melody: string, section: string } | null>(null)
const player = ref<{ toggle: () => void, paused: boolean } | null>(null)

function modeNameOf(chant: BethGazoBookChantDto) {
  return chant.modeId ? modeById.value.get(chant.modeId)?.name ?? null : null
}

function toggle(chant: BethGazoBookChantDto, melody: string, section: string) {
  if (current.value?.chant.id === chant.id) {
    player.value?.toggle()
    return
  }
  current.value = { chant, melody, section }
}

function isPlaying(chant: BethGazoBookChantDto) {
  return current.value?.chant.id === chant.id && player.value?.paused === false
}

const playerSubtitle = computed(() => {
  if (!current.value) return ''
  return [modeNameOf(current.value.chant), current.value.section].filter(Boolean).join(' · ')
})
</script>

<template>
  <section class="mx-auto max-w-4xl">
    <header class="mb-6 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-nahlo)]">
        {{ t('bethGazo.title') }}
        <span dir="rtl" lang="syc" class="ml-1 font-syriac text-[15px] normal-case tracking-normal">ܒܝܬ ܓܙܐ</span>
      </p>
      <h1 class="mt-3 font-serif text-4xl font-semibold tracking-[-0.02em] text-balance sm:text-[2.75rem]">{{ t('bethGazo.heading') }}</h1>
      <p class="mt-3 max-w-2xl font-serif text-[17px] text-[var(--color-text-muted)]">
        {{ book ? t('bethGazo.lede', { chants: t('bethGazo.chants', totals.chants), melodies: t('bethGazo.melodies', totals.melodies) }) : t('bethGazo.ledeEmpty') }}
      </p>
      <a
        :href="config.public.nahloUrl"
        class="mt-4 inline-flex items-center gap-2 rounded-[10px] bg-[var(--color-nahlo)] px-3.5 py-2 font-sans text-[13.5px] font-semibold text-[var(--color-bg)] no-underline hover:bg-[var(--color-nahlo-dark)]"
      ><span aria-hidden="true" class="font-syriac text-[15px] leading-none">ܢ</span>{{ t('bethGazo.play') }}</a>
    </header>

    <p v-if="failed" role="alert" class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('bethGazo.loadFailed') }}</p>
    <p v-else-if="pending && !book" class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('common.loading') }}</p>

    <template v-else-if="book">
      <div class="mb-2 flex flex-wrap items-center gap-2.5">
        <label class="flex min-w-0 flex-[1_1_240px] items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2">
          <svg aria-hidden="true" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" class="size-4 shrink-0 text-[var(--color-text-faint)]"><circle cx="9" cy="9" r="6" /><path d="m17 17-3.5-3.5" stroke-linecap="round" /></svg>
          <span class="sr-only">{{ t('bethGazo.search') }}</span>
          <input id="beth-gazo-search" v-model="query" type="search" :placeholder="t('bethGazo.search')" class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none">
        </label>
        <div class="flex max-w-full min-w-0 gap-1.5 overflow-x-auto [scrollbar-width:none]" role="group" :aria-label="t('bethGazo.modesLabel')">
          <button
            type="button"
            class="whitespace-nowrap rounded-full border px-3 py-1.5 font-sans text-[13.5px]"
            :class="modeId === null ? 'border-[var(--color-nahlo)] bg-[var(--color-nahlo-faint)] font-medium text-[var(--color-nahlo)]' : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)]'"
            :aria-pressed="modeId === null"
            @click="modeId = null"
          >{{ t('bethGazo.allModes') }}</button>
          <button
            v-for="mode in modes"
            :key="mode.id"
            type="button"
            class="min-w-9 whitespace-nowrap rounded-full border px-3 py-1.5 font-sans text-[13.5px] tabular-nums"
            :class="modeId === mode.id ? 'border-[var(--color-nahlo)] bg-[var(--color-nahlo-faint)] font-medium text-[var(--color-nahlo)]' : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)]'"
            :aria-pressed="modeId === mode.id"
            :aria-label="mode.name"
            :title="mode.name"
            @click="chooseMode(mode.id)"
          >{{ labels.get(mode.id) }}</button>
        </div>
      </div>

      <div
        v-if="chosenMode"
        class="mt-4 rounded-2xl border border-[color-mix(in_oklab,var(--color-nahlo)_35%,var(--color-border))] bg-[color-mix(in_oklab,var(--color-nahlo-faint)_50%,var(--color-bg-elevated))] px-4 py-3.5"
        aria-live="polite"
      >
        <div class="flex items-center gap-3">
          <span
            aria-hidden="true"
            class="grid size-9 shrink-0 place-items-center rounded-[10px] font-serif text-[17px] font-semibold"
            :class="labels.get(chosenMode.id) === '✶' ? 'bg-[var(--color-nahlo-faint)] text-[var(--color-nahlo)]' : 'bg-[var(--color-nahlo)] text-[var(--color-bg)]'"
          >{{ labels.get(chosenMode.id) }}</span>
          <h2 class="font-serif text-[17px] font-semibold">{{ chosenMode.name }}</h2>
          <span v-if="chosenMode.syriacName" dir="rtl" lang="syc" class="ml-auto font-syriac text-[17px] text-[var(--color-nahlo)]">{{ chosenMode.syriacName }}</span>
        </div>
        <p v-if="lineFor(chosenMode.descriptions, locale)" class="mt-2 font-serif text-[14.5px] leading-relaxed text-[var(--color-text)]">{{ lineFor(chosenMode.descriptions, locale) }}</p>
        <p class="mt-1.5 font-sans text-xs text-[var(--color-text-muted)]">
          {{ chosenModeCount ? t('bethGazo.inThisMode', { chants: t('bethGazo.chants', chosenModeCount) }) : t('bethGazo.noneInMode') }}
        </p>
      </div>

      <p v-if="!sections.length && query" class="mt-6 font-sans text-sm text-[var(--color-text-muted)]">{{ t('bethGazo.empty') }}</p>

      <ol class="mt-5">
        <li v-for="section in sections" :key="section.id" class="border-t border-[var(--color-border)] py-4">
          <div class="flex items-baseline gap-3">
            <span aria-hidden="true" class="w-6 shrink-0 text-right font-serif text-sm font-semibold tabular-nums text-[var(--color-accent)]">{{ ordinals.get(section.id) }}</span>
            <div class="min-w-0 flex-1">
              <h2 class="font-serif text-[19px] font-semibold first-letter:text-[var(--color-accent)]">
                {{ section.name }}
                <span v-if="section.syriacName" dir="rtl" lang="syc" class="ml-1.5 inline-block whitespace-nowrap font-syriac text-base font-normal text-[var(--color-text-muted)]">{{ section.syriacName }}</span>
              </h2>
              <p v-if="lineFor(section.descriptions, locale)" class="mt-0.5 font-serif text-[14px] italic leading-snug text-[var(--color-text-muted)]">{{ lineFor(section.descriptions, locale) }}</p>
            </div>
            <span class="shrink-0 font-sans text-xs text-[var(--color-text-muted)]">{{ sectionMeta(section) }}</span>
          </div>

          <div v-for="melody in section.melodies" :key="melody.name" class="mt-3 sm:ml-9">
            <h3 class="mb-1.5 mt-3 font-serif text-[16.5px] font-semibold">{{ melody.name }}</h3>
            <div class="grid gap-1.5 sm:grid-cols-2">
              <BethGazoChant
                v-for="chant in melody.chants"
                :key="chant.id"
                :chant="chant"
                :melody="melody.name"
                :mode-name="modeNameOf(chant)"
                :mode-label="chant.modeId ? labels.get(chant.modeId) ?? null : null"
                :playing="isPlaying(chant)"
                @toggle="toggle(chant, melody.name, section.name)"
              />
            </div>
          </div>
        </li>
      </ol>

      <BethGazoPlayer
        v-if="current"
        ref="player"
        :src="mediaUrl(current.chant.audioUrl)"
        :title="current.melody"
        :subtitle="playerSubtitle"
        @close="current = null"
      />
    </template>
  </section>
</template>
