<script setup lang="ts">
/**
 * A deliberate yes to something consequential.
 *
 * Exists because granting Owner was a bare checkbox: one stray click on a row
 * you were only reading handed somebody every area of the backoffice, silently,
 * with no step in between and nothing to say what had just happened. The button
 * that opens this asks; this says what the answer means before taking it.
 *
 * Not `window.confirm`: that renders unstyled and untranslated, and it blocks the
 * whole tab. It also cannot say *whose* access is about to change, which is the
 * one detail worth reading before agreeing.
 *
 * Focus moves to the cancel button on open — never to the confirming one, so a
 * held or repeated Enter cannot carry through from the button that opened this.
 * A slot may claim focus instead by marking an element `data-confirm-focus`:
 * a field you have to fill in is safe to land on and is the thing to do next.
 */
const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    /** The consequence, in plain words. Shown above the buttons. */
    body: string
    confirmLabel: string
    cancelLabel: string
    /** `danger` for taking access away, `accent` for granting it. */
    tone?: 'accent' | 'danger'
    busy?: boolean
    /** Held shut until the slot says its condition is met. */
    confirmDisabled?: boolean
  }>(),
  { tone: 'accent', busy: false, confirmDisabled: false },
)

const emit = defineEmits<{ (e: 'confirm' | 'cancel'): void }>()

const panel = ref<HTMLElement | null>(null)
const cancelButton = ref<HTMLButtonElement | null>(null)
const titleId = useId()
const bodyId = useId()

watch(
  () => props.open,
  async (open) => {
    if (!open) return
    await nextTick()
    const claimed = panel.value?.querySelector<HTMLElement>('[data-confirm-focus]')
    ;(claimed ?? cancelButton.value)?.focus()
  },
)

function onCancel() {
  if (props.busy) return
  emit('cancel')
}

/**
 * Escape cancels, as in any dialog. Bound while open only, so the page's other
 * Escape handling is untouched the rest of the time.
 */
onMounted(() => {
  const onKeydown = (event: KeyboardEvent) => {
    if (props.open && event.key === 'Escape') onCancel()
  }
  window.addEventListener('keydown', onKeydown)
  onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
})

const confirmClass = computed(() =>
  props.tone === 'danger'
    ? 'border border-[var(--color-accent)] bg-transparent text-[var(--color-accent)] hover:bg-[var(--color-accent-faint)]'
    : 'bg-[var(--color-accent)] text-white hover:bg-[var(--color-accent-hover)]',
)
</script>

<template>
  <Transition
    enter-active-class="transition-opacity duration-150"
    enter-from-class="opacity-0"
    leave-active-class="transition-opacity duration-100"
    leave-to-class="opacity-0"
  >
    <div
      v-if="open"
      class="fixed inset-0 z-50 grid place-items-center bg-[rgb(28_25_23/0.45)] p-4"
      @click.self="onCancel"
    >
      <div
        ref="panel"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        :aria-describedby="bodyId"
        class="w-full max-w-md rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
      >
        <h2 :id="titleId" class="font-serif text-xl font-semibold tracking-[-0.01em] text-[var(--color-text)]">
          {{ title }}
        </h2>
        <p :id="bodyId" class="mt-3 font-sans text-sm leading-relaxed text-[var(--color-text-muted)]">
          {{ body }}
        </p>

        <slot />

        <div class="mt-6 flex flex-wrap justify-end gap-3">
          <button
            ref="cancelButton"
            type="button"
            class="rounded-md border border-[var(--color-border-strong)] px-4 py-2 font-sans text-sm font-medium text-[var(--color-text)] transition-colors hover:bg-[var(--color-bg-subtle)] disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="busy"
            @click="onCancel"
          >{{ cancelLabel }}</button>
          <button
            type="button"
            class="rounded-md px-4 py-2 font-sans text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50"
            :class="confirmClass"
            :disabled="busy || confirmDisabled"
            @click="emit('confirm')"
          >{{ confirmLabel }}</button>
        </div>
      </div>
    </div>
  </Transition>
</template>
