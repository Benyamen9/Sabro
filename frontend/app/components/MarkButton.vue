<script setup lang="ts">
import type { LibraryBook, LibraryPlaceLabel } from '~/utils/libraryPlaces'

/**
 * The reader's own mark on an item (Owner, 2026-11-01): the scribe's ܀, grey until the item is
 * marked and gold once it is. A pill with its word on an entry ("Mark ܀" / "Marked"), the sign
 * alone on a line of a list. Marked items are found again under "܀ Marked" on the Library shelf.
 */
const props = withDefaults(defineProps<{
  book: LibraryBook
  itemKey: string
  /** How the Marked panel names the item until Sabro names it. */
  label: LibraryPlaceLabel | null
  /** What a screen reader calls the item ("king", "Hablo l'itokh, mode 3"). */
  name: string
  variant?: 'pill' | 'icon'
  /** On a dark ground (the phone's lifted figure card). */
  onDark?: boolean
}>(), { variant: 'pill', onDark: false })

const { t } = useI18n()
const { loaded, load, isMarked, toggle } = useLibraryMarks()

onMounted(() => {
  if (!loaded.value) load()
})

const marked = computed(() => isMarked(props.book, props.itemKey))
const ariaLabel = computed(() => t(marked.value ? 'library.marks.unmarkItem' : 'library.marks.markItem', { item: props.name }))
</script>

<template>
  <button
    v-if="variant === 'pill'"
    type="button"
    class="mark-pill inline-flex items-center gap-1.5 rounded-full border py-1.5 pr-3.5 pl-3 font-sans text-[13px] font-medium transition-colors"
    :class="{ 'is-marked': marked, 'on-dark': onDark }"
    :aria-pressed="marked"
    :aria-label="ariaLabel"
    @click="toggle(book, itemKey, label)"
  >
    <span aria-hidden="true" class="sign font-syriac text-[25px] leading-[0.7]">܀</span>
    {{ t(marked ? 'library.marks.marked' : 'library.marks.mark') }}
  </button>
  <button
    v-else
    type="button"
    class="mark-icon grid size-9 shrink-0 place-items-center rounded-full font-syriac text-[29px] leading-none transition-colors"
    :class="{ 'is-marked': marked }"
    :aria-pressed="marked"
    :aria-label="ariaLabel"
    :title="t(marked ? 'library.marks.marked' : 'library.marks.mark')"
    @click="toggle(book, itemKey, label)"
  >
    <span aria-hidden="true">܀</span>
  </button>
</template>

<style scoped>
.mark-pill,
.mark-icon {
  --gold: #c9a24c;
}

.mark-pill {
  border-color: var(--color-border-strong);
  background: var(--color-bg-elevated);
  color: var(--color-text-muted);
}

.mark-pill .sign { color: var(--color-text-faint); }
.mark-pill:hover { border-color: var(--gold); }

.mark-pill.is-marked {
  border-color: var(--gold);
  background: color-mix(in oklab, var(--gold) 14%, var(--color-bg-elevated));
  color: color-mix(in oklab, var(--gold) 55%, var(--color-text));
}

.mark-pill.is-marked .sign { color: var(--gold); }

.mark-pill.on-dark:not(.is-marked) {
  border-color: rgb(255 255 255 / 0.4);
  background: transparent;
  color: #fff;
}

.mark-pill.on-dark:not(.is-marked) .sign { color: rgb(255 255 255 / 0.7); }

.mark-icon { color: var(--color-text-faint); }
.mark-icon:hover { background: var(--color-bg-subtle); color: var(--color-text-muted); }
.mark-icon.is-marked { color: var(--gold); }
</style>
