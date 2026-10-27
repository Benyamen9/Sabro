<script setup lang="ts">
import type { BethGazoBookChantDto, BethGazoBookMelodyDto, BethGazoBookSectionDto } from '~/types/api'
import { chantCount, filterBook, foundByIncipit, lineFor, locateChant, modeLabels, modeSlots } from '~/utils/bethGazo'

/**
 * Beth Gazo (ܒܝܬ ܓܙܐ), the treasury of chants, as an open book (Owner,
 * 2026-10-25). Every published, recorded chant is here and playable, with its
 * melody, mode, section and incipit, so a player can look up today's Nahlo: the
 * Owner ruled the treasury open to anyone who wants to learn or listen, knowing
 * that.
 *
 * Laid out as closed chapters (Owner, 2026-10-27), so a full book stays short:
 * the sections open one by one, each melody is one line with a button per mode,
 * and the melody's name unfolds it into its chants with their incipits. A
 * section without modes lists its chants by incipit straight away, since nothing
 * else tells them apart. A search or a mode pill opens the sections with a match.
 * The ribbon remembers the last chant played, on this device, and the page
 * reopens there.
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
const narrowed = computed(() => query.value.trim().length > 0 || modeId.value !== null)

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

/**
 * What is open. With nothing searched or chosen, a section is open only once
 * tapped; while narrowed every section with a match is open unless tapped shut.
 * A melody unfolds when its name is tapped, or by itself when the search found it
 * by an incipit.
 */
const openSections = ref(new Set<string>())
const shutWhileNarrowed = ref(new Set<string>())
const unfolded = ref(new Set<string>())

watch(narrowed, () => shutWhileNarrowed.value.clear())

function isOpen(section: BethGazoBookSectionDto) {
  return narrowed.value ? !shutWhileNarrowed.value.has(section.id) : openSections.value.has(section.id)
}

function toggleSection(section: BethGazoBookSectionDto) {
  const set = narrowed.value ? shutWhileNarrowed.value : openSections.value
  if (set.has(section.id)) set.delete(section.id)
  else set.add(section.id)
}

const melodyKey = (section: BethGazoBookSectionDto, melody: BethGazoBookMelodyDto) => `${section.id}|${melody.name}`

function isUnfolded(section: BethGazoBookSectionDto, melody: BethGazoBookMelodyDto) {
  return unfolded.value.has(melodyKey(section, melody)) || foundByIncipit(melody, query.value)
}

function toggleMelody(section: BethGazoBookSectionDto, melody: BethGazoBookMelodyDto) {
  const key = melodyKey(section, melody)
  if (unfolded.value.has(key)) unfolded.value.delete(key)
  else unfolded.value.add(key)
}

/** A melody with no chant in a mode: a madrosho, listed by incipit. */
const isModeless = (melody: BethGazoBookMelodyDto) => melody.chants.every(c => !c.modeId)

