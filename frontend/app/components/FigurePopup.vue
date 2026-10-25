<script setup lang="ts">
import type { GalleryFigure } from '~/utils/gallery'

/**
 * On a phone, a card lifts out over the collection instead of opening a page
 * (Owner, 2026-10-25). Tap it to turn it over: the back holds what the player
 * knows. Closing returns them to the same place in the grid.
 */
const props = defineProps<{ figure: GalleryFigure }>()
const emit = defineEmits<{ (e: 'close'): void }>()

const { t } = useI18n()

const turned = ref(false)
const closeButton = ref<HTMLButtonElement | null>(null)
const tilt = ref('')

watch(() => props.figure.id, () => { turned.value = false })

function onKey(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('close')
}

function lean(event: PointerEvent) {
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
  const box = (event.currentTarget as HTMLElement).getBoundingClientRect()
  const x = (event.clientX - box.left) / box.width - 0.5
  const y = (event.clientY - box.top) / box.height - 0.5
  tilt.value = `rotateY(${x * 18}deg) rotateX(${-y * 18}deg)`
}

onMounted(() => {
  document.addEventListener('keydown', onKey)
  document.documentElement.style.overflow = 'hidden'
  closeButton.value?.focus()
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKey)
  document.documentElement.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <div
      class="figure-popup fixed inset-0 z-50 flex flex-col items-center justify-center gap-4 bg-[rgb(24_22_20/0.7)] px-4"
      role="dialog"
      aria-modal="true"
      :aria-label="figure.name"
      @click.self="emit('close')"
    >
      <button
        ref="closeButton"
        type="button"
        class="absolute grid size-9 place-items-center rounded-full bg-[var(--color-bg-elevated)] text-xl leading-none text-[var(--color-text)]"
        :style="{ top: 'calc(12px + env(safe-area-inset-top, 0px))', right: 'calc(12px + env(safe-area-inset-right, 0px))' }"
        :aria-label="t('bethSalme.detail.close')"
        @click="emit('close')"
      >×</button>

      <div class="lift" :style="{ transform: tilt }" @pointermove="lean" @pointerleave="tilt = ''">
        <!-- The card itself turns on a tap; the button under it does the same for
             keyboards and screen readers, so the card needs no role of its own. -->
        <div class="flip" :class="{ turned }" @click="turned = !turned">
          <span class="face front" :class="`frame-${figure.tier}`" :aria-hidden="turned">
            <span class="art"><FigureArt :figure="figure" :tier="figure.tier" :illuminated="figure.illuminated" /></span>
            <span class="caption">
              <span class="block font-sans text-[10px] font-semibold uppercase tracking-[0.1em] text-[var(--color-text-muted)]">
                {{ t('bethSalme.card.number', { no: figure.no }) }} · {{ t(`bethSalme.status.${figure.tier}`) }}<template v-if="figure.tier === 'found' && figure.illuminated"> · ✦</template>
              </span>
              <span class="mt-1 block font-serif text-[19px] font-semibold leading-tight">{{ figure.name }}</span>
              <span
                v-if="figure.syriacName"
                dir="rtl"
                lang="syc"
                class="mt-0.5 block text-left font-syriac text-[17px] text-[var(--color-shmo)]"
              >{{ figure.syriacName }}</span>
            </span>
          </span>
          <div class="face back" :class="`frame-${figure.tier}`" :aria-hidden="!turned">
            <div class="sheet">
              <p class="mb-2 font-serif text-[17px] font-semibold leading-tight">{{ figure.name }}</p>
              <FigureDetails :figure="figure" :show-link="false" />
            </div>
          </div>
        </div>
      </div>

      <div class="flex max-w-[300px] flex-col items-center gap-2 text-center font-sans text-[13px] leading-snug text-white/90">
        <button
          type="button"
          class="rounded-full border border-white/40 px-4 py-1.5 font-medium text-white hover:bg-white/10"
          :aria-pressed="turned"
          @click="turned = !turned"
        >{{ turned ? t('bethSalme.detail.turnBack') : t('bethSalme.detail.turnOver') }}</button>
        <p v-if="figure.tier !== 'found'">{{ t(figure.tier === 'met' ? 'bethSalme.detail.findToColour' : 'bethSalme.detail.lockedHint', { name: figure.name }) }}</p>
        <a
          v-if="figure.readMoreUrl"
          :href="figure.readMoreUrl"
          target="_blank"
          rel="noopener"
          class="text-white underline-offset-2 hover:underline"
        >{{ t('bethSalme.detail.readMore') }} →</a>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.figure-popup {
  perspective: 1100px;
  animation: popup-fade 0.2s ease;
}

.lift {
  width: min(78vw, 270px);
  aspect-ratio: 5 / 7;
  transform-style: preserve-3d;
  transition: transform 0.12s ease-out;
  animation: card-lift 0.5s cubic-bezier(0.2, 1.35, 0.4, 1);
}

.flip {
  position: relative;
  display: block;
  width: 100%;
  height: 100%;
  color: var(--color-text);
  transform-style: preserve-3d;
  transition: transform 0.65s cubic-bezier(0.3, 0.7, 0.3, 1);
}

.flip { cursor: pointer; }
.flip.turned { transform: rotateY(180deg); }

.face {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  padding: 9px;
  border-radius: 18px;
  background: var(--frame);
  box-shadow: 0 22px 55px rgb(0 0 0 / 0.45);
  backface-visibility: hidden;
  -webkit-backface-visibility: hidden;
}

.face.back { transform: rotateY(180deg); }

.frame-found { --frame: linear-gradient(160deg, color-mix(in oklab, var(--color-shmo) 55%, white), var(--color-shmo-dark)); }
.frame-met { --frame: var(--color-bg-subtle); }
.frame-locked { --frame: var(--figure-locked-mid); }

.art {
  display: block;
  overflow: hidden;
  border-radius: 11px;
}

.caption,
.sheet {
  display: block;
  flex: 1;
  min-height: 0;
  margin-top: 7px;
  padding: 8px 11px;
  border-radius: 9px;
  background: var(--color-bg-elevated);
}

.frame-locked .caption { background: var(--figure-locked-bg); color: var(--figure-locked-text); }

.sheet {
  margin-top: 0;
  padding: 13px 14px;
  overflow-y: auto;
}

@keyframes popup-fade { from { opacity: 0; } }
@keyframes card-lift { from { transform: translateY(70px) scale(0.42) rotate(-7deg); opacity: 0; } }

@media (prefers-reduced-motion: reduce) {
  .figure-popup, .lift { animation: none; }
  .flip { transition: none; }
}
</style>
