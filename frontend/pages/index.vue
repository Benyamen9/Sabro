<script setup lang="ts">
const { t, locale } = useI18n()
const { melthoUrl, mnoUrl } = useRuntimeConfig().public
const { listWords } = useMelthoLibrary()
const preferredMeaning = usePreferredMeaning()

// The daily circuit: which games were finished today (shared cookie written
// by Meltho and Mno). Played doors get a check, and the hero's primary CTA
// points at the next unplayed game so the day continues where it left off.
const { hasPlayed, nextUnplayed } = useDailyCircuit()
const melthoPlayed = computed(() => hasPlayed('meltho'))
const mnoPlayed = computed(() => hasPlayed('mno'))
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

// Meltho's door demo: a scored board row — teal "correct" tiles with lomad
// and olaf as gold "present" tiles. Decorative, aria-hidden; the heading
// carries the name for assistive tech.
const melthoTiles = [
  { letter: 'ܡ', state: 'correct' },
  { letter: 'ܐ', state: 'present' },
  { letter: 'ܬ', state: 'correct' },
  { letter: 'ܠ', state: 'present' },
] as const

function tileClass(state: 'correct' | 'present') {
  const base
    = 'flex size-10 items-center justify-center rounded-lg border-2 text-xl text-white'
  return state === 'present'
    ? `${base} border-[var(--color-meltho-gold-dark)] bg-[var(--color-meltho-gold)]`
    : `${base} border-[var(--color-meltho-dark)] bg-[var(--color-meltho)]`
}

// Mno's door demo: the opening of an equation in Syriac numerals, value
// hints showing — the game's signature. Decorative, like Meltho's row.
const mnoTiles = [
  { glyph: 'ܝ', hint: '10' },
  { glyph: 'ܒ', hint: '2' },
  { glyph: '×', hint: '' },
  { glyph: 'ܗ', hint: '5' },
] as const

// The newest past word, the library door's living proof. Best-effort: with
// the API unreachable the door simply renders without it.
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
  = 'inline-flex items-center gap-2 rounded-xl bg-[var(--color-accent)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(140_47_57/0.25)] transition-colors hover:bg-[var(--color-accent-hover)]'
const ghostButton
  = 'inline-flex items-center gap-2 rounded-xl border border-[var(--color-border-strong)] px-4 py-3 font-sans text-sm font-medium text-[var(--color-text)] no-underline transition-colors hover:bg-[var(--color-bg-subtle)]'

// The hero's lead button follows the circuit: today's next unplayed game, or
// the library once both are done.
function heroButton(target: 'meltho' | 'mno' | 'library') {
  const isPrimary = nextGame.value === target || (nextGame.value === null && target === 'library')
  return isPrimary ? primaryButton : ghostButton
}
</script>

