<script setup lang="ts">
import type { CircuitGame } from '~/composables/useDailyCircuit'

// The home page's game section (owner, 2026-10-15; reworked 2026-10-23): the
// five apps as a deck of cards, the rest of the section following the game on
// top — its label, name, description and button — on a soft tint of its colour.
// Each card carries the game's little board.
//
// The deck moves only when the visitor moves it (owner, 2026-10-23: a card that
// changed every 2 s was too fast to read and could move under a click). On a
// touch screen you swipe the top card — left for the next game, right for the
// previous — and a line under the deck says so. With a mouse, ‹ › buttons sit
// under the deck instead and the hint is gone: nobody expects to drag a card
// with a mouse, though it still works. The game badges between them jump
// straight to a game on both.
type DeckGame = CircuitGame

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

const TUCK_MS = 240
/** How far (px) a card must be dragged sideways before letting go deals it. */
const SWIPE_PX = 70

/** Every game shows played / new: Sofro's word of the day counts since 2026-10-28. */
function playedToday(id: DeckGame) {
  return hasPlayed(id)
}

// The deck opens on today's next unplayed game, Meltho once the day is done.
const top = ref(Math.max(0, games.findIndex((g) => g.id === (nextUnplayed() ?? 'meltho'))))
/** The card flying off the deck, and to which side. */
const leaving = ref<{ index: number, side: 'left' | 'right' } | null>(null)
/** How far the top card is being dragged right now (px), while a swipe is live. */
const dragX = ref<number | null>(null)

/** How far a card sits from the top of the deck (0 = on top). */
function depth(index: number) {
  return (index - top.value + games.length) % games.length
}

function cardStyle(index: number) {
  const d = depth(index)
  const style: Record<string, string | number> = {
    zIndex: 10 - d,
    '--card-c': `var(--color-${games[index]!.id})`,
  }
  if (d === 0 && dragX.value !== null) {
    style.transform = `translate(${dragX.value}px, ${Math.abs(dragX.value) * 0.05}px) rotate(${dragX.value / 18}deg)`
  } else if (leaving.value?.index !== index) {
    style.transform = `translate(${d * 12}px, ${d * 10}px) rotate(${d * 2}deg)`
  }
  return style
}

function show(index: number) {
  top.value = (index + games.length) % games.length
}

let tuck: ReturnType<typeof setTimeout> | undefined

/** Deal: the top card flies off to one side, then tucks in at the back. */
function fling(side: 'left' | 'right') {
  if (leaving.value !== null) return
  leaving.value = { index: top.value, side }
  tuck = setTimeout(() => {
    leaving.value = null
    show(top.value + (side === 'left' ? 1 : -1))
  }, TUCK_MS)
}

function next() {
  fling('left')
}

function previous() {
  if (leaving.value === null) show(top.value - 1)
}

function jump(index: number) {
  if (leaving.value === null) show(index)
}

// Swiping. A drag only becomes a swipe once it is clearly sideways, so a
// vertical drag still scrolls the page (the cards are `touch-action: pan-y`).
let swipe: { x: number, y: number, pointer: number, sideways: boolean | null } | null = null
/** Set when a swipe just ended, so the click that follows doesn't open the game. */
let swiped = false

function onPointerDown(event: PointerEvent, index: number) {
  if (depth(index) !== 0 || leaving.value !== null || event.button !== 0) return
  swipe = { x: event.clientX, y: event.clientY, pointer: event.pointerId, sideways: null }
  swiped = false
}

