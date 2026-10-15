<script setup lang="ts">
import type { CircuitGame } from '~/composables/useDailyCircuit'

// The home page's game section (owner, 2026-10-15): the five apps as a deck of
// cards that deals itself every 2 s — the top card tucks in behind the others —
// while the rest of the section follows the game on top: its label, name,
// description, little board and button, on a soft tint of its colour. Arrows sit
// on the section's edges, not on the cards. Pointing at the section (or focusing
// inside it) pauses the deal, so a click never lands on a card that just moved;
// with reduced motion it never deals by itself at all.
type DeckGame = CircuitGame | 'sofro'

interface DeckEntry {
  id: DeckGame
  /** The app's badge glyph(s), as in the app launcher. */
  badge: string
  /** The word the name transliterates, shown beside it. */
  word: string
  url: string
  verb: 'play' | 'listen' | 'write'
}

const { t } = useI18n()
const config = useRuntimeConfig().public
const { hasPlayed, nextUnplayed } = useDailyCircuit()

const games: readonly DeckEntry[] = [
  { id: 'meltho', badge: 'ܡ', word: 'ܡܠܬܐ', url: config.melthoUrl as string, verb: 'play' },
  { id: 'mno', badge: 'ܐ݈', word: 'ܡܢܐ', url: config.mnoUrl as string, verb: 'play' },
  { id: 'shmo', badge: 'ܫ', word: 'ܫܡܐ', url: config.shmoUrl as string, verb: 'play' },
  { id: 'nahlo', badge: 'ܢ', word: 'ܢܚܠܐ', url: config.nahloUrl as string, verb: 'listen' },
  { id: 'sofro', badge: 'ܣܦ', word: 'ܣܦܪܐ', url: config.sofroUrl as string, verb: 'write' },
]

const DEAL_MS = 2000
const TUCK_MS = 240

/** Daily games show played / new; Sofro is free practice and never "played". */
function playedToday(id: DeckGame) {
  return id !== 'sofro' && hasPlayed(id)
}

// The deck opens on today's next unplayed game, Meltho once the day is done.
const top = ref(Math.max(0, games.findIndex((g) => g.id === (nextUnplayed() ?? 'meltho'))))
const current = computed(() => games[top.value]!)
const leaving = ref<number | null>(null)
const cycle = ref(0)
const paused = ref(false)
// Off on the server and until mount, and for good with reduced motion.
const autoplay = ref(false)

/** How far a card sits from the top of the deck (0 = on top). */
function depth(index: number) {
  return (index - top.value + games.length) % games.length
}

function cardStyle(index: number) {
  const d = depth(index)
  return {
    transform: `translate(${d * 12}px, ${d * 10}px) rotate(${d * 2}deg)`,
    zIndex: 10 - d,
    '--card-c': `var(--color-${games[index]!.id})`,
  }
}

function show(index: number) {
  top.value = (index + games.length) % games.length
  cycle.value++
}

let tuck: ReturnType<typeof setTimeout> | undefined

/** Deal: the top card slides out, then tucks in at the back of the deck. */
function next() {
  if (leaving.value !== null) return
  leaving.value = top.value
  tuck = setTimeout(() => {
    leaving.value = null
    show(top.value + 1)
  }, TUCK_MS)
}

function previous() {
  if (leaving.value === null) show(top.value - 1)
}

function jump(index: number) {
  if (leaving.value === null) show(index)
}

function onTimerEnd() {
  if (!paused.value) next()
}

onMounted(() => {
  autoplay.value = !window.matchMedia('(prefers-reduced-motion: reduce)').matches
})
onBeforeUnmount(() => clearTimeout(tuck))

// Meltho's board: its own letters, the lomad and olaf as gold "present" tiles
// with those two swapped in position. Decorative, like every board here.
const melthoTiles = [
  { letter: 'ܡ', state: 'correct' },
  { letter: 'ܐ', state: 'present' },
  { letter: 'ܬ', state: 'correct' },
  { letter: 'ܠ', state: 'present' },
] as const

