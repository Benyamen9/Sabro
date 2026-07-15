<script setup lang="ts">
import type { GameStats } from '~/composables/usePlayStats'

/**
 * One game's stats block on the profile page — streaks, totals, and the guess
 * histogram, coloured in the game's own palette (games speak their colour,
 * not the hub's burgundy). Data comes from the parent: one results fetch
 * serves every game's card.
 */
const props = defineProps<{
  stats: GameStats | null
  loading: boolean
  loaded: boolean
  heading: string
  subtitle: string
  empty: string
  playUrl: string
  palette: 'meltho' | 'mno'
}>()

const { t, n } = useI18n()

const hasPlayed = computed(() => Boolean(props.stats && props.stats.played > 0))

// The streak leads (it's the habit the game builds), best streak accented,
// then the totals. Tone per tile via the game's palette. Class strings stay
// literal per palette so Tailwind sees them at build time.
const tiles = computed(() => {
  const s = props.stats
  if (!s) return []
  return [
    { key: 'currentStreak', label: t('account.stats.currentStreak'), value: String(s.currentStreak), tone: 'hero' },
    { key: 'maxStreak', label: t('account.stats.maxStreak'), value: String(s.maxStreak), tone: 'accent' },
    { key: 'played', label: t('account.stats.played'), value: String(s.played), tone: 'plain' },
    { key: 'winRate', label: t('account.stats.winRate'), value: `${s.winRate}%`, tone: 'plain' },
  ]
})

const paletteClasses: Record<'meltho' | 'mno', Record<string, string>> = {
  meltho: {
    hero: 'border-[color-mix(in_oklab,var(--color-meltho)_35%,var(--color-border))] bg-[var(--color-meltho-faint)] [&>dd]:text-[var(--color-meltho)]',
    accent: 'border-[var(--color-border)] bg-[var(--color-bg)] [&>dd]:text-[var(--color-meltho-gold)]',
    plain: 'border-[var(--color-border)] bg-[var(--color-bg)]',
    bar: 'bg-[var(--color-meltho)]',
    dot: 'bg-[var(--color-meltho)]',
  },
  mno: {
    hero: 'border-[color-mix(in_oklab,var(--color-mno)_35%,var(--color-border))] bg-[var(--color-mno-faint)] [&>dd]:text-[var(--color-mno)]',
    accent: 'border-[var(--color-border)] bg-[var(--color-bg)] [&>dd]:text-[var(--color-mno-dark)]',
    plain: 'border-[var(--color-border)] bg-[var(--color-bg)]',
    bar: 'bg-[var(--color-mno)]',
    dot: 'bg-[var(--color-mno)]',
  },
}

const classes = computed(() => paletteClasses[props.palette])

// Whole days between today and a yyyy-mm-dd date, parsed at UTC midnight so the
// integer gap is stable (matches the streak math in usePlayStats).
function daysSince(date: string): number {
  const now = new Date()
  const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
  return Math.round((Date.parse(`${today}T00:00:00Z`) - Date.parse(`${date}T00:00:00Z`)) / 86_400_000)
}

// A small recency line so the card reflects live activity, not a static block.
const lastPlayedLabel = computed(() => {
  const last = props.stats?.lastPlayed
  if (!last) return null
  const gap = daysSince(last)
  if (gap <= 0) return t('account.stats.lastPlayedToday')
  if (gap === 1) return t('account.stats.lastPlayedYesterday')
  return t('account.stats.lastPlayedDaysAgo', { count: gap })
})

// Width % for a histogram bar, with a floor so a non-zero count is always visible.
function barWidth(value: number): string {
  const max = props.stats?.maxBucket ?? 0
  if (value === 0 || max === 0) return '0%'
  return `${Math.max(12, Math.round((value / max) * 100))}%`
}
</script>

<template>
  <section
    class="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
  >
    <div class="flex items-baseline justify-between gap-3">
      <div>
        <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
          {{ heading }}
        </h2>
        <p class="mt-0.5 font-sans text-sm text-[var(--color-text-muted)]">
          {{ subtitle }}
        </p>
      </div>
      <a
        :href="playUrl"
        class="inline-flex shrink-0 items-center gap-1 rounded-full bg-[var(--color-accent-faint)] px-3 py-1 font-sans text-xs font-medium text-[var(--color-accent)] no-underline transition-colors hover:bg-[color-mix(in_oklab,var(--color-accent-faint)_70%,var(--color-accent)_12%)]"
      >
        {{ t('account.stats.play') }}
        <span aria-hidden="true">→</span>
      </a>
    </div>

    <p
      v-if="loading && !loaded"
      class="mt-6 font-sans text-sm text-[var(--color-text-muted)]"
    >{{ t('common.loading') }}</p>

    <!-- Empty: signed in but no games recorded yet. -->
    <div
      v-else-if="!hasPlayed"
      class="mt-5 rounded-xl border border-dashed border-[var(--color-border-strong)] px-5 py-6 text-center"
    >
      <p class="font-sans text-sm text-[var(--color-text-muted)]">{{ empty }}</p>
    </div>

    <template v-else-if="stats">
      <!-- Recency signal — when the player last took the daily puzzle. -->
      <p
        v-if="lastPlayedLabel"
        class="mt-4 inline-flex items-center gap-1.5 font-sans text-xs text-[var(--color-text-muted)]"
      >
        <span class="size-1.5 shrink-0 rounded-full" :class="classes.dot" aria-hidden="true" />
        {{ lastPlayedLabel }}
      </p>

      <!-- Headline tiles. -->
      <dl class="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div
          v-for="tile in tiles"
          :key="tile.key"
          class="rounded-xl border px-3 py-3 text-center"
          :class="classes[tile.tone]"
        >
          <dd class="font-sans text-2xl font-bold tabular-nums text-[var(--color-text)]">{{ tile.value }}</dd>
          <dt class="mt-0.5 font-sans text-[11px] uppercase tracking-[0.05em] text-[var(--color-text-faint)]">{{ tile.label }}</dt>
        </div>
      </dl>

      <!-- Guess distribution — the wins bucketed by attempt count. -->
      <div class="mt-6">
        <div class="flex items-baseline justify-between">
          <p class="font-sans text-sm font-medium text-[var(--color-text)]">{{ t('account.stats.distribution') }}</p>
          <p
            v-if="stats.wins > 0"
            class="font-sans text-xs text-[var(--color-text-muted)]"
          >{{ t('account.stats.averageGuesses', { value: n(stats.averageGuesses, { maximumFractionDigits: 1 }) }) }}</p>
        </div>
        <ul class="mt-3 space-y-1.5">
          <li
            v-for="(count, i) in stats.distribution"
            :key="i"
            class="flex items-center gap-2.5"
          >
            <span class="w-3 shrink-0 text-right font-sans text-xs tabular-nums text-[var(--color-text-muted)]">{{ i + 1 }}</span>
            <div class="flex-1">
              <div
                class="flex h-5 items-center justify-end rounded-sm px-1.5 font-sans text-xs font-medium tabular-nums text-white transition-all"
                :class="classes.bar"
                :style="{ width: barWidth(count) }"
              >
                <span v-if="count > 0">{{ count }}</span>
              </div>
            </div>
          </li>
        </ul>
      </div>
    </template>
  </section>
</template>