function onPointerMove(event: PointerEvent) {
  if (!swipe || event.pointerId !== swipe.pointer) return
  const dx = event.clientX - swipe.x
  const dy = event.clientY - swipe.y
  if (swipe.sideways === null && Math.abs(dx) + Math.abs(dy) > 6) {
    swipe.sideways = Math.abs(dx) > Math.abs(dy)
    if (swipe.sideways) (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
  }
  if (swipe.sideways) {
    swiped = true
    dragX.value = dx
  }
}

function onPointerEnd() {
  if (!swipe) return
  const dx = dragX.value ?? 0
  swipe = null
  dragX.value = null
  if (Math.abs(dx) > SWIPE_PX) fling(dx < 0 ? 'left' : 'right')
}

function onCardClick(event: MouseEvent) {
  if (!swiped) return
  event.preventDefault()
  swiped = false
}

onBeforeUnmount(() => clearTimeout(tuck))

// Meltho's board: its own letters, the lomad and olaf as gold "present" tiles
// with those two swapped in position. Decorative, like every board here.
const melthoTiles = [
  { letter: 'ܡ', state: 'correct' },
  { letter: 'ܐ', state: 'present' },
  { letter: 'ܬ', state: 'correct' },
  { letter: 'ܠ', state: 'present' },
] as const

// Mno's: the name ܡܢܐ read as an equation (40 + 50 × 1), drawn the way Mno
// draws a guess (owner, 2026-10-23): five equal tiles, every one — the signs too
// — scored in Mno's own colours: green right, amber a number in the wrong place,
// pale grey a wrong one. No value hints, so it reads at a glance.
const mnoTiles = [
  { glyph: 'ܡ', operator: false, state: 'correct' },
  { glyph: '+', operator: true, state: 'correct' },
  { glyph: 'ܢ', operator: false, state: 'present' },
  { glyph: '×', operator: true, state: 'correct' },
  { glyph: 'ܐ', operator: false, state: 'absent' },
] as const

// Shmo's: three real scored cells from a guess row (Jacob of Serugh guessed
// against Ephrem the Syrian) — hit, near with its arrow, miss.
const shmoTiles = [
  { value: 'Patristic', state: 'hit', arrow: null },
  { value: '4th c.', state: 'near', arrow: '↓' },
  { value: 'Bishop', state: 'miss', arrow: null },
] as const

// Sofro's: a practice sheet — letters written on ruled lines, the next ones
// waiting in pale grey (owner's pick, 2026-10-23).
const sofroLetters = [
  { letter: 'ܐ', written: true },
  { letter: 'ܒ', written: true },
  { letter: 'ܓ', written: true },
  { letter: 'ܕ', written: false },
  { letter: 'ܗ', written: false },
] as const
</script>

<template>
  <section
    class="deck"
    :style="{ '--deck-c': `var(--color-${games[top]!.id})`, '--deck-on': `var(--color-on-${games[top]!.id}, #fff)` }"
    :aria-label="t('home.deck.label')"
    aria-roledescription="carousel"
  >
    <div class="min-w-0">
      <div class="deck-stack">
        <a
          v-for="(game, index) in games"
          :key="game.id"
          :href="game.url"
          class="deck-card"
          :class="{
            'is-dragging': depth(index) === 0 && dragX !== null,
            'is-leaving-left': leaving?.index === index && leaving.side === 'left',
            'is-leaving-right': leaving?.index === index && leaving.side === 'right',
          }"
          :style="cardStyle(index)"
          :aria-hidden="depth(index) !== 0 ? 'true' : undefined"
          :tabindex="depth(index) === 0 ? undefined : -1"
          draggable="false"
          @pointerdown="onPointerDown($event, index)"
          @pointermove="onPointerMove"
          @pointerup="onPointerEnd"
          @pointercancel="onPointerEnd"
          @dragstart.prevent
          @click.capture="onCardClick"
        >
          <span class="flex items-center gap-2.5">
            <span class="deck-badge font-syriac" :class="{ 'is-wide': game.badge.length > 1 && game.id === 'sofro' }" dir="rtl" aria-hidden="true">{{ game.badge }}</span>
            <span>
              <span class="flex items-center gap-2 font-sans text-[1.05rem] font-semibold text-[var(--color-text)]">
                {{ t(`home.${game.id}.heading`) }}
                <span v-if="playedToday(game.id)" class="text-sm font-bold text-green-700 dark:text-green-400" :title="t('home.playedToday')">✓</span>
                <span v-else class="size-2 rounded-full bg-green-500 ring-4 ring-green-500/20" :title="t(`home.${game.id}.live`)" />
              </span>
              <span class="block font-sans text-xs text-[var(--color-text-muted)]">{{ t(`home.${game.id}.kind`) }}</span>
            </span>
          </span>

          <!-- The game's little board, decorative; the heading carries the name. -->
          <span class="deck-board" aria-hidden="true">
            <span v-if="game.id === 'meltho'" class="flex gap-1.5" dir="rtl">
              <span v-for="tile in melthoTiles" :key="tile.letter" class="deck-tile font-syriac" :class="tile.state === 'present' ? 'is-gold' : 'is-meltho'">{{ tile.letter }}</span>
            </span>
            <span v-else-if="game.id === 'mno'" class="flex gap-1.5" dir="rtl">
              <span v-for="(tile, tileIndex) in mnoTiles" :key="tileIndex" class="deck-tile" :class="[`is-mno-${tile.state}`, tile.operator ? 'font-sans text-base font-semibold' : 'font-syriac']">{{ tile.glyph }}</span>
            </span>
            <span v-else-if="game.id === 'shmo'" class="flex flex-wrap gap-1.5">
              <span v-for="tile in shmoTiles" :key="tile.value" class="deck-cell" :class="`is-${tile.state}`">
                {{ tile.value }}<span v-if="tile.arrow" class="font-bold">{{ tile.arrow }}</span>
              </span>
            </span>
            <span v-else-if="game.id === 'nahlo'" class="flex flex-wrap gap-1.5">
              <span class="deck-part is-right">{{ t('home.deck.melody') }}<b>Zodeq dnehwe</b></span>
              <span class="deck-part is-right">{{ t('home.deck.mode') }}<b>3</b></span>
              <span class="deck-part is-wrong">{{ t('home.deck.shuhlofo') }}<b>{{ t('home.deck.no') }}</b></span>
            </span>
            <span v-else class="deck-sheet" dir="rtl">
              <span v-for="item in sofroLetters" :key="item.letter" class="deck-sheet-letter font-syriac" :class="{ 'is-ghost': !item.written }">{{ item.letter }}</span>
            </span>
          </span>

          <span class="font-sans text-[0.8rem] font-semibold text-[var(--card-c)]">{{ t(`home.deck.${game.verb}`) }} →</span>
        </a>
      </div>

      <!-- Under the deck: ‹ › with a mouse, the badges always, the swipe hint on touch. -->
      <div class="deck-nav">
        <button type="button" class="deck-arrow" :aria-label="t('home.deck.previous')" @click="previous">‹</button>
        <span class="flex gap-1.5">
          <button
            v-for="(game, index) in games"
            :key="game.id"
            type="button"
            class="deck-pip font-syriac"
            :class="{ 'is-on': index === top, 'is-done': playedToday(game.id), 'is-wide': game.badge.length > 1 && game.id === 'sofro' }"
            :style="{ '--pip-c': `var(--color-${game.id})`, '--pip-on': `var(--color-on-${game.id}, #fff)` }"
            :aria-label="t('home.deck.show', { game: t(`home.${game.id}.heading`) })"
            :aria-current="index === top ? 'true' : undefined"
            @click="jump(index)"
          ><span dir="rtl" aria-hidden="true">{{ game.badge }}</span></button>
        </span>
        <button type="button" class="deck-arrow" :aria-label="t('home.deck.next')" @click="next">›</button>
      </div>
      <p class="deck-hint font-sans">{{ t('home.deck.swipeHint') }}</p>
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
            {{ t(`home.${game.id}.kind`) }} · {{ t('home.deck.daily') }}
          </p>
          <h2 class="mt-1.5 flex flex-wrap items-baseline gap-x-3 gap-y-1 font-serif text-[1.75rem] font-semibold sm:text-[2.1rem]">
            {{ t(`home.${game.id}.heading`) }}
            <SyriacText :text="game.word" class="text-2xl font-normal text-[var(--color-text-faint)]" />
            <span
              v-if="playedToday(game.id)"
              class="self-center text-sm font-bold text-green-700 dark:text-green-400"
              :title="t('home.playedToday')"
              :aria-label="t('home.playedToday')"
            >✓</span>
          </h2>
          <p class="mb-5 mt-2.5 max-w-xl font-serif text-base leading-relaxed text-[var(--color-text-muted)] sm:text-[1.05rem]">
            {{ t(`home.${game.id}.body`) }}
          </p>

          <a :href="game.url" class="deck-cta">{{ t(`home.${game.id}.cta`) }} →</a>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.deck {
  position: relative;
  display: grid;
  gap: 1.25rem;
  align-items: center;
  margin-top: 1.25rem;
  padding: 1.25rem 1rem 1.5rem;
  border-radius: 1.375rem;
  background: color-mix(in oklab, var(--deck-c) 9%, var(--color-bg));
  transition: background-color 0.6s;
}
@media (min-width: 768px) {
  .deck {
    grid-template-columns: 23.5rem minmax(0, 1fr);
    gap: 2.25rem;
    padding: 1.875rem 2rem;
  }
}