// Mno's: the name ܡܢܐ read as an equation (40 + 50 × 1), in Mno's own
// feedback colours — green correct, its gold for the wrong place.
const mnoTiles = [
  { glyph: 'ܡ', hint: '40', state: 'correct' },
  { glyph: '+', hint: '', state: 'operatorHit' },
  { glyph: 'ܢ', hint: '50', state: 'present' },
  { glyph: '×', hint: '', state: 'operator' },
  { glyph: 'ܐ', hint: '1', state: 'correct' },
] as const

// Shmo's: three real scored cells from a guess row (Jacob of Serugh guessed
// against Ephrem the Syrian) — hit, near with its arrow, miss.
const shmoTiles = [
  { value: 'Patristic', state: 'hit', arrow: null },
  { value: '4th c.', state: 'near', arrow: '↓' },
  { value: 'Bishop', state: 'miss', arrow: null },
] as const
</script>

<template>
  <section
    class="deck"
    :class="{ 'is-paused': paused }"
    :style="{ '--deck-c': `var(--color-${current.id})`, '--deck-on': `var(--color-on-${current.id}, #fff)` }"
    :aria-label="t('home.deck.label')"
    aria-roledescription="carousel"
    @mouseenter="paused = true"
    @mouseleave="paused = false"
    @focusin="paused = true"
    @focusout="paused = false"
  >
    <button type="button" class="deck-arrow deck-arrow-prev" :aria-label="t('home.deck.previous')" @click="previous">‹</button>
    <button type="button" class="deck-arrow deck-arrow-next" :aria-label="t('home.deck.next')" @click="next">›</button>

    <div class="deck-stack">
      <a
        v-for="(game, index) in games"
        :key="game.id"
        :href="game.url"
        class="deck-card"
        :class="{ 'is-leaving': leaving === index }"
        :style="cardStyle(index)"
        :aria-hidden="depth(index) !== 0 ? 'true' : undefined"
        :tabindex="depth(index) === 0 ? undefined : -1"
      >
        <span class="flex items-center gap-2.5">
          <span class="deck-badge font-syriac" dir="rtl" aria-hidden="true">{{ game.badge }}</span>
          <span>
            <span class="flex items-center gap-2 font-sans text-[1.05rem] font-semibold text-[var(--color-text)]">
              {{ t(`home.${game.id}.heading`) }}
              <span v-if="playedToday(game.id)" class="text-sm font-bold text-green-700 dark:text-green-400" :title="t('home.playedToday')">✓</span>
              <span v-else-if="game.id === 'sofro'" class="deck-any">{{ t('home.deck.anyTime') }}</span>
              <span v-else class="size-2 rounded-full bg-green-500 ring-4 ring-green-500/20" :title="t(`home.${game.id}.live`)" />
            </span>
            <span class="block font-sans text-xs text-[var(--color-text-muted)]">{{ t(`home.${game.id}.kind`) }}</span>
          </span>
        </span>
        <span class="flex-1 font-serif text-[0.95rem] leading-snug text-[var(--color-text-muted)]">{{ t(`home.${game.id}.short`) }}</span>
        <span class="font-sans text-[0.8rem] font-semibold text-[var(--card-c)]">{{ t(`home.deck.${game.verb}`) }} →</span>
      </a>
    </div>

    <div class="min-w-0">
      <!-- All five game texts share one grid cell and only the top one shows, so
           the section keeps the height of the longest in every language and the
           page below never jumps when a card is dealt. -->
      <div class="grid">
        <div
          v-for="(game, index) in games"
          :key="game.id"
          class="deck-info [grid-area:1/1]"
          :class="index === top ? 'is-current' : 'invisible'"
          :aria-hidden="index === top ? undefined : 'true'"
        >
          <p class="font-sans text-xs font-semibold uppercase tracking-[0.14em] text-[var(--deck-c)]">
            {{ t(`home.${game.id}.kind`) }} · {{ game.id === 'sofro' ? t('home.deck.anyTime') : t('home.deck.daily') }}
          </p>
          <h2 class="mt-1.5 flex flex-wrap items-baseline gap-x-3 gap-y-1 font-serif text-3xl font-semibold sm:text-[2.1rem]">
            {{ t(`home.${game.id}.heading`) }}
            <SyriacText :text="game.word" class="text-2xl font-normal text-[var(--color-text-faint)]" />
            <span
              v-if="playedToday(game.id)"
              class="self-center text-sm font-bold text-green-700 dark:text-green-400"
              :title="t('home.playedToday')"
              :aria-label="t('home.playedToday')"
            >✓</span>
          </h2>
          <p class="mt-2.5 max-w-xl font-serif text-[1.05rem] leading-relaxed text-[var(--color-text-muted)]">
            {{ t(`home.${game.id}.body`) }}
          </p>

          <!-- The game's little board, decorative; the heading carries the name. -->
          <div class="mt-4 mb-5 flex min-h-11 items-center" aria-hidden="true">
            <div v-if="game.id === 'meltho'" class="flex gap-1.5" dir="rtl">
              <span v-for="tile in melthoTiles" :key="tile.letter" class="deck-tile font-syriac" :class="tile.state === 'present' ? 'is-gold' : 'is-meltho'">{{ tile.letter }}</span>
            </div>
            <div v-else-if="game.id === 'mno'" class="flex gap-1.5" dir="rtl">
              <span v-for="(tile, tileIndex) in mnoTiles" :key="tileIndex" class="deck-tile" :class="`is-mno-${tile.state}`">
                <span :class="tile.state.startsWith('operator') ? 'font-sans text-lg font-semibold' : 'font-syriac'">{{ tile.glyph }}</span>
                <span v-if="tile.hint" class="font-sans text-[9px] leading-none opacity-85" dir="ltr">{{ tile.hint }}</span>
              </span>
            </div>
            <div v-else-if="game.id === 'shmo'" class="flex flex-wrap gap-1.5">
              <span v-for="tile in shmoTiles" :key="tile.value" class="deck-cell" :class="`is-${tile.state}`">
                {{ tile.value }}<span v-if="tile.arrow" class="font-bold">{{ tile.arrow }}</span>
              </span>
            </div>
            <div v-else-if="game.id === 'nahlo'" class="flex flex-wrap gap-1.5">
              <span class="deck-part is-right">{{ t('home.deck.melody') }}<b>Zodeq dnehwe</b></span>
              <span class="deck-part is-right">{{ t('home.deck.mode') }}<b>3</b></span>
              <span class="deck-part is-wrong">{{ t('home.deck.shuhlofo') }}<b>{{ t('home.deck.no') }}</b></span>
            </div>
            <div v-else class="flex gap-1.5" dir="rtl">
              <span v-for="(letter, letterIndex) in ['ܐ', 'ܒ', 'ܓ', 'ܕ']" :key="letter" class="deck-trace font-syriac" :class="{ 'is-inked': letterIndex < 2 }">{{ letter }}</span>
            </div>
          </div>

          <a :href="game.url" class="deck-cta">{{ t(`home.${game.id}.cta`) }} →</a>
        </div>
      </div>

      <div class="mt-6 flex items-center gap-4">
        <div class="flex gap-1.5">
          <button
            v-for="(game, index) in games"
            :key="game.id"
            type="button"
            class="deck-pip font-syriac"
            :class="{ 'is-on': index === top }"
            :style="{ '--pip-c': `var(--color-${game.id})` }"
            :aria-label="t('home.deck.show', { game: t(`home.${game.id}.heading`) })"
            :aria-current="index === top ? 'true' : undefined"
            @click="jump(index)"
          ><span dir="rtl" aria-hidden="true">{{ game.badge }}</span></button>
        </div>
        <span class="deck-timer" aria-hidden="true">
          <span :key="cycle" class="deck-timer-fill" :class="{ 'is-running': autoplay }" :style="{ animationDuration: `${DEAL_MS}ms` }" @animationend="onTimerEnd" />
        </span>
      </div>
    </div>
  </section>
