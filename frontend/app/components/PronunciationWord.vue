<script setup lang="ts">
/**
 * The word, and its own voice.
 *
 * There is no button here — the word *is* the control, and the strip beneath it
 * is the recording drawn. Pressing it deliberately produces **no press effect**
 * (owner, 2026-08-09): no tint, no shift, no pill. The only thing that answers a
 * click is the strip filling, which is the point — you are looking at the sound,
 * not at a widget.
 *
 * ⚠️ The strip runs **right to left**, like the word above it. `dir="rtl"` on the
 * flex row reverses the bars visually, so peak 0 — the start of the recording —
 * sits under the word's first letter on the right, and the fill travels the same
 * way the eye reads. Do not "fix" this to left-to-right.
 *
 * The bars are real: the file is decoded once with the Web Audio API and reduced
 * to a fixed number of peaks, the same approach as Nahlo's ChantPlayer, so a
 * recording looks like a recording anywhere in the ecosystem. Drawing a
 * plausible-looking fixed shape instead would be a picture of a sound that was
 * never made.
 *
 * ⚠️ Decoding is best-effort and must never cost the player the audio. A file
 * that will not decode — unsupported container, a CORS refusal, no AudioContext
 * — leaves `peaks` empty and the strip falls back to a flat line that still
 * plays and still fills. Only the picture is missing.
 */
const props = defineProps<{
  /** Absolute media URL, or null when the word has no recording yet. */
  src: string | null
}>()

const { t } = useI18n()

const audioEl = ref<HTMLAudioElement | null>(null)
const playing = ref(false)
/** How far through the recording playback is, 0–1, for filling the bars. */
const progress = ref(0)

const PEAK_COUNT = 30
const peaks = ref<number[]>([])

/** The drawn strip: real peaks when we have them, else a flat line. A word with
 *  no recording gets the same flat line, dimmed — it still says "a recording
 *  belongs here", which is quieter and truer than a disabled button labelled
 *  with an action you cannot take. */
const bars = computed(() =>
  peaks.value.length ? peaks.value : Array.from({ length: PEAK_COUNT }, () => 0.1),
)

const litUpTo = computed(() =>
  progress.value <= 0 ? -1 : Math.floor(progress.value * bars.value.length),
)

async function drawWaveform() {
  if (!import.meta.client || !props.src || peaks.value.length > 0) return
  const Ctx = window.AudioContext
    ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
  if (!Ctx) return
  try {
    const response = await fetch(props.src)
    const buffer = await response.arrayBuffer()
    const ctx = new Ctx()
    const decoded = await ctx.decodeAudioData(buffer)
    void ctx.close()

    const channel = decoded.getChannelData(0)
    const per = Math.floor(channel.length / PEAK_COUNT) || 1
    const raw: number[] = []
    for (let i = 0; i < PEAK_COUNT; i++) {
      let peak = 0
      for (let j = 0; j < per; j++) {
        const v = Math.abs(channel[i * per + j] ?? 0)
        if (v > peak) peak = v
      }
      raw.push(peak)
    }
    // Normalised against the loudest peak, so a quietly recorded word still
    // fills the strip rather than reading as a flat line.
    const loudest = Math.max(...raw, 0.0001)
    peaks.value = raw.map(p => Math.max(0.12, p / loudest))
  }
  catch {
    // Flat strip; the recording still plays.
  }
}

onMounted(drawWaveform)
watch(() => props.src, () => {
  peaks.value = []
  progress.value = 0
  void drawWaveform()
})

function toggle() {
  const audio = audioEl.value
  if (!audio) return
  if (audio.paused) void audio.play()
  else audio.pause()
}

/**
 * The fill is driven by requestAnimationFrame, NOT by the `timeupdate` event.
 *
 * `timeupdate` fires roughly four times a second. These recordings are about one
 * second long, so it would advance the strip in three or four visible jumps —
 * and since the owner asked for no press effect, this fill is the *only* thing
 * that answers a click. A stuttering fill would read as a broken control.
 */
let frame = 0

function step() {
  const audio = audioEl.value
  if (!audio) return
  // duration is NaN until metadata loads; leave progress alone rather than
  // writing NaN into the style binding.
  if (Number.isFinite(audio.duration) && audio.duration > 0) {
    progress.value = audio.currentTime / audio.duration
  }
  if (!audio.paused) frame = requestAnimationFrame(step)
}

function onPlay() {
  playing.value = true
  cancelAnimationFrame(frame)
  frame = requestAnimationFrame(step)
}

function onPause() {
  playing.value = false
  cancelAnimationFrame(frame)
}

function onEnded() {
  playing.value = false
  cancelAnimationFrame(frame)
  progress.value = 0
}

onBeforeUnmount(() => cancelAnimationFrame(frame))

/** Bar height in px, from a 0–1 peak. Floored so a silent stretch still draws a
 *  line rather than vanishing and leaving a gap in the strip. */
function barHeight(peak: number) {
  return `${Math.max(2, Math.round(peak * 14))}px`
}
</script>

<template>
  <div class="flex flex-col items-center">
    <button
      type="button"
      :disabled="!src"
      :aria-label="src ? t('library.pronunciation.listen') : t('library.pronunciation.none')"
      :title="src ? undefined : t('library.pronunciation.none')"
      class="inline-flex flex-col items-center gap-1.5 border-0 bg-transparent p-0 not-disabled:cursor-pointer disabled:cursor-default"
      @click="toggle"
    >
      <slot />
      <!-- dir="rtl": the strip reads with the word, and fills from the right. -->
      <span
        dir="rtl"
        aria-hidden="true"
        class="flex h-[14px] items-center gap-[2px]"
        :class="{ 'opacity-45': !src }"
      >
        <span
          v-for="(peak, index) in bars"
          :key="index"
          class="w-[2.5px] rounded-full transition-colors duration-100"
          :style="{ height: barHeight(peak) }"
          :class="index <= litUpTo ? 'bg-[var(--color-accent)]' : 'bg-[var(--color-text-faint)]'"
        />
      </span>
    </button>
    <audio
      v-if="src"
      ref="audioEl"
      class="hidden"
      :src="src"
      preload="metadata"
      @play="onPlay"
      @pause="onPause"
      @ended="onEnded"
    />
  </div>
</template>
