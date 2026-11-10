<script setup lang="ts">
import { drawTenseDots, grammarFontReady, prepareWord, type Tense } from '~/utils/tenseDots'

/**
 * Syriac in the Grammar book, set in Serto Urhoy (Owner, 2026-10-30), whatever script the reader
 * chose elsewhere: the tables depend on where this font puts qushoyo, rukkokho and the vowels.
 *
 * With a `tense`, the form's tense dots are taken out of the text and drawn in red where the book
 * has them (see utils/tenseDots). Without one the text is shown as it is, the font's own marks only.
 * Each word stays one unbroken text node: wrapping a letter in an element breaks Syriac joining.
 */
const props = defineProps<{
  text: string
  tense?: Tense
}>()

const words = computed(() => props.text.split(' ').filter(Boolean).map(word => (
  props.tense ? prepareWord(word, props.tense) : { word, marks: [], raise: null }
)))
const needsDrawing = computed(() => words.value.some(w => w.marks.length || w.raise))

const hosts = ref<HTMLElement[]>([])
let frame = 0

function draw() {
  if (!props.tense) return
  hosts.value.forEach((host, index) => {
    const prepared = words.value[index]
    const textEl = host.querySelector<HTMLElement>('.gw-text')
    if (prepared && textEl) drawTenseDots(host, textEl, prepared, props.tense!)
  })
}

function schedule() {
  cancelAnimationFrame(frame)
  frame = requestAnimationFrame(draw)
}

onMounted(() => {
  if (!needsDrawing.value) return
  grammarFontReady().then(schedule)
  window.addEventListener('resize', schedule)
})

onBeforeUnmount(() => {
  cancelAnimationFrame(frame)
  window.removeEventListener('resize', schedule)
})

watch(() => [props.text, props.tense], () => nextTick(() => grammarFontReady().then(schedule)))
</script>

<template>
  <span class="grammar-syriac" dir="rtl" lang="syc"><template v-for="(prepared, index) in words" :key="index"><span ref="hosts" class="gw"><span class="gw-text">{{ prepared.word }}</span></span>{{ index < words.length - 1 ? ' ' : '' }}</template></span>
</template>

<style>
/* Not scoped: the dots and overlays are made by script, outside Vue's scoping. */
.grammar-syriac {
  font-family: "Serto Urhoy", var(--font-syriac);
  unicode-bidi: isolate;
  font-feature-settings: "kern", "liga";
}

.gw {
  position: relative;
  white-space: nowrap;
}

.gw-text.is-raised { color: transparent; }

.tense-dot {
  position: absolute;
  border-radius: 50%;
  background: var(--color-tense-dot, #b3261e);
  pointer-events: none;
}

.tense-overlay {
  position: absolute;
  pointer-events: none;
  user-select: none;
}

.tense-copy { white-space: nowrap; }

.tense-vowel {
  background: currentColor;
  mask-size: 100% 100%;
  -webkit-mask-size: 100% 100%;
  mask-repeat: no-repeat;
  -webkit-mask-repeat: no-repeat;
}
</style>