.deck-stack {
  position: relative;
  height: 12.5rem;
  width: calc(100% - 3.5rem);
  /* The back cards fan out ~3rem below the top one: room for them above the
     badges. */
  margin-bottom: 4rem;
}
@media (min-width: 768px) {
  .deck-stack { width: 20rem; height: 13.5rem; }
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
  /* Vertical drags still scroll the page; sideways ones swipe. */
  touch-action: pan-y;
  user-select: none;
  -webkit-user-select: none;
  transition: transform 0.3s cubic-bezier(0.3, 0.7, 0.3, 1), opacity 0.24s;
}
.deck-card.is-dragging { transition: none; cursor: grabbing; }
.deck-card::before {
  content: "";
  position: absolute;
  inset: 0 0 auto;
  height: 3px;
  background: linear-gradient(to right, var(--card-c), transparent);
}
/* Dealt: out to one side first, then (back in .deck-card's own transition)
   tucked in at the back of the deck. */
.deck-card.is-leaving-left {
  transform: translate(-120%, 10px) rotate(-14deg);
  opacity: 0;
  transition-duration: 0.24s;
}
.deck-card.is-leaving-right {
  transform: translate(120%, 10px) rotate(14deg);
  opacity: 0;
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
.deck-badge.is-wide { font-size: 0.8rem; }
.deck-board {
  display: flex;
  flex: 1;
  align-items: flex-end;
  padding-bottom: 0.125rem;
}
.deck-board > [dir="rtl"] { width: 100%; justify-content: flex-end; }

.deck-info.is-current { animation: deck-swap 0.45s ease both; }
@keyframes deck-swap {
  from { opacity: 0; transform: translateY(8px); }
}

.deck-tile {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.5rem;
  border: 2px solid transparent;
  border-radius: 0.5rem;
  color: #fff;
  font-size: 1.25rem;
  line-height: 1;
}
/* Mno's five tiles must fit the card on the narrowest phones. */
@media (max-width: 380px) {
  .deck-tile { width: 2rem; }
}
.deck-tile.is-meltho { background: var(--color-meltho); border-color: var(--color-meltho-dark); }
.deck-tile.is-gold { background: var(--color-meltho-gold); border-color: var(--color-meltho-gold-dark); }
/* Mno's green is a literal, as on the live cards: this preview is the only
   place outside Mno's repo that needs it. Its "absent" grey is the hub's own
   border tones, which are Mno's to within a shade in both themes. */
.deck-tile.is-mno-correct { background: #4e9c56; border-color: #3d7e44; }
.deck-tile.is-mno-present { background: var(--color-mno); border-color: var(--color-mno-dark); }
.deck-tile.is-mno-absent { background: var(--color-border); border-color: var(--color-border-strong); color: var(--color-text-faint); }

.deck-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  height: 2.25rem;
  padding: 0 0.5rem;
  border: 2px solid transparent;
  border-radius: 0.5625rem;
  color: #fff;
  font: 600 0.7rem var(--font-sans);
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
  padding: 0.3125rem 0.5rem;
  border-radius: 0.5rem;
  text-align: center;
  font: 600 0.6rem var(--font-sans);
}
.deck-part b { font-size: 0.75rem; }
.deck-part.is-right { background: var(--color-nahlo); color: #fff; }
.deck-part.is-wrong { background: var(--color-nahlo-faint); color: var(--color-nahlo); }

/* Sofro's practice sheet: a dashed top line and a solid baseline. */
.deck-sheet {
  display: flex;
  gap: 0.625rem;
  align-items: flex-end;
  padding: 0.25rem 0.375rem 0;
  background:
    linear-gradient(var(--color-border-strong), var(--color-border-strong)) left 0 bottom 0.5rem / 100% 1.5px no-repeat,
    repeating-linear-gradient(90deg, var(--color-border) 0 4px, transparent 4px 8px) left 0 top 0.625rem / 100% 1px no-repeat;
}
.deck-sheet-letter {
  font-size: 2rem;
  line-height: 1.3;
  color: var(--color-sofro);
}
.deck-sheet-letter.is-ghost { color: var(--color-border-strong); }

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

/* Under the deck, centred on the cards (the fan sticks out ~3rem right). */
.deck-nav {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.625rem;
  width: calc(100% - 0.5rem);
}
@media (min-width: 768px) {
  .deck-nav { width: 23rem; }
}

.deck-arrow {
  display: none;
  flex: none;
  place-items: center;
  width: 2.125rem;
  height: 2.125rem;
  border: 1px solid var(--color-border);
  border-radius: 9999px;
  background: var(--color-bg-elevated);
  color: var(--deck-c);
  font: 600 1.125rem/1 var(--font-sans);
  cursor: pointer;
  box-shadow: var(--shadow-soft);
  transition: transform 0.15s, color 0.6s;
}
.deck-arrow:first-child:hover { transform: translateX(-2px); }
.deck-arrow:last-child:hover { transform: translateX(2px); }
.deck-arrow:focus-visible { outline: 2px solid var(--deck-c); outline-offset: 2px; }

.deck-hint {
  display: none;
  margin: 0.625rem 0 0;
  width: calc(100% - 0.5rem);
  text-align: center;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}

/* A mouse gets the arrows; a touch screen gets the swipe hint. */
@media (hover: hover) and (pointer: fine) {
  .deck-arrow { display: grid; }
}
@media (pointer: coarse) {
  .deck-hint { display: block; }
}

.deck-pip {
  position: relative;
  display: grid;
  place-items: center;
  width: 2.125rem;
  height: 2.125rem;
  border: 1px solid var(--color-border);
  border-radius: 0.625rem;
  background: var(--color-bg-elevated);
  color: var(--color-text-muted);
  font-size: 1rem;
  line-height: 1;
  cursor: pointer;
  transition: transform 0.15s, background-color 0.2s;
}
.deck-pip.is-wide { font-size: 0.75rem; }
.deck-pip:hover { transform: translateY(-1px); }
.deck-pip.is-on {
  border-color: var(--pip-c);
  background: var(--pip-c);
  color: var(--pip-on);
  box-shadow: 0 0 0 3px color-mix(in oklab, var(--pip-c) 20%, transparent);
}
.deck-pip.is-done::after {
  content: "✓";
  position: absolute;
  right: -0.3rem;
  bottom: -0.3rem;
  width: 0.875rem;
  height: 0.875rem;
  border-radius: 9999px;
  background: #4e9c56;
  color: #fff;
  font: 700 0.5625rem/0.875rem var(--font-sans);
  text-align: center;
}
.deck-pip:focus-visible { outline: 2px solid var(--deck-c); outline-offset: 2px; }

@media (prefers-reduced-motion: reduce) {
  .deck,
  .deck-card,
  .deck-arrow,
  .deck-pip { transition: none; }
  .deck-info { animation: none; }
}
</style>