</template>

<style scoped>
.deck {
  position: relative;
  display: grid;
  gap: 1.5rem;
  align-items: center;
  margin-top: 1.25rem;
  padding: 1.25rem 1rem 1.5rem;
  border-radius: 1.375rem;
  background: color-mix(in oklab, var(--deck-c) 9%, var(--color-bg));
  transition: background-color 0.6s;
}
@media (min-width: 768px) {
  .deck {
    grid-template-columns: 25.5rem minmax(0, 1fr);
    gap: 2.25rem;
    padding: 1.875rem 2rem;
  }
}

/* The arrows sit on the section's own edges, half outside it. */
.deck-arrow {
  position: absolute;
  top: 50%;
  z-index: 30;
  display: grid;
  place-items: center;
  width: 2.5rem;
  height: 2.5rem;
  margin-top: -1.25rem;
  border: 1px solid var(--color-border);
  border-radius: 9999px;
  background: var(--color-bg-elevated);
  color: var(--deck-c);
  font: 600 1.25rem/1 var(--font-sans);
  cursor: pointer;
  box-shadow: var(--shadow-soft);
  transition: transform 0.15s, color 0.6s;
}
.deck-arrow-prev { left: -1.25rem; }
.deck-arrow-next { right: -1.25rem; }
.deck-arrow-prev:hover { transform: translateX(-3px); }
.deck-arrow-next:hover { transform: translateX(3px); }
.deck-arrow:focus-visible { outline: 2px solid var(--deck-c); outline-offset: 2px; }
@media (max-width: 767px) {
  /* Stacked layout: level with the middle of the deck (section padding + half
     the stack), not the middle of the much taller section, where the text is. */
  .deck-arrow { top: 8.75rem; width: 2.125rem; height: 2.125rem; margin-top: -1.0625rem; font-size: 1.05rem; }
  .deck-arrow-prev { left: -0.75rem; }
  .deck-arrow-next { right: -0.75rem; }
}

