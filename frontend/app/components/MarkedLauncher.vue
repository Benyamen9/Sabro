<script setup lang="ts">
import type { LibraryMark } from '~/utils/libraryMarks'

/**
 * The "܀ Marked n" button (Owner, 2026-11-01, round 10 C): by the Library's title and on the
 * profile. It opens the Marked panel; marks unticked there go when it closes, and for a few seconds
 * a line offers to put them back.
 */
const UNDO_FOR_MS = 7000

const { t } = useI18n()
const { marks, load, mark, unmark } = useLibraryMarks()

const open = ref(false)
const removed = ref<LibraryMark[]>([])
let undoTimer: ReturnType<typeof setTimeout> | undefined

onMounted(load)
onBeforeUnmount(() => clearTimeout(undoTimer))

function onClose(gone: LibraryMark[]) {
  open.value = false
  if (!gone.length) return
  for (const item of gone) unmark(item.book, item.itemKey)
  removed.value = gone
  clearTimeout(undoTimer)
  undoTimer = setTimeout(() => { removed.value = [] }, UNDO_FOR_MS)
}

function undo() {
  clearTimeout(undoTimer)
  for (const item of removed.value) mark(item.book, item.itemKey, item.label, item.markedAt)
  removed.value = []
}
</script>

<template>
  <button
    type="button"
    class="marked-button inline-flex items-center gap-2 rounded-full border py-2 pr-3.5 pl-3 font-sans text-sm font-medium"
    aria-haspopup="dialog"
    @click="open = true"
  >
    <span aria-hidden="true" class="font-syriac text-[27px] leading-[0.7] text-[#c9a24c]">܀</span>
    {{ t('library.marks.title') }}
    <ClientOnly>
      <span v-if="marks.length" class="rounded-full bg-[#c9a24c] px-[7px] py-px text-xs font-semibold text-white">{{ marks.length }}</span>
    </ClientOnly>
  </button>

  <MarkedPanel v-if="open" @close="onClose" />

  <Teleport to="body">
    <div
      v-if="removed.length"
      class="fixed inset-x-0 z-40 flex justify-center px-4"
      :style="{ bottom: 'calc(16px + env(safe-area-inset-bottom, 0px))' }"
      role="status"
    >
      <div class="flex items-center gap-4 rounded-full bg-[var(--color-text)] py-2.5 pr-2.5 pl-4 font-sans text-[13.5px] text-[var(--color-bg)] shadow-[0_8px_30px_rgb(0_0_0/0.25)]">
        {{ t('library.marks.removed', { count: removed.length }, removed.length) }}
        <button type="button" class="rounded-full bg-[var(--color-bg)] px-3 py-1 font-semibold text-[var(--color-text)]" @click="undo">
          {{ t('library.marks.undo') }}
        </button>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.marked-button {
  --gold: #c9a24c;
  border-color: color-mix(in oklab, var(--gold) 45%, var(--color-border));
  background: color-mix(in oklab, var(--gold) 14%, var(--color-bg-elevated));
  color: color-mix(in oklab, var(--gold) 55%, var(--color-text));
}

.marked-button:hover { border-color: var(--gold); }
</style>
