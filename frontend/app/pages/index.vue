<script setup lang="ts">
const { t, locale } = useI18n()
const { melthoUrl, mnoUrl, shmoUrl } = useRuntimeConfig().public
const { listWords } = useMelthoLibrary()
const preferredMeaning = usePreferredMeaning()

// The daily circuit: which games were finished today (shared cookie written
// by Meltho, Mno and Shmo). Played cards swap their LIVE dot for a check, and
// the hero's lead button follows the next unplayed game.
const { hasPlayed, nextUnplayed } = useDailyCircuit()
const melthoPlayed = computed(() => hasPlayed('meltho'))
const mnoPlayed = computed(() => hasPlayed('mno'))
const shmoPlayed = computed(() => hasPlayed('shmo'))
const nextGame = computed(() => nextUnplayed())

// The hero word-board: the name ܣܒܪܐ spelled letter by letter, right to left.
// Letter names come from the shared library.letters table (West Syriac forms);
// the vocalized glyphs and romanized sounds are content, not UI strings.
const wordBoard = [
  { glyph: 'ܣܰ', code: 'Semkath', sound: 'sa' },
  { glyph: 'ܒ', code: 'Beth', sound: 'b' },
  { glyph: 'ܪܳ', code: 'Rish', sound: 'ro' },
  { glyph: 'ܐ', code: 'Alaph', sound: '—' },
] as const

// The feature card's hero: Meltho's letters as a tile board — teal "correct"
// tiles with the lomad (ܠ) and olaf (ܐ) as gold "present" tiles, with those two
// swapped in position. Decorative, so it's aria-hidden; the heading carries the
// name for assistive tech.
const melthoTiles = [
  { letter: 'ܡ', state: 'correct' },
  { letter: 'ܐ', state: 'present' },
  { letter: 'ܬ', state: 'correct' },
  { letter: 'ܠ', state: 'present' },
] as const

function tileClass(state: 'correct' | 'present') {
  const base
    = 'flex size-12 items-center justify-center rounded-xl border-2 text-2xl text-white sm:size-14 sm:text-3xl'
  return state === 'present'
    ? `${base} border-[var(--color-meltho-gold-dark)] bg-[var(--color-meltho-gold)]`
    : `${base} border-[var(--color-meltho-dark)] bg-[var(--color-meltho)]`
}

// Mno's hero: the game's own name, ܡܢܐ, read as an equation — its letters ARE
// numbers (40 + 50 × 1). Scored like a real board in Mno's own feedback colours
// (green correct, its gold for wrong place), matching the game's tiles: the +
// scores green with the values, the × is neutral notation. Decorative, like
// Meltho's board.
const mnoTiles = [
  { glyph: 'ܡ', hint: '40', state: 'correct' },
  { glyph: '+', hint: '', state: 'operatorHit' },
  { glyph: 'ܢ', hint: '50', state: 'present' },
  { glyph: '×', hint: '', state: 'operator' },
  { glyph: 'ܐ', hint: '1', state: 'correct' },
] as const

// Mno's feedback palette on the hub: green correct + its own gold for wrong
// place, matching the game's own tiles. The green is a literal hex — like Shmo's
// near colour below, this hub preview is the only place outside Mno's repo that
// needs it; the gold is already the shared --color-mno.
function mnoTileClass(state: 'correct' | 'present' | 'operatorHit' | 'operator') {
  const base = 'flex size-12 flex-col items-center justify-center rounded-xl border-2 sm:size-14'
  if (state === 'present') return `${base} border-[var(--color-mno-dark)] bg-[var(--color-mno)] text-white`
  if (state === 'operator') return `${base} border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]`
  return `${base} border-[#3d7e44] bg-[#4e9c56] text-white`
}

// Shmo's hero: three real scored cells from a guess row — a worked example
// (Jacob of Serugh guessed against the answer Ephrem the Syrian), the same
// case verified by hand during development. Hit (green), near with a
// direction arrow (amber), miss (grey) — the same three states a real round
// scores. Decorative, like Meltho's and Mno's boards; real values (not
// abstract category names) so it reads as actual gameplay at a glance.
const shmoTiles = [
  { value: 'Patristic', state: 'hit', arrow: null },
  { value: '4th c.', state: 'near', arrow: '↓' },
  { value: 'Bishop', state: 'miss', arrow: null },
] as const

// The "near" amber matches Shmo's own app palette (--color-cell-near there) —
// a generic amber, not Meltho's gold identity colour, kept as a literal hex
// since this hub preview is the only place outside Shmo's own repo that needs it.
function shmoTileClass(state: 'hit' | 'near' | 'miss') {
  const base
    = 'flex h-12 min-w-[3.25rem] items-center justify-center gap-1 rounded-xl border-2 px-2 text-white sm:h-14'
  if (state === 'hit') return `${base} border-[color-mix(in_oklab,var(--color-shmo)_70%,black)] bg-[var(--color-shmo)]`
  if (state === 'near') return `${base} border-[#ab9538] bg-[#c9b043]`
  return `${base} border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] !text-[var(--color-text-faint)]`
}