.deck-stack {
  position: relative;
  height: 15rem;
  width: calc(100% - 3.5rem);
  /* The back cards fan out ~3rem below the top one: room for them when the
     text sits underneath the deck. */
  margin-bottom: 2.75rem;
}
@media (min-width: 768px) {
  .deck-stack { width: 20rem; margin-bottom: 0; }
}

.deck-card {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  overflow: hidden;
  padding: 1.125rem 1.125rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  background: var(--color-bg-elevated);
  box-shadow: var(--shadow-soft);
  text-decoration: none;
  transition: transform 0.3s cubic-bezier(0.3, 0.7, 0.3, 1);
}
.deck-card::before {
  content: "";
  position: absolute;
  inset: 0 0 auto;
  height: 3px;
  background: linear-gradient(to right, var(--card-c), transparent);
}
/* Dealt: out to the side first, then (back in .deck-card's own transition)
   tucked in at the back of the deck. */
.deck-card.is-leaving {
  transform: translate(-64%, -8px) rotate(-8deg) !important;
  transition-duration: 0.24s;
}
.deck-badge {
  display: grid;
  flex: none;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.625rem;
  background: var(--card-c);
  color: #fff;
  font-size: 1.05rem;
  line-height: 1;
}
.deck-any {
  border: 1px solid var(--color-border);
  border-radius: 9999px;
  padding: 0.125rem 0.375rem;
  font-size: 0.55rem;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--color-text-muted);
}

.deck-info.is-current { animation: deck-swap 0.45s ease both; }
@keyframes deck-swap {
  from { opacity: 0; transform: translateY(8px); }
}

.deck-tile {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.875rem;
  border: 2px solid transparent;
  border-radius: 0.5625rem;
  color: #fff;
  font-size: 1.4rem;
  line-height: 1;
}
.deck-tile.is-meltho { background: var(--color-meltho); border-color: var(--color-meltho-dark); }
.deck-tile.is-gold { background: var(--color-meltho-gold); border-color: var(--color-meltho-gold-dark); }
/* Mno's green is a literal, as on the live cards: this preview is the only
   place outside Mno's repo that needs it. */
