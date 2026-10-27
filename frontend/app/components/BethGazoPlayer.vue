<script setup lang="ts">
/**
 * The one player of the Beth Gazo page, held at the bottom of the screen while a
 * chant is chosen (Owner, 2026-10-25: one player bar, not one per chant). Starting
 * another chant swaps the source; the bar stays.
 */
const props = defineProps<{
  src: string
  title: string
  subtitle: string
  incipit?: string | null
}>()

const emit = defineEmits<{ close: [] }>()
const { t } = useI18n()

const audio = ref<HTMLAudioElement | null>(null)
const paused = ref(true)
const current = ref(0)
const duration = ref(0)

function toggle() {
  const el = audio.value
  if (!el) return
  if (el.paused) void el.play().catch(() => { paused.value = true })
  else el.pause()
}

function seek(event: Event) {
  const el = audio.value
  if (el) el.currentTime = Number((event.target as HTMLInputElement).value)
}

function clock(seconds: number) {
  if (!Number.isFinite(seconds)) return '0:00'
  const s = Math.floor(seconds)
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
}

// A new chant plays at once: the click that chose it is the user gesture.
watch(() => props.src, async () => {
  current.value = 0
  duration.value = 0
  await nextTick()
  void audio.value?.play().catch(() => { paused.value = true })
})

onMounted(() => {
  void audio.value?.play().catch(() => { paused.value = true })
})

defineExpose({ toggle, paused })
</script>

<template>
  <div
    class="sticky bottom-0 z-30 -mx-6 mt-6 flex items-center gap-3 border-t border-[var(--color-border)] bg-[color-mix(in_oklab,var(--color-bg-elevated)_94%,transparent)] px-6 py-2.5 backdrop-blur-md"
    role="region"
    :aria-label="t('bethGazo.player')"
  >
    <audio
      ref="audio"
      :src="src"
      preload="metadata"
      @play="paused = false"
      @pause="paused = true"
      @ended="paused = true"
      @timeupdate="current = audio?.currentTime ?? 0"
      @loadedmetadata="duration = audio?.duration ?? 0"
    />
    <button
      type="button"
      class="grid size-10 shrink-0 place-items-center rounded-full bg-[var(--color-nahlo)] text-[var(--color-bg)]"
      :aria-label="t(paused ? 'bethGazo.resume' : 'bethGazo.pauseShort')"
      @click="toggle"
    >
      <svg v-if="!paused" aria-hidden="true" viewBox="0 0 12 14" class="size-3"><path d="M1 0h3v14H1zM8 0h3v14H8z" fill="currentColor" /></svg>
      <svg v-else aria-hidden="true" viewBox="0 0 12 14" class="ml-0.5 size-3"><path d="M0 0l12 7-12 7z" fill="currentColor" /></svg>
    </button>
    <div class="w-[38%] min-w-0 shrink-0 sm:w-56">
      <p class="truncate font-serif text-[15px] font-semibold text-[var(--color-text)]">{{ title }}</p>
      <p class="truncate font-sans text-xs text-[var(--color-text-muted)]">{{ subtitle }}</p>
      <p v-if="incipit" dir="rtl" lang="syc" class="truncate text-left font-syriac text-sm text-[var(--color-text-muted)]">{{ incipit }}</p>
    </div>
    <div class="flex min-w-0 flex-1 items-center gap-2">
      <input
        type="range"
        min="0"
        :max="duration || 0"
        step="0.1"
        :value="current"
        class="min-w-0 flex-1 accent-[var(--color-nahlo)]"
        :aria-label="t('bethGazo.position')"
        @input="seek"
      >
      <span class="hidden shrink-0 font-sans text-xs tabular-nums text-[var(--color-text-muted)] sm:inline">{{ clock(current) }} / {{ clock(duration) }}</span>
    </div>
    <button
      type="button"
      class="grid size-8 shrink-0 place-items-center rounded-full text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)]"
      :aria-label="t('bethGazo.closePlayer')"
      @click="emit('close')"
    >
      <svg aria-hidden="true" viewBox="0 0 14 14" class="size-3.5"><path d="M2 2l10 10M12 2 2 12" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" /></svg>
    </button>
  </div>
</template>