<template>
  <div>
    <!-- Hero: what this place is, in plain words — and the name itself as
         the first tiny lesson, in the same card anatomy the library uses. -->
    <section class="grid items-center gap-10 pt-2 sm:pt-6 lg:grid-cols-[minmax(0,1fr)_auto] lg:gap-12">
      <div class="max-w-2xl">
        <p class="flex items-baseline gap-2 font-sans text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-accent)]">
          <SyriacText text="ܣܒܪܐ" class="!text-[15px] normal-case tracking-normal" />
          {{ t('home.eyebrow') }}
        </p>

        <h1 class="mt-5 font-serif text-4xl font-semibold leading-[1.06] tracking-[-0.02em] sm:text-5xl md:text-[3.4rem]">
          {{ t('home.title') }}
        </h1>

        <p class="mt-6 max-w-xl font-serif text-lg leading-relaxed text-[var(--color-text-muted)] sm:text-xl">
          {{ t('home.sub') }}
        </p>

        <div class="mt-8 flex flex-wrap gap-3">
          <a :href="melthoUrl" :class="heroButton('meltho')">{{ t('home.meltho.cta') }} →</a>
          <a :href="mnoUrl" :class="heroButton('mno')">{{ t('home.mno.cta') }} →</a>
          <NuxtLink to="/library" :class="heroButton('library')">{{ t('home.ctaLibrary') }}</NuxtLink>
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
        <p class="mx-auto mt-4 max-w-[36ch] font-sans text-sm text-[var(--color-text-muted)]">
          {{ t('home.boardCaption') }}
        </p>
      </div>
    </section>

    <!-- The three doors: each explains itself in plain speech. Played games
         carry the day's check mark (the shared circuit cookie). -->
    <section class="mt-14 sm:mt-16">
      <div class="border-b border-[var(--color-border)] pb-3.5">
        <span class="font-sans text-xs font-semibold uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
          {{ t('home.doorsHeading') }}
        </span>
      </div>

      <div class="mt-5 grid gap-5 lg:grid-cols-3">
        <!-- Meltho — the word game. -->
        <div class="flex flex-col rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]">
          <p class="flex items-center gap-2.5 font-sans text-[10.5px] font-bold uppercase tracking-[0.12em] text-[var(--color-meltho)]">
            {{ t('home.meltho.kind') }}
            <span v-if="melthoPlayed" class="inline-flex items-center gap-1 text-green-700 dark:text-green-400">✓ {{ t('home.playedToday') }}</span>
            <span v-else class="inline-flex items-center gap-1.5 text-green-700 dark:text-green-400">
              <span class="size-1.5 rounded-full bg-green-500 ring-4 ring-green-500/20" />
              {{ t('home.live') }}
            </span>
          </p>
          <h2 class="mt-2.5 font-sans text-[22px]">{{ t('home.meltho.heading') }}</h2>
          <p class="mt-1.5 font-serif text-[15px] leading-relaxed text-[var(--color-text-muted)]">
            {{ t('home.meltho.body') }}
          </p>
          <div class="mt-5 flex gap-1.5" dir="rtl" aria-hidden="true">
            <span v-for="tile in melthoTiles" :key="tile.letter" :class="tileClass(tile.state)">
              <SyriacText :text="tile.letter" class="leading-none" />
            </span>
          </div>
          <a
            :href="melthoUrl"
            class="mt-5 inline-flex items-center gap-1.5 font-sans text-sm font-bold text-[var(--color-meltho)] no-underline transition-opacity hover:opacity-80"
          >
            {{ t('home.meltho.cta') }} →
          </a>
        </div>

        <!-- Mno — the numbers game. -->
        <div class="flex flex-col rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]">
          <p class="flex items-center gap-2.5 font-sans text-[10.5px] font-bold uppercase tracking-[0.12em] text-[var(--color-mno-dark)]">
            {{ t('home.mno.kind') }}
            <span v-if="mnoPlayed" class="inline-flex items-center gap-1 text-green-700 dark:text-green-400">✓ {{ t('home.playedToday') }}</span>
            <span v-else class="inline-flex items-center gap-1.5 text-green-700 dark:text-green-400">
              <span class="size-1.5 rounded-full bg-green-500 ring-4 ring-green-500/20" />
              {{ t('home.live') }}
            </span>
          </p>
          <h2 class="mt-2.5 font-sans text-[22px]">{{ t('home.mno.heading') }}</h2>
          <p class="mt-1.5 font-serif text-[15px] leading-relaxed text-[var(--color-text-muted)]">
            {{ t('home.mno.body') }}
          </p>
          <div class="mt-5 flex gap-1.5" dir="rtl" aria-hidden="true">
            <span
              v-for="(tile, index) in mnoTiles"
              :key="index"
              class="flex size-10 flex-col items-center justify-center rounded-lg border-2 border-[var(--color-mno-dark)] bg-[var(--color-mno)] text-white"
            >
              <SyriacText :text="tile.glyph" class="text-lg leading-none" />
              <span v-if="tile.hint" class="mt-0.5 font-sans text-[9px] leading-none opacity-80" dir="ltr">{{ tile.hint }}</span>
            </span>
          </div>
          <a
            :href="mnoUrl"
            class="mt-5 inline-flex items-center gap-1.5 font-sans text-sm font-bold text-[var(--color-mno-dark)] no-underline transition-opacity hover:opacity-80"
          >
            {{ t('home.mno.cta') }} →
          </a>
        </div>

        <!-- The library — the dictionary door, led by its newest entry. -->
        <div class="flex flex-col rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]">
          <p class="font-sans text-[10.5px] font-bold uppercase tracking-[0.12em] text-[var(--color-accent)]">
            {{ t('home.library.kind') }}
          </p>
          <h2 class="mt-2.5 font-sans text-[22px]">{{ t('home.library.heading') }}</h2>
          <p class="mt-1.5 font-serif text-[15px] leading-relaxed text-[var(--color-text-muted)]">
            {{ t('home.library.body') }}
          </p>
          <NuxtLink
            v-if="latest"
            :to="`/library/${latest.lexiconEntryId}`"
            class="mt-5 flex items-center gap-3 rounded-xl bg-[var(--color-bg-subtle)] px-4 py-2.5 no-underline transition-colors hover:bg-[var(--color-accent-faint)]"
          >
            <SyriacText :text="latest.syriacUnvocalized" class="!text-[1.7rem] leading-none text-[var(--color-accent)]" />
            <span class="font-sans text-xs text-[var(--color-text-muted)]">
              <strong v-if="latest.sblTransliteration" class="font-semibold text-[var(--color-text)]">{{ latest.sblTransliteration }}</strong>
              — “{{ preferredMeaning(latest.meanings) }}”
              <template v-if="latestShownOn"> · {{ latestShownOn }}</template>
            </span>
          </NuxtLink>
          <NuxtLink
            to="/library"
            class="mt-5 inline-flex items-center gap-1.5 font-sans text-sm font-bold text-[var(--color-accent)] no-underline transition-opacity hover:opacity-80"
          >
            {{ t('home.library.cta') }} →
          </NuxtLink>
        </div>
      </div>
    </section>

    <!-- The one tease: something new is coming. No name, no description. -->
    <div class="mt-6 flex flex-wrap items-baseline gap-x-4 gap-y-1.5 rounded-2xl border border-[var(--color-border)] border-l-4 border-l-[var(--color-soon)] bg-[var(--color-soon-faint)] px-6 py-4">
      <span class="font-sans text-[10.5px] font-extrabold uppercase tracking-[0.14em] text-[var(--color-soon)]">
        {{ t('home.soonTag') }}
      </span>
      <p class="font-serif text-[15px] text-[var(--color-text)]">{{ t('home.soonBody') }}</p>
    </div>

    <!-- New here? The three questions a first-time visitor actually has. -->
    <section class="mt-12">
      <div class="border-b border-[var(--color-border)] pb-3.5">
        <span class="font-sans text-xs font-semibold uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
          {{ t('home.newHereHeading') }}
        </span>
      </div>
      <div class="mt-5 grid gap-x-8 gap-y-6 sm:grid-cols-3">
        <div>
          <h3 class="font-serif text-[17px] font-semibold">{{ t('home.q1.q') }}</h3>
          <p class="mt-2 font-sans text-[13.5px] leading-relaxed text-[var(--color-text-muted)]">{{ t('home.q1.a') }}</p>
        </div>
        <div>
          <h3 class="font-serif text-[17px] font-semibold">{{ t('home.q2.q') }}</h3>
          <p class="mt-2 font-sans text-[13.5px] leading-relaxed text-[var(--color-text-muted)]">{{ t('home.q2.a') }}</p>
        </div>
        <div>
          <h3 class="font-serif text-[17px] font-semibold">{{ t('home.q3.q') }}</h3>
          <i18n-t keypath="home.q3.a" tag="p" class="mt-2 font-sans text-[13.5px] leading-relaxed text-[var(--color-text-muted)]">
            <template #link>
              <a :href="melthoUrl" class="font-semibold text-[var(--color-accent)] no-underline">{{ t('home.q3.link') }}</a>
            </template>
          </i18n-t>
        </div>
      </div>
      <p class="mt-8 text-right font-serif text-[13.5px] italic text-[var(--color-text-muted)]">
        {{ t('home.closing') }}
      </p>
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
