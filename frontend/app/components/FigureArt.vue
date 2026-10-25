<script setup lang="ts">
import type { HistoricalFigureListItem } from '~/types/api'
import { EMBLEM_PATHS, OPEN_EMBLEMS, ROLE_EMBLEM_PATHS } from '~/utils/emblems'
import { toneOf, type GalleryTier } from '~/utils/gallery'

/**
 * The picture on a Beth Ṣalme card: the figure's emblem (or its role's) in a
 * medallion, toned by category. Found is full colour, met is a sketch, not met
 * yet is a dark card. Decorative: the card around it carries the words.
 */
const props = withDefaults(
  defineProps<{
    figure: Pick<HistoricalFigureListItem, 'category' | 'role' | 'emblem'>
    tier: GalleryTier
    illuminated?: boolean
  }>(),
  { illuminated: false },
)

const path = computed(() =>
  props.figure.emblem ? EMBLEM_PATHS[props.figure.emblem] : ROLE_EMBLEM_PATHS[props.figure.role])
const open = computed(() => OPEN_EMBLEMS.has(props.figure.emblem ?? props.figure.role))
</script>

<template>
  <span class="figure-art" :class="[`tone-${toneOf(figure.category)}`, `tier-${tier}`]">
    <svg viewBox="0 0 120 120" aria-hidden="true" focusable="false">
      <rect class="ground" width="120" height="120" />
      <circle v-if="tier === 'found'" class="halo" cx="60" cy="62" r="40" />
      <circle class="medal" cx="60" cy="62" r="35" />
      <path class="mark" :class="{ open }" :d="path" />
    </svg>
    <span v-if="illuminated && tier === 'found'" class="foil" aria-hidden="true" />
  </span>
</template>

<style scoped>
.figure-art {
  position: relative;
  display: block;
  overflow: hidden;
}

.figure-art svg {
  display: block;
  width: 100%;
  height: auto;
}

.tone-ot { --tone-bg: var(--figure-ot-bg); --tone-mid: var(--figure-ot-mid); --tone-ink: var(--figure-ot-ink); }
.tone-nt { --tone-bg: var(--figure-nt-bg); --tone-mid: var(--figure-nt-mid); --tone-ink: var(--figure-nt-ink); }
.tone-pa { --tone-bg: var(--figure-pa-bg); --tone-mid: var(--figure-pa-mid); --tone-ink: var(--figure-pa-ink); }

.mark {
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Found: the full colour card. */
.tier-found .ground { fill: var(--tone-bg); }
.tier-found .halo { fill: none; stroke: var(--tone-mid); stroke-width: 2; }
.tier-found .medal { fill: color-mix(in oklab, var(--color-bg-elevated) 60%, transparent); }
.tier-found .mark { fill: var(--tone-mid); fill-opacity: 0.35; stroke: var(--tone-ink); stroke-width: 3; }

/* Met: a sketch on paper, waiting to be coloured in. */
.tier-met .ground { fill: var(--figure-paper); }
.tier-met .medal { fill: none; stroke: var(--tone-mid); stroke-width: 1.5; stroke-dasharray: 3 3; }
.tier-met .mark { fill: none; stroke: var(--tone-ink); stroke-width: 2; }

/* Not met yet: the dark card, its emblem only just showing. */
.tier-locked .ground { fill: var(--figure-locked-bg); }
.tier-locked .medal { fill: var(--figure-locked-mid); }
.tier-locked .mark { fill: none; stroke: var(--figure-locked-ink); stroke-width: 2.6; }

.mark.open { fill: none; }

/* Illuminated: a foil that drifts across the picture (Owner, 2026-10-25). */
.foil {
  position: absolute;
  inset: 0;
  pointer-events: none;
  mix-blend-mode: color-dodge;
  background: linear-gradient(
    115deg,
    transparent 22%,
    rgb(255 0 170 / 0.45) 32%,
    rgb(255 220 0 / 0.45) 41%,
    rgb(0 255 200 / 0.45) 50%,
    rgb(0 140 255 / 0.45) 59%,
    transparent 70%
  );
  background-size: 260% 260%;
  animation: foil-drift 3.6s linear infinite;
}

@keyframes foil-drift {
  from { background-position: 110% 0; }
  to { background-position: -160% 0; }
}

@media (prefers-reduced-motion: reduce) {
  .foil {
    animation: none;
    background-position: 30% 0;
  }
}
</style>