// The newest past word, shown as the library band's live chip. Best-effort:
// with the API unreachable the band simply renders without it.
const { data: latest } = await useAsyncData(
  'home-latest-word',
  async () => {
    const result = await listWords({ page: 1, pageSize: 1, sort: 'Recent', direction: 'Descending' })
    return result.items[0] ?? null
  },
  { lazy: true, default: () => null },
)

const latestShownOn = computed(() => {
  const shown = latest.value?.lastPlayedOn
  if (!shown) return ''
  return new Intl.DateTimeFormat(locale.value, { month: 'short', day: 'numeric' })
    .format(new Date(`${shown}T00:00:00`))
})

const primaryButton
  = 'inline-flex items-center gap-2 rounded-xl bg-[var(--color-accent)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(140_47_57/0.25)] transition-all hover:-translate-y-px hover:bg-[var(--color-accent-hover)] hover:shadow-[0_5px_14px_rgb(140_47_57/0.3)]'

// The hero carries ONE action (the cards below hold the rest): the circuit's
// next unplayed game, or the library once the day is complete.
const heroAction = computed(() => {
  if (nextGame.value === 'mno') return { href: mnoUrl, label: 'home.mno.cta' }
  if (nextGame.value === 'shmo') return { href: shmoUrl, label: 'home.shmo.cta' }
  if (nextGame.value === null) return { href: '/library', label: 'home.library.cta' }
  return { href: melthoUrl, label: 'home.meltho.cta' }
})
</script>

