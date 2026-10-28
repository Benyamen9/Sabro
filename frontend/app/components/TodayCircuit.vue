<script setup lang="ts">
import { CIRCUIT_GAMES, circuitToday, type CircuitGame } from '~/composables/useDailyCircuit'

// The home page's "Today" line (owner, 2026-10-23). One row says what to play
// next and starts it; under it, a square per daily game shows what's done —
// Sofro's word of the day the fifth since 2026-10-28. Once all five are done,
// the row invites the player to Sofro's free practice, with a countdown to the
// next puzzles and, for a signed-in player, how many days in a row they have
// done all five (owner, 2026-10-28: Sofro's word counts too, though its results
// only began that day, so every streak started again from it).
// On a wide screen the two halves sit side by side. Played/next state comes
// from the shared daily-circuit cookie, so it renders the same on the server.
// A signed-in player's saved results fill the cookie in too, so a round played
// on the phone shows as done on the computer without opening the game.
const { t } = useI18n()
const config = useRuntimeConfig().public
const { hasPlayed, nextUnplayed, markPlayed } = useDailyCircuit()
const { isSignedIn } = useAuth()
const { results, load: loadResults, circuitStreak } = usePlayStats()

const badges: Record<CircuitGame, string> = { meltho: 'ܡ', mno: 'ܐ݈', shmo: 'ܫ', nahlo: 'ܢ', sofro: 'ܣܦ' }
const urls: Record<CircuitGame, string> = {
  meltho: config.melthoUrl as string,
  mno: config.mnoUrl as string,
  shmo: config.shmoUrl as string,
  nahlo: config.nahloUrl as string,
  sofro: config.sofroUrl as string,
}

const next = computed(() => nextUnplayed())
const doneCount = computed(() => CIRCUIT_GAMES.filter((game) => hasPlayed(game)).length)

function squareLabel(game: CircuitGame) {
  const name = t(`home.${game}.heading`)
  if (hasPlayed(game)) return `${name} — ${t('home.today.done')}`
  if (game === next.value) return `${name} — ${t('home.today.upNextShort')}`
  return name
}

// The streak: only for a signed-in player (only their results are saved), shown
// from the first day (owner, 2026-10-23).
const streak = computed(() => {
  if (next.value !== null || !isSignedIn.value) return 0
  return circuitStreak(CIRCUIT_GAMES, circuitToday()) ?? 0
})

// The countdown to the next puzzles (midnight UTC, when the puzzle date
// changes). Client-only: the server's clock and the visitor's minute differ.
const countdown = ref<string | null>(null)
let tick: ReturnType<typeof setInterval> | undefined

function updateCountdown() {
  const now = new Date()
  const midnight = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1)
  const minutes = Math.max(0, Math.floor((midnight - now.getTime()) / 60_000))
  countdown.value = t('home.today.hoursMinutes', { h: Math.floor(minutes / 60), m: String(minutes % 60).padStart(2, '0') })
}

// Rounds this player saved today, wherever they played them, count as played.
watch(results, (saved) => {
  const today = circuitToday()
  for (const result of saved ?? []) {
    const game = result.gameId as CircuitGame
    if (result.playedOn === today && CIRCUIT_GAMES.includes(game)) markPlayed(game, today)
  }
}, { immediate: true })

watch(next, (game) => {
  if (game !== null || tick) return
  updateCountdown()
  tick = setInterval(updateCountdown, 30_000)
})

onMounted(() => {
  if (isSignedIn.value) loadResults()
  if (next.value === null) {
    updateCountdown()
    tick = setInterval(updateCountdown, 30_000)
  }
})
onBeforeUnmount(() => clearInterval(tick))
</script>

