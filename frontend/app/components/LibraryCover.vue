<script setup lang="ts">
import type { LibraryBook, LibraryPlaceLabel } from '~/utils/libraryPlaces'

/**
 * One book on the Library shelf (Owner, 2026-11-01, cover "M2"): leather in the book's colour, a
 * gold frame, the book's picture in an arched gold plate, its Syriac title in gold. A red ribbon
 * hangs over the top when the reader has a place in the book, with that place written under the
 * count. Wide on a phone, a tall book on a computer.
 */
const props = defineProps<{
  book: LibraryBook
  to: string
  title: string
  syriacTitle: string
  subtitle: string
  count: string | null
  /** The reader has a place in this book (the ribbon hangs even before the place has a name). */
  placed: boolean
  place: LibraryPlaceLabel | null
}>()

const { t } = useI18n()
</script>

<template>
  <NuxtLink :to="props.to" class="cover no-underline" :class="`cover--${props.book}`">
    <span v-if="props.placed" class="ribbon" aria-hidden="true" />
    <span class="plate" aria-hidden="true">
      <!-- Lexicon: an open book. -->
      <svg v-if="props.book === 'lexicon'" viewBox="0 0 64 64" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M32 18c-6-4-14-5-22-4v34c8-1 16 0 22 4 6-4 14-5 22-4V14c-8-1-16 0-22 4z" /><path d="M32 18v34" /><path d="M15 24c4-.5 8 0 12 1.5M15 31c4-.5 8 0 12 1.5M15 38c4-.5 8 0 12 1.5M37 25.5c4-1.5 8-2 12-1.5M37 32.5c4-1.5 8-2 12-1.5M37 39.5c4-1.5 8-2 12-1.5" /></svg>
      <!-- Beth Gazo: a harp. -->
      <svg v-else-if="props.book === 'beth-gazo'" viewBox="0 0 64 64" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M19 55V13" /><path d="M19 13c7-6 14-4 20 0s9 4 12 1" /><path d="M51 14L25 53" stroke-width="3" /><path d="M25 14.5v37M31 11.6v30.5M37 12.4v21M43 16.2v10" /><path d="M15 55h14" /><circle cx="19" cy="10" r="1.6" fill="currentColor" /></svg>
      <!-- Beth Ṣalme: a figure in an arched frame. -->
      <svg v-else viewBox="0 0 64 64" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M16 56V28a16 16 0 0 1 32 0v28z" /><circle cx="32" cy="27" r="9" stroke-dasharray="2 2.5" /><circle cx="32" cy="28" r="5" /><path d="M23 52c1-8 5-13 9-13s8 5 9 13" /></svg>
    </span>
    <span class="text">
      <span class="syriac" dir="rtl" lang="syc">{{ props.syriacTitle }}</span>
      <span class="title">{{ props.title }}</span>
      <span class="subtitle">{{ props.subtitle }}</span>
      <span v-if="props.count" class="count">{{ props.count }}</span>
      <span v-if="props.place?.title" class="left">
        <span class="sr-only">{{ t('library.shelf.leftOff') }}: </span>
        <span v-if="props.place.syriac" class="left-syriac" dir="rtl" lang="syc">{{ props.place.syriac }}</span>
        <template v-if="props.place.syriac"> · </template>
        <b v-if="!props.place.syriac">{{ props.place.title }}</b>
        <template v-else>{{ props.place.title }}</template>
        <template v-if="props.place.subtitle"> · {{ props.place.subtitle }}</template>
      </span>
    </span>
  </NuxtLink>
</template>

<style scoped>
.cover {
  --gold: #c9a24c;
  --gold-soft: rgb(201 162 76 / 0.45);
  position: relative;
  display: grid;
  grid-template-columns: 88px 1fr;
  gap: 14px;
  align-items: center;
  min-height: 124px;
  padding: 16px 18px 16px 30px;
  border-radius: 4px 12px 12px 4px;
  color: #f3e7cc;
  /* The spine: a darker band on the left, catching a little light at its edge. */
  box-shadow: 0 6px 14px rgb(0 0 0 / 0.2), inset 10px 0 0 rgb(0 0 0 / 0.22), inset 12px 0 0 rgb(255 255 255 / 0.06);
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.cover:hover { transform: translateY(-2px); box-shadow: 0 10px 22px rgb(0 0 0 / 0.24), inset 10px 0 0 rgb(0 0 0 / 0.22), inset 12px 0 0 rgb(255 255 255 / 0.06); }
.cover:focus-visible { outline: 2px solid var(--gold); outline-offset: 3px; }
/* The gold-tooled frame. */
.cover::before {
  content: "";
  position: absolute;
  inset: 9px 9px 9px 20px;
  border: 1.5px solid var(--gold-soft);
  outline: 1px solid rgb(201 162 76 / 0.25);
  outline-offset: -5px;
  border-radius: 3px 8px 8px 3px;
  pointer-events: none;
}
.cover--lexicon { background: #134f4b; }
.cover--beth-gazo { background: #4f3474; }
.cover--beth-salme { background: #3f5a41; }

/* The red ribbon: this book has a saved place. */
.ribbon {
  position: absolute;
  top: -4px;
  right: 22px;
  z-index: 2;
  width: 18px;
  height: 46px;
  background: #8c2f39;
  clip-path: polygon(0 0, 100% 0, 100% 100%, 50% 82%, 0 100%);
  filter: drop-shadow(0 2px 2px rgb(0 0 0 / 0.3));
}

.plate {
  display: grid;
  place-items: center;
  aspect-ratio: 1;
  border: 1.5px solid var(--gold);
  border-radius: 50% 50% 6px 6px / 40% 40% 6px 6px;
  background: rgb(255 255 255 / 0.08);
  box-shadow: inset 0 0 0 4px rgb(0 0 0 / 0.12);
  color: var(--gold);
}
.plate svg { width: 56%; height: 56%; }

.text { display: flex; min-width: 0; flex-direction: column; }
.syriac { order: 1; display: block; margin-top: 2px; font-family: var(--font-syriac); font-size: 19px; font-weight: 500; color: var(--gold); }
.title { order: 0; font-family: var(--font-serif); font-size: 20px; font-weight: 600; letter-spacing: 0.02em; }
.subtitle { order: 2; display: none; margin-top: 2px; font-family: var(--font-serif); font-size: 14px; font-style: italic; opacity: 0.75; }
.count { order: 3; margin-top: 8px; font-family: var(--font-sans); font-size: 11px; font-weight: 600; letter-spacing: 0.14em; text-transform: uppercase; color: var(--gold); }
.left { order: 4; display: block; margin-top: 8px; font-family: var(--font-serif); font-size: 13px; line-height: 1.35; overflow-wrap: anywhere; }
.left b { font-weight: 600; }
.left-syriac { font-family: var(--font-syriac); font-size: 15px; }

@media (min-width: 768px) {
  .cover {
    grid-template-columns: 1fr;
    justify-items: center;
    align-content: start;
    min-height: 370px;
    padding: 28px 22px 26px 34px;
    text-align: center;
  }
  .ribbon { right: 26px; width: 22px; height: 64px; }
  .plate { width: 128px; }
  .text { align-items: center; }
  /* On the tall book the Syriac title sits under the plate, above the English. */
  .syriac { order: 0; margin-top: 16px; font-size: 26px; }
  .title { order: 1; font-size: 21px; }
  .subtitle { display: block; }
}
</style>