.deck-tile.is-mno-correct,
.deck-tile.is-mno-operatorHit { background: #4e9c56; border-color: #3d7e44; }
.deck-tile.is-mno-present { background: var(--color-mno); border-color: var(--color-mno-dark); }
.deck-tile.is-mno-operator { background: var(--color-bg-subtle); border-color: var(--color-border-strong); color: var(--color-text-muted); }
.deck-tile.is-mno-operator,
.deck-tile.is-mno-operatorHit { width: 1.75rem; }

.deck-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  height: 2.5rem;
  padding: 0 0.625rem;
  border: 2px solid transparent;
  border-radius: 0.625rem;
  color: #fff;
  font: 600 0.75rem var(--font-sans);
}
.deck-cell.is-hit { background: var(--color-shmo); border-color: color-mix(in oklab, var(--color-shmo) 70%, black); }
/* Shmo's own "near" amber, a literal like on the live card. */
.deck-cell.is-near { background: #c9b043; border-color: #ab9538; }
.deck-cell.is-miss { background: var(--color-bg-subtle); border-color: var(--color-border-strong); color: var(--color-text-faint); }

/* Nahlo marks a right part in its own purple, a wrong one in a pale tint
   (owner, 2026-10-12) — never the shared green. */
.deck-part {
  display: grid;
  gap: 1px;
  padding: 0.375rem 0.625rem;
  border-radius: 0.5625rem;
  text-align: center;
  font: 600 0.625rem var(--font-sans);
}
.deck-part b { font-size: 0.8rem; }
.deck-part.is-right { background: var(--color-nahlo); color: #fff; }
.deck-part.is-wrong { background: var(--color-nahlo-faint); color: var(--color-nahlo); }

.deck-trace {
  display: grid;
  place-items: center;
  width: 2.5rem;
  height: 2.875rem;
  border: 2px dashed var(--color-border-strong);
  border-radius: 0.5625rem;
  color: var(--color-border-strong);
  font-size: 1.55rem;
  line-height: 1;
}
.deck-trace.is-inked { border-style: solid; border-color: var(--color-sofro); color: var(--color-sofro); }

.deck-cta {
  display: inline-flex;
  align-items: center;
  padding: 0.75rem 1.25rem;
  border-radius: 0.75rem;
  background: var(--deck-c);
  color: var(--deck-on);
  font: 600 0.875rem var(--font-sans);
  text-decoration: none;
  box-shadow: 0 1px 2px rgb(0 0 0 / 0.12);
  transition: filter 0.15s;
}
.deck-cta:hover { filter: brightness(0.92); }

.deck-pip {
  display: grid;
  place-items: center;
  width: 1.625rem;
  height: 1.625rem;
  border: 0;
  border-radius: 0.5rem;
  background: var(--pip-c);
  color: #fff;
  font-size: 0.75rem;
  line-height: 1;
  opacity: 0.32;
  cursor: pointer;
  transition: opacity 0.2s, transform 0.2s;
}
.deck-pip.is-on { opacity: 1; transform: scale(1.12); }
.deck-pip:hover { opacity: 0.7; }
.deck-pip:focus-visible { outline: 2px solid var(--deck-c); outline-offset: 2px; }

.deck-timer {
  flex: none;
  width: 8.75rem;
  height: 3px;
  overflow: hidden;
  border-radius: 2px;
  background: var(--color-border);
}
.deck-timer-fill {
  display: block;
  width: 0;
  height: 100%;
  background: var(--deck-c);
}
.deck-timer-fill.is-running { animation: deck-fill linear forwards; }
.deck.is-paused .deck-timer-fill { animation-play-state: paused; }
@keyframes deck-fill {
  to { width: 100%; }
}

@media (prefers-reduced-motion: reduce) {
  .deck,
  .deck-card,
  .deck-arrow,
  .deck-pip { transition: none; }
  .deck-info { animation: none; }
}
</style>