<template>
  <section class="today" :aria-label="t('home.today.heading')">
    <!-- Up next: the next daily game and one button that starts it. -->
    <a
      v-if="next"
      :href="urls[next]"
      class="today-up"
      :style="{ '--step-c': `var(--color-${next})`, '--step-on': `var(--color-on-${next}, #fff)` }"
    >
      <span class="today-badge font-syriac" dir="rtl" aria-hidden="true">{{ badges[next] }}</span>
      <span class="min-w-0 flex-1">
        <span class="today-kicker">{{ t('home.today.upNext') }}</span>
        <span class="block font-serif text-base font-semibold leading-tight text-[var(--color-text)]">
          {{ t(`home.${next}.heading`) }} — {{ t(`home.today.task.${next}`) }}
        </span>
      </span>
      <span class="today-go">{{ t(next === 'nahlo' ? 'home.deck.listen' : next === 'sofro' ? 'home.deck.write' : 'home.deck.play') }}</span>
    </a>

    <!-- The day is done: keep writing in Sofro, and when the next puzzles come. -->
    <a
      v-else
      :href="urls.sofro"
      class="today-up is-all-done"
      :style="{ '--step-c': 'var(--color-sofro)', '--step-on': 'var(--color-on-sofro, #fff)' }"
    >
      <span class="today-badge is-wide font-syriac" dir="rtl" aria-hidden="true">ܣܦ</span>
      <span class="min-w-0 flex-1">
        <span class="today-kicker">
          <template v-if="streak >= 1">{{ t('home.today.allDoneShort') }} · <span class="today-streak">🔥 {{ t('home.today.streak', { days: streak }, streak) }}</span></template>
          <template v-else>{{ t('home.today.allDone') }}</template>
        </span>
        <span class="block font-serif text-base font-semibold leading-tight text-[var(--color-text)]">{{ t('home.today.keepGoing') }}</span>
        <!-- scope="global": this component owns no local i18n scope (see index.vue). -->
        <i18n-t
          v-if="countdown"
          keypath="home.today.newPuzzlesIn"
          scope="global"
          tag="span"
          class="mt-0.5 block font-sans text-xs tabular-nums text-[var(--color-text-muted)]"
        >
          <template #time>
            <span class="whitespace-nowrap">{{ countdown }}</span>
          </template>
        </i18n-t>
      </span>
      <span class="today-go">{{ t('home.deck.write') }}</span>
    </a>

    <!-- What's done: a square per daily game, Sofro's word last. -->
    <div class="today-squares font-sans">
      <a
        v-for="game in CIRCUIT_GAMES"
        :key="game"
        :href="urls[game]"
        class="today-square font-syriac"
        :class="{ 'is-done': hasPlayed(game), 'is-next': game === next, 'is-wide': game === 'sofro' && !hasPlayed(game) }"
        :style="{ '--step-c': `var(--color-${game})`, '--step-on': `var(--color-on-${game}, #fff)` }"
        :title="squareLabel(game)"
        :aria-label="squareLabel(game)"
      ><span dir="rtl" aria-hidden="true">{{ hasPlayed(game) ? '✓' : badges[game] }}</span></a>
      <span class="ml-0.5 whitespace-nowrap">{{ t('home.today.progress', { done: doneCount, total: CIRCUIT_GAMES.length }) }}</span>
    </div>
  </section>
</template>

<style scoped>
.today {
  margin-top: 1.5rem;
  padding: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background: var(--color-bg-elevated);
  box-shadow: var(--shadow-soft);
}

.today-up {
  display: flex;
  flex: 1;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
  text-decoration: none;
}
.today-badge {
  display: grid;
  flex: none;
  place-items: center;
  width: 2.625rem;
  height: 2.625rem;
  border-radius: 0.75rem;
  background: var(--step-c);
  color: var(--step-on, #fff);
  font-size: 1.25rem;
  line-height: 1;
}
.today-badge.is-wide { font-size: 0.8rem; }
.today-kicker {
  display: block;
  font: 600 0.65625rem var(--font-sans);
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--step-c);
}
.today-up.is-all-done .today-kicker { color: #3d7e44; }
.today-streak { color: #c2410c; }
.today-go {
  flex: none;
  padding: 0.5625rem 1rem;
  border-radius: 0.625rem;
  background: var(--step-c);
  color: var(--step-on, #fff);
  font: 600 0.84375rem var(--font-sans);
  white-space: nowrap;
  transition: filter 0.15s;
}
.today-up:hover .today-go { filter: brightness(0.92); }

.today-squares {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  margin-top: 0.625rem;
  padding-top: 0.625rem;
  border-top: 1px solid var(--color-border);
  font-size: 0.71875rem;
  color: var(--color-text-muted);
}
.today-square {
  display: grid;
  flex: none;
  place-items: center;
  width: 1.5rem;
  height: 1.5rem;
  border: 1.5px solid color-mix(in oklab, var(--step-c) 45%, var(--color-bg-elevated));
  border-radius: 0.4375rem;
  color: var(--step-c);
  font-size: 0.7rem;
  line-height: 1;
  text-decoration: none;
  transition: transform 0.15s;
}
a.today-square:hover { transform: translateY(-1px); }
.today-square.is-done {
  border-color: #4e9c56;
  background: #4e9c56;
  color: #fff;
  font: 700 0.7rem/1 var(--font-sans);
}
.today-square.is-next {
  border-color: var(--step-c);
  background: var(--step-c);
  color: var(--step-on, #fff);
}
.today-square.is-wide { font-size: 0.5625rem; }

/* A wide screen: the two halves side by side, nothing stretched thin. */
@media (min-width: 768px) {
  .today {
    display: flex;
    align-items: center;
    gap: 1.375rem;
    padding: 0.75rem 1rem;
  }
  .today-squares {
    align-self: stretch;
    margin: 0;
    padding: 0 0 0 1.375rem;
    border-top: 0;
    border-left: 1px solid var(--color-border);
  }
}

@media (prefers-reduced-motion: reduce) {
  .today-square { transition: none; }
}
</style>
