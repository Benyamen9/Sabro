<script setup lang="ts">
const { t, locale } = useI18n()
const { melthoUrl, mnoUrl, shmoUrl, nahloUrl } = useRuntimeConfig().public
const { listWords } = useMelthoLibrary()
const preferredMeaning = usePreferredMeaning()

// The daily circuit: which games were finished today (shared cookie written
// by the games). The hero's lead button follows the next unplayed game; the
// game deck below reads the same cookie for its played / new marks.
const { nextUnplayed } = useDailyCircuit()
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
  if (nextGame.value === 'nahlo') return { href: nahloUrl, label: 'home.nahlo.cta' }
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
          <template #nahlo>
            <strong class="font-semibold text-[var(--color-text)]">Nahlo</strong>
          </template>
          <template #sofro>
            <strong class="font-semibold text-[var(--color-text)]">Sofro</strong>
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

    <!-- Live now: the five apps as a self-dealing deck (GameDeck). -->
    <section class="mt-14 sm:mt-16">
      <div class="border-b border-[var(--color-border)] pb-3.5">
        <span class="font-sans text-xs font-semibold uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
          {{ t('home.liveNow') }}
        </span>
      </div>

      <GameDeck />
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