<template>
  <div>
    <!-- Hero: the claim on the left, the claim made visible on the right —
         "letter by letter" shown on the name itself, in the same card anatomy
         the library uses. -->
    <section class="grid items-center gap-10 pt-2 sm:pt-6 lg:grid-cols-[minmax(0,1fr)_auto] lg:gap-12">
      <div class="max-w-2xl">
        <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
          {{ t('home.eyebrow') }}
        </p>

        <h1 class="mt-5 font-serif text-4xl font-semibold leading-[1.06] tracking-[-0.02em] sm:text-5xl md:text-[3.4rem]">
          <span class="block font-normal italic">{{ t('home.titleLine1') }}</span>
          <span class="block">{{ t('home.titleLine2') }}</span>
        </h1>

        <!-- scope="global" is required, not decorative: <i18n-t> resolves against
             its PARENT's i18n scope by default, this page owns no local scope, and
             since @nuxtjs/i18n v10 that combination warns "Not found parent scope"
             on every render before falling back to global anyway. Saying so is
             both the fix and the truth — the app has one locale, app-wide. -->
        <i18n-t
          keypath="home.lede"
          scope="global"
          tag="p"
          class="mt-6 max-w-xl font-serif text-lg leading-relaxed text-[var(--color-text-muted)] sm:text-xl"
        >
          <template #meltho>
            <strong class="font-semibold text-[var(--color-text)]">Meltho</strong>
          </template>
          <template #mno>
            <strong class="font-semibold text-[var(--color-text)]">Mno</strong>
          </template>
          <template #shmo>
            <strong class="font-semibold text-[var(--color-text)]">Shmo</strong>
          </template>
        </i18n-t>

        <!-- One action only — the game cards below carry the rest. -->
        <div class="mt-8">
          <NuxtLink :to="heroAction.href" :class="primaryButton">{{ t(heroAction.label) }} →</NuxtLink>
        </div>
      </div>

      <div class="text-center" :aria-label="t('home.nameGloss')">
        <div class="flex justify-center gap-2 sm:gap-3" dir="rtl">
          <div
            v-for="(tile, index) in wordBoard"
            :key="tile.code + index"
            class="word-tile w-[19vw] max-w-24 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-0 pb-3.5 pt-4 shadow-[var(--shadow-soft)] lg:w-[6.75rem] lg:max-w-none"
          >
            <SyriacText :text="tile.glyph" class="!text-[3.4rem] leading-[1.35] text-[var(--color-accent)]" />
            <p class="mt-2 font-sans text-xs font-semibold">{{ t(`library.letters.${tile.code}`) }}</p>
            <p class="mt-px font-sans text-[11px] text-[var(--color-text-faint)]">{{ tile.sound }}</p>
          </div>
        </div>
        <p class="mt-4 font-sans text-sm text-[var(--color-text-muted)]">{{ t('home.nameGloss') }}</p>
      </div>
    </section>

    <!-- Live now: the two launched games, each in its own colour, each with a
         direct play CTA. -->
    <section class="mt-14 sm:mt-16">
      <div class="border-b border-[var(--color-border)] pb-3.5">
        <span class="font-sans text-xs font-semibold uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
          {{ t('home.liveNow') }}
        </span>
      </div>

      <div class="mt-5 grid gap-5 lg:grid-cols-3">
        <!-- Meltho — the word game, in its teal. -->
        <div
          class="relative flex flex-col items-center gap-5 overflow-hidden rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-7 text-center shadow-[var(--shadow-soft)] transition-all hover:-translate-y-0.5 hover:shadow-[0_8px_24px_rgb(28_25_23/0.09)] sm:items-start sm:text-left"
        >
          <span class="absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r from-[var(--color-meltho)] to-transparent" aria-hidden="true" />
          <div class="flex gap-1.5 sm:gap-2" dir="rtl" aria-hidden="true">
            <span v-for="tile in melthoTiles" :key="tile.letter" :class="tileClass(tile.state)">
              <SyriacText :text="tile.letter" class="leading-none" />
            </span>
          </div>

          <div class="flex-1">
            <h2 class="flex flex-wrap items-center justify-center gap-x-3 gap-y-1.5 font-sans text-2xl sm:justify-start">
              {{ t('home.meltho.heading') }}
              <span
                v-if="melthoPlayed"
                class="inline-flex items-center text-sm font-bold text-green-700 dark:text-green-400"
                :title="t('home.playedToday')"
                :aria-label="t('home.playedToday')"
              >✓</span>
              <span
                v-else
                class="inline-flex items-center"
                :title="t('home.meltho.live')"
                :aria-label="t('home.meltho.live')"
              >
                <span class="size-2 rounded-full bg-green-500 ring-4 ring-green-500/20" />
              </span>
            </h2>
            <p class="mx-auto mt-2 max-w-md font-serif text-[var(--color-text-muted)] sm:mx-0">
              {{ t('home.meltho.body') }}
            </p>
          </div>

          <a
            :href="melthoUrl"
            class="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-[var(--color-meltho)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(0_0_0/0.12)] transition-colors hover:bg-[var(--color-meltho-dark)] sm:w-auto"
          >
            {{ t('home.meltho.cta') }} →
          </a>
        </div>

        <!-- Mno — the numbers game. Card accent is Mno's honey amber; the tile
             board is the name ܡܢܐ scored in the game's own green/gold feedback. -->
        <div
          class="relative flex flex-col items-center gap-5 overflow-hidden rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-7 text-center shadow-[var(--shadow-soft)] transition-all hover:-translate-y-0.5 hover:shadow-[0_8px_24px_rgb(28_25_23/0.09)] sm:items-start sm:text-left"
        >
          <span class="absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r from-[var(--color-mno)] to-transparent" aria-hidden="true" />
          <div class="flex gap-1.5 sm:gap-2" dir="rtl" aria-hidden="true">
            <span
              v-for="(tile, index) in mnoTiles"
              :key="index"
              :class="mnoTileClass(tile.state)"
            >
              <span
                v-if="tile.state === 'operator' || tile.state === 'operatorHit'"
                class="font-sans text-2xl font-semibold leading-none sm:text-3xl"
              >{{ tile.glyph }}</span>
              <SyriacText v-else :text="tile.glyph" class="text-2xl leading-none sm:text-3xl" />
              <span v-if="tile.hint" class="mt-0.5 font-sans text-[10px] leading-none opacity-80" dir="ltr">{{ tile.hint }}</span>
            </span>
          </div>

          <div class="flex-1">
            <h2 class="flex flex-wrap items-center justify-center gap-x-3 gap-y-1.5 font-sans text-2xl sm:justify-start">
              {{ t('home.mno.heading') }}
              <span
                v-if="mnoPlayed"
                class="inline-flex items-center text-sm font-bold text-green-700 dark:text-green-400"
                :title="t('home.playedToday')"
                :aria-label="t('home.playedToday')"
              >✓</span>
              <span
                v-else
                class="inline-flex items-center"
                :title="t('home.mno.live')"
                :aria-label="t('home.mno.live')"
              >
                <span class="size-2 rounded-full bg-green-500 ring-4 ring-green-500/20" />
              </span>
            </h2>
            <p class="mx-auto mt-2 max-w-md font-serif text-[var(--color-text-muted)] sm:mx-0">
              {{ t('home.mno.body') }}
            </p>
          </div>

          <a
            :href="mnoUrl"
            class="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-[var(--color-mno)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(0_0_0/0.12)] transition-colors hover:bg-[var(--color-mno-dark)] sm:w-auto"
          >
            {{ t('home.mno.cta') }} →
          </a>
        </div>

        <!-- Shmo — the historical-figure game, in its verdigris. -->
        <div
          class="relative flex flex-col items-center gap-5 overflow-hidden rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-7 text-center shadow-[var(--shadow-soft)] transition-all hover:-translate-y-0.5 hover:shadow-[0_8px_24px_rgb(28_25_23/0.09)] sm:items-start sm:text-left"
        >
          <span class="absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r from-[var(--color-shmo)] to-transparent" aria-hidden="true" />
          <div class="flex flex-wrap justify-center gap-1.5 sm:justify-start sm:gap-2" aria-hidden="true">
            <span v-for="tile in shmoTiles" :key="tile.value" :class="shmoTileClass(tile.state)">
              <span class="font-sans text-[11px] font-semibold leading-none sm:text-xs">{{ tile.value }}</span>
              <span v-if="tile.arrow" class="font-sans text-xs font-bold leading-none">{{ tile.arrow }}</span>
            </span>
          </div>

          <div class="flex-1">
            <h2 class="flex flex-wrap items-center justify-center gap-x-3 gap-y-1.5 font-sans text-2xl sm:justify-start">
              {{ t('home.shmo.heading') }}
              <span
                v-if="shmoPlayed"
                class="inline-flex items-center text-sm font-bold text-green-700 dark:text-green-400"
                :title="t('home.playedToday')"
                :aria-label="t('home.playedToday')"
              >✓</span>
              <span
                v-else
                class="inline-flex items-center"
                :title="t('home.shmo.live')"
                :aria-label="t('home.shmo.live')"
              >
                <span class="size-2 rounded-full bg-green-500 ring-4 ring-green-500/20" />
              </span>
            </h2>
            <p class="mx-auto mt-2 max-w-md font-serif text-[var(--color-text-muted)] sm:mx-0">
              {{ t('home.shmo.body') }}
            </p>
          </div>

          <a
            :href="shmoUrl"
            class="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-[var(--color-shmo)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(0_0_0/0.12)] transition-colors hover:bg-[var(--color-shmo-dark)] sm:w-auto"
          >
            {{ t('home.shmo.cta') }} →
          </a>
        </div>
      </div>
    </section>

    <!-- The library as a living archive, led by its newest entry. -->
    <section class="mt-10 overflow-hidden rounded-2xl border border-[var(--color-border)]">
      <div class="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 px-7 pt-6 sm:px-8">
        <h3 class="font-sans text-xl">{{ t('home.library.heading') }}</h3>
        <NuxtLink to="/library" class="font-sans text-sm font-semibold text-[var(--color-accent)] no-underline">
          {{ t('home.library.cta') }} →
        </NuxtLink>
      </div>
      <p class="max-w-2xl px-7 pt-1.5 font-serif text-[var(--color-text-muted)] sm:px-8">
        {{ t('home.library.body') }}
      </p>
      <NuxtLink
        v-if="latest"
        :to="`/library/${latest.lexiconEntryId}`"
        class="mx-7 mb-6 mt-5 flex flex-wrap items-center gap-x-5 gap-y-2 rounded-xl bg-[var(--color-bg-subtle)] px-5 py-4 no-underline transition-colors hover:bg-[var(--color-accent-faint)] sm:mx-8"
      >
        <SyriacText :text="latest.syriacUnvocalized" class="!text-[2.1rem] leading-none" />
        <span class="font-sans text-sm text-[var(--color-text-muted)]">
          <strong v-if="latest.sblTransliteration" class="font-semibold text-[var(--color-text)]">{{ latest.sblTransliteration }}</strong>
          — “{{ preferredMeaning(latest.meanings) }}”
          <template v-if="latestShownOn"> · {{ latestShownOn }}</template>
        </span>
        <span class="ml-auto font-sans text-[11px] font-semibold uppercase tracking-[0.08em] text-[var(--color-text-faint)]">
          {{ t('home.library.latestTag') }}
        </span>
      </NuxtLink>
      <div v-else class="pb-6" />
    </section>

  </div>
</template>

<style scoped>
/* The hero tiles rise in one after another, spelling the word letter by
   letter. A single orchestrated moment; stilled for reduced motion. */
.word-tile {
  animation: word-tile-rise 0.55s cubic-bezier(0.2, 0.7, 0.3, 1) both;
}
.word-tile:nth-child(1) { animation-delay: 0.05s; }
.word-tile:nth-child(2) { animation-delay: 0.15s; }
.word-tile:nth-child(3) { animation-delay: 0.25s; }
.word-tile:nth-child(4) { animation-delay: 0.35s; }

@keyframes word-tile-rise {
  from {
    opacity: 0;
    transform: translateY(14px);
  }
}

@media (prefers-reduced-motion: reduce) {
  .word-tile {
    animation: none;
  }
}
</style>