function sectionMeta(section: BethGazoBookSectionDto) {
  if (narrowed.value) return t('bethGazo.found', section.melodies.length)
  const full = book.value?.sections.find(s => s.id === section.id) ?? section
  if (!full.melodies.length) return t('bethGazo.notRecorded')
  return t('bethGazo.sectionCounts', {
    melodies: t('bethGazo.melodies', full.melodies.length),
    chants: t('bethGazo.chants', chantCount([full])),
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
  saveRibbon(chant.id)
}

function isPlaying(chant: BethGazoBookChantDto) {
  return current.value?.chant.id === chant.id && player.value?.paused === false
}

const playerSubtitle = computed(() => {
  if (!current.value) return ''
  return [modeNameOf(current.value.chant), current.value.section].filter(Boolean).join(' · ')
})

/**
 * The ribbon: the last chant played, kept on this device only (a per-viewer
 * convenience, so browser storage and never an account). Storage can be missing
 * or refuse, in which case the page simply has no ribbon.
 */
const RIBBON_KEY = 'sabro:beth-gazo:ribbon'
const ribbonId = ref<string | null>(null)
const ribbon = computed(() => (book.value && ribbonId.value ? locateChant(book.value, ribbonId.value) : null))

function saveRibbon(id: string) {
  ribbonId.value = id
  try {
    localStorage.setItem(RIBBON_KEY, id)
  }
  catch {
    // No storage: the ribbon lasts this visit only.
  }
}

onMounted(() => {
  try {
    ribbonId.value = localStorage.getItem(RIBBON_KEY)
  }
  catch {
    ribbonId.value = null
  }
  const found = ribbon.value
  if (!found) return
  // Reopen the book where it was left: the section, and the melody unfolded.
  openSections.value.add(found.section.id)
  unfolded.value.add(melodyKey(found.section, found.melody))
})

/** Which melody line carries the ribbon mark. */
const ribbonMelody = computed(() => (ribbon.value ? melodyKey(ribbon.value.section, ribbon.value.melody) : null))

const ribbonLabel = computed(() => {
  const found = ribbon.value
  if (!found) return ''
  return [found.melody.name, modeNameOf(found.chant), found.section.name].filter(Boolean).join(' · ')
})

async function continueFromRibbon() {
  const found = ribbon.value
  if (!found) return
  query.value = ''
  modeId.value = null
  openSections.value.add(found.section.id)
  unfolded.value.add(melodyKey(found.section, found.melody))
  toggle(found.chant, found.melody.name, found.section.name)
  await nextTick()
  document.getElementById(`chant-${found.chant.id}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' })
}
</script>

<template>
  <section class="mx-auto max-w-4xl">
    <header class="mb-5 pt-2">
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
      <button
        v-if="ribbon && !current"
        type="button"
        class="mb-4 flex w-full items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] py-2.5 pr-3 pl-3 text-left hover:border-[var(--color-border-strong)]"
        @click="continueFromRibbon"
      >
        <svg aria-hidden="true" viewBox="0 0 14 20" class="h-5 w-3.5 shrink-0 text-[var(--color-accent)]"><path d="M1 0h12v19l-6-4.5L1 19z" fill="currentColor" /></svg>
        <span class="min-w-0 flex-1">
          <span class="block font-sans text-xs text-[var(--color-text-muted)]">{{ t('bethGazo.ribbon') }}</span>
          <span class="block truncate font-serif text-[15px] font-semibold text-[var(--color-text)]">{{ ribbonLabel }}</span>
        </span>
        <span class="shrink-0 rounded-full bg-[var(--color-nahlo)] px-3 py-1.5 font-sans text-[13px] font-semibold text-[var(--color-bg)]">{{ t('bethGazo.continue') }}</span>
      </button>

      <div class="flex flex-wrap items-center gap-2.5">
        <label class="flex min-w-0 flex-[1_1_240px] items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2">
          <svg aria-hidden="true" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" class="size-4 shrink-0 text-[var(--color-text-faint)]"><circle cx="9" cy="9" r="6" /><path d="m17 17-3.5-3.5" stroke-linecap="round" /></svg>
          <span class="sr-only">{{ t('bethGazo.search') }}</span>
          <input id="beth-gazo-search" v-model="query" type="search" :placeholder="t('bethGazo.search')" class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none">
        </label>
        <div class="flex max-w-full min-w-0 gap-1.5 overflow-x-auto [scrollbar-width:none]" role="group" :aria-label="t('bethGazo.modesLabel')">
          <button
            type="button"
            class="shrink-0 whitespace-nowrap rounded-full border px-3 py-1.5 font-sans text-[13.5px]"
            :class="modeId === null ? 'border-[var(--color-nahlo)] bg-[var(--color-nahlo-faint)] font-medium text-[var(--color-nahlo)]' : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)]'"
            :aria-pressed="modeId === null"
            @click="modeId = null"
          >{{ t('bethGazo.allModes') }}</button>
          <button
            v-for="mode in modes"
            :key="mode.id"
            type="button"
            class="min-w-9 shrink-0 whitespace-nowrap rounded-full border px-3 py-1.5 font-sans text-[13.5px] tabular-nums"
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

      <ol class="mt-4">
        <li v-for="section in sections" :key="section.id" class="border-t border-[var(--color-border)]">
          <button
            type="button"
            class="flex w-full items-center gap-3 py-3.5 text-left"
            :aria-expanded="isOpen(section)"
            @click="toggleSection(section)"
          >
            <span aria-hidden="true" class="w-6 shrink-0 text-right font-serif text-sm font-semibold tabular-nums text-[var(--color-accent)]">{{ ordinals.get(section.id) }}</span>
            <span class="min-w-0 flex-1">
              <span class="block font-serif text-[19px] font-semibold first-letter:text-[var(--color-accent)]">
                {{ section.name }}
                <span v-if="section.syriacName" dir="rtl" lang="syc" class="ml-1.5 inline-block whitespace-nowrap font-syriac text-base font-normal text-[var(--color-text-muted)]">{{ section.syriacName }}</span>
              </span>
              <span v-if="lineFor(section.descriptions, locale)" class="mt-0.5 block font-serif text-[14px] italic leading-snug text-[var(--color-text-muted)]">{{ lineFor(section.descriptions, locale) }}</span>
              <span class="mt-0.5 block font-sans text-xs text-[var(--color-text-muted)] sm:hidden">{{ sectionMeta(section) }}</span>
            </span>
            <span class="hidden shrink-0 font-sans text-xs text-[var(--color-text-muted)] sm:block">{{ sectionMeta(section) }}</span>
            <svg aria-hidden="true" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" class="size-4 shrink-0 text-[var(--color-text-faint)] transition-transform" :class="isOpen(section) ? 'rotate-90' : ''"><path d="m6 3 5 5-5 5" stroke-linecap="round" stroke-linejoin="round" /></svg>
          </button>

          <div v-if="isOpen(section) && section.melodies.length" class="pb-4 sm:pl-9">
            <div v-for="melody in section.melodies" :key="melody.name" class="border-t border-dashed border-[var(--color-border)] py-1.5 first:border-t-0">
              <!-- A madrosho: no modes, so its chants are listed by incipit straight away. -->
              <template v-if="isModeless(melody)">
                <p class="flex items-center gap-1.5 pt-1 font-serif text-[15.5px] font-semibold">
                  {{ melody.name }}
                  <svg v-if="ribbonMelody === melodyKey(section, melody)" aria-hidden="true" viewBox="0 0 14 20" class="h-3.5 w-2.5 text-[var(--color-accent)]"><path d="M1 0h12v19l-6-4.5L1 19z" fill="currentColor" /></svg>
                </p>
                <BethGazoChant
                  v-for="chant in melody.chants"
                  :id="`chant-${chant.id}`"
                  :key="chant.id"
                  :chant="chant"
                  :melody="melody.name"
                  :mode-name="null"
                  :mode-label="null"
                  :playing="isPlaying(chant)"
                  :current="current?.chant.id === chant.id"
                  @toggle="toggle(chant, melody.name, section.name)"
                />
              </template>

              <template v-else>
                <div class="flex flex-wrap items-center gap-x-3 gap-y-1.5">
                  <button
                    type="button"
                    class="flex min-w-0 flex-1 basis-40 items-center gap-1.5 text-left font-serif text-[15.5px] font-semibold"
                    :aria-expanded="isUnfolded(section, melody)"
                    @click="toggleMelody(section, melody)"
                  >
                    <span class="truncate">{{ melody.name }}</span>
                    <svg v-if="ribbonMelody === melodyKey(section, melody)" aria-hidden="true" viewBox="0 0 14 20" class="h-3.5 w-2.5 shrink-0 text-[var(--color-accent)]"><path d="M1 0h12v19l-6-4.5L1 19z" fill="currentColor" /></svg>
                    <svg aria-hidden="true" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" class="size-3.5 shrink-0 text-[var(--color-text-faint)] transition-transform" :class="isUnfolded(section, melody) ? 'rotate-90' : ''"><path d="m6 3 5 5-5 5" stroke-linecap="round" stroke-linejoin="round" /></svg>
                  </button>
                  <div class="flex flex-wrap gap-1">
                    <button
                      v-for="slot in modeSlots(melody.chants)"
                      :key="slot.modeId"
                      type="button"
                      class="relative grid size-[30px] place-items-center rounded-lg border font-sans text-[13px] font-semibold tabular-nums"
                      :class="current?.chant.id === slot.main.id
                        ? 'border-[var(--color-nahlo)] bg-[var(--color-nahlo)] text-[var(--color-bg)]'
                        : 'border-[color-mix(in_oklab,var(--color-nahlo)_30%,var(--color-border))] bg-[var(--color-bg-elevated)] text-[var(--color-nahlo)] hover:bg-[var(--color-nahlo-faint)]'"
                      :title="modeById.get(slot.modeId)?.name"
                      :aria-label="t(isPlaying(slot.main) ? 'bethGazo.pauseChant' : 'bethGazo.playChant', { chant: [melody.name, modeById.get(slot.modeId)?.name].filter(Boolean).join(', ') })"
                      @click="toggle(slot.main, melody.name, section.name)"
                    >
                      {{ labels.get(slot.modeId) }}
                      <span v-if="slot.variants.length" aria-hidden="true" class="absolute top-[3px] right-[3px] size-[5px] rounded-full bg-current opacity-70" />
                    </button>
                  </div>
                </div>
                <div v-if="isUnfolded(section, melody)" class="mt-1.5 mb-1 border-l-2 border-[var(--color-nahlo-faint)] pl-2.5">
                  <BethGazoChant
                    v-for="chant in melody.chants"
                    :id="`chant-${chant.id}`"
                    :key="chant.id"
                    :chant="chant"
                    :melody="melody.name"
                    :mode-name="modeNameOf(chant)"
                    :mode-label="chant.modeId ? labels.get(chant.modeId) ?? null : null"
                    :playing="isPlaying(chant)"
                    :current="current?.chant.id === chant.id"
                    @toggle="toggle(chant, melody.name, section.name)"
                  />
                </div>
              </template>
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
        :incipit="current.chant.syriacIncipitVocalized ?? current.chant.syriacIncipit"
        @close="current = null"
      />
    </template>
  </section>
</template>
