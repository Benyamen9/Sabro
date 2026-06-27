<script setup lang="ts">
const { t } = useI18n()
const melthoUrl = useRuntimeConfig().public.melthoUrl

// The Syriac letter tiles teasing the word library (content, not UI strings).
const libraryTiles = ['ܐ', 'ܒ', 'ܓ', 'ܕ']

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

const primaryButton
  = 'inline-flex items-center gap-2 rounded-xl bg-[var(--color-accent)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(140_47_57/0.25)] transition-colors hover:bg-[var(--color-accent-hover)]'
const ghostButton
  = 'inline-flex items-center gap-2 rounded-xl border border-[var(--color-border-strong)] px-4 py-3 font-sans text-sm font-medium text-[var(--color-text)] no-underline transition-colors hover:bg-[var(--color-bg-subtle)]'
const sectionEyebrow
  = 'font-sans text-xs font-semibold uppercase tracking-[0.1em] text-[var(--color-text-faint)]'
</script>

<template>
  <div>
    <!-- Hero: the name's meaning ("hope") is surfaced as a legible word-line,
         not a decorative watermark. -->
    <section class="max-w-3xl pt-2 sm:pt-6">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('home.eyebrow') }}
      </p>

      <h1 class="mt-5 font-sans text-4xl leading-[1.05] tracking-[-0.015em] sm:text-5xl md:text-6xl">
        <span class="block font-serif font-normal italic">{{ t('home.titleLine1') }}</span>
        <span class="block">{{ t('home.titleLine2') }}</span>
      </h1>

      <div class="mt-5 flex items-center gap-3.5">
        <SyriacText text="ܣܒܪܐ" class="!text-[2rem] leading-none text-[var(--color-accent)]" />
        <span class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('home.nameGloss') }}</span>
      </div>

      <i18n-t
        keypath="home.lede"
        tag="p"
        class="mt-7 max-w-xl font-serif text-lg leading-relaxed text-[var(--color-text-muted)] sm:text-xl"
      >
        <template #app>
          <strong class="font-semibold text-[var(--color-text)]">Meltho</strong>
        </template>
      </i18n-t>

      <div class="mt-8 flex flex-wrap gap-3">
        <a :href="melthoUrl" :class="primaryButton">{{ t('home.meltho.ctaShort') }} →</a>
        <NuxtLink to="/library" :class="ghostButton">{{ t('home.exploreCta') }}</NuxtLink>
      </div>
    </section>

    <!-- Live now: Meltho, the launched app, as a real feature with a direct play CTA. -->
    <section class="mt-14 sm:mt-20">
      <div class="flex items-baseline justify-between border-b border-[var(--color-border)] pb-3.5">
        <span class="font-sans text-xs font-semibold uppercase tracking-[0.12em] text-[var(--color-text-muted)]">
          {{ t('home.liveNow') }}
        </span>
      </div>

      <div
        class="mt-5 flex flex-col items-center gap-6 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-7 text-center shadow-[var(--shadow-soft)] sm:flex-row sm:gap-7 sm:p-8 sm:text-left"
      >
        <!-- The game itself as the hero: a Meltho tile row spelling ܡܠܬܐ as a
             solved word (teal "correct" + gold "present"). On mobile it centres
             above the text; on desktop it sits to the left like a board. -->
        <div class="flex shrink-0 gap-1.5 sm:gap-2" dir="rtl" aria-hidden="true">
          <span v-for="tile in melthoTiles" :key="tile.letter" :class="tileClass(tile.state)">
            <SyriacText :text="tile.letter" class="leading-none" />
          </span>
        </div>

        <div class="flex-1">
          <h2 class="flex flex-wrap items-center justify-center gap-x-3 gap-y-1.5 font-sans text-2xl sm:justify-start">
            {{ t('home.meltho.heading') }}
            <span
              class="inline-flex items-center gap-1.5 font-sans text-[0.7rem] font-bold uppercase tracking-wider text-green-700 dark:text-green-400"
            >
              <span class="size-2 rounded-full bg-green-500 ring-4 ring-green-500/20" />
              {{ t('home.meltho.live') }}
            </span>
          </h2>
          <p class="mx-auto mt-2 max-w-md font-serif text-[var(--color-text-muted)] sm:mx-0">
            {{ t('home.meltho.body') }}
          </p>
        </div>

        <a
          :href="melthoUrl"
          class="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-[var(--color-meltho)] px-5 py-3 font-sans text-sm font-semibold text-white no-underline shadow-[0_1px_2px_rgb(0_0_0/0.12)] transition-colors hover:bg-[var(--color-meltho-dark)] sm:ml-auto sm:w-auto"
        >
          {{ t('home.meltho.cta') }} →
        </a>
      </div>
    </section>

    <!-- Two real destinations: the word library and a short "what Sabro is" note. -->
    <section class="mt-10 mb-4 grid gap-5 sm:grid-cols-[1.15fr_1fr]">
      <NuxtLink
        to="/library"
        class="group flex flex-col rounded-2xl border border-[var(--color-border)] p-7 no-underline transition-colors hover:border-[var(--color-border-strong)]"
      >
        <span :class="sectionEyebrow">{{ t('home.library.eyebrow') }}</span>
        <h3 class="mt-3 font-sans text-xl">{{ t('home.library.heading') }}</h3>
        <p class="mt-2.5 font-serif text-[var(--color-text-muted)]">{{ t('home.library.body') }}</p>
        <div class="mt-4 flex gap-1.5">
          <span
            v-for="letter in libraryTiles"
            :key="letter"
            class="flex h-11 w-9 items-center justify-center rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-elevated)]"
          >
            <SyriacText :text="letter" class="!text-xl text-[var(--color-accent)]" />
          </span>
        </div>
        <span class="mt-5 font-sans text-sm font-semibold text-[var(--color-accent)]">
          {{ t('home.library.cta') }} →
        </span>
      </NuxtLink>

      <div class="rounded-2xl bg-[var(--color-bg-subtle)] p-7">
        <span :class="sectionEyebrow">{{ t('home.about.eyebrow') }}</span>
        <h3 class="mt-3 font-sans text-xl">{{ t('home.about.heading') }}</h3>
        <p class="mt-2.5 font-serif text-[var(--color-text-muted)]">{{ t('home.about.body') }}</p>
      </div>
    </section>
  </div>
</template>
