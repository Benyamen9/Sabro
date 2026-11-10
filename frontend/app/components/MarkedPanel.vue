<script setup lang="ts">
import { daysAgo, markHref, markId, type LibraryMark } from '~/utils/libraryMarks'
import { LIBRARY_BOOKS, type LibraryBook } from '~/utils/libraryPlaces'

/**
 * Everything the reader marked ܀, in one plain panel (Owner, 2026-11-01, round 13): a sheet from
 * the bottom on a phone, a panel on the right on a computer. Newest first, a filter per book, each
 * row edged in its book's colour. A row opens its item; its gold tick unmarks it — the row only
 * fades and stays until the panel closes, so a slip is one tap to put right, and closing hands the
 * removed marks to the launcher for its "Undo" line.
 */
const emit = defineEmits<{ close: [removed: LibraryMark[]] }>()

const { t, locale } = useI18n()
const { marks, signedIn } = useLibraryMarks()
const { isConfigured, signIn } = useAuth()

const BOOK_KEYS: Record<LibraryBook, string> = { 'lexicon': 'lexicon', 'beth-gazo': 'bethGazo', 'beth-salme': 'bethSalme' }
const BOOK_COLOURS: Record<LibraryBook, string> = {
  'lexicon': 'var(--color-meltho)',
  'beth-gazo': 'var(--color-nahlo)',
  'beth-salme': 'var(--color-shmo)',
}
const bookName = (book: LibraryBook) => t(`library.shelf.books.${BOOK_KEYS[book]}.title`)

const filter = ref<LibraryBook | null>(null)
const shown = computed(() => (filter.value ? marks.value.filter(mark => mark.book === filter.value) : marks.value))

/** Marks whose tick was emptied: still listed, removed when the panel closes. */
const unticked = ref(new Set<string>())
const isOff = (mark: LibraryMark) => unticked.value.has(markId(mark.book, mark.itemKey))

function toggleTick(mark: LibraryMark) {
  const id = markId(mark.book, mark.itemKey)
  if (unticked.value.has(id)) unticked.value.delete(id)
  else unticked.value.add(id)
}

function close() {
  emit('close', marks.value.filter(isOff))
}

function when(mark: LibraryMark) {
  const days = daysAgo(mark.markedAt)
  if (days <= 1) return new Intl.RelativeTimeFormat(locale.value, { numeric: 'auto' }).format(-Math.max(days, 0), 'day')
  const then = new Date(mark.markedAt)
  const sameYear = then.getFullYear() === new Date().getFullYear()
  return new Intl.DateTimeFormat(locale.value, { day: 'numeric', month: 'short', year: sameYear ? undefined : 'numeric' }).format(then)
}

function itemName(mark: LibraryMark) {
  return [mark.label?.title, mark.label?.subtitle].filter(Boolean).join(' · ') || mark.label?.syriac || bookName(mark.book)
}

const closeButton = ref<HTMLButtonElement | null>(null)

function onKey(event: KeyboardEvent) {
  if (event.key === 'Escape') close()
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
    <div class="marked-shade fixed inset-0 z-50 bg-[rgb(28_25_23/0.35)]" @click.self="close">
      <div
        class="marked-panel absolute flex flex-col bg-[var(--color-bg)] max-sm:inset-x-0 max-sm:bottom-0 max-sm:max-h-[82dvh] max-sm:rounded-t-[20px] max-sm:shadow-[0_-10px_40px_rgb(0_0_0/0.2)] sm:inset-y-0 sm:right-0 sm:w-[420px] sm:shadow-[-10px_0_40px_rgb(0_0_0/0.18)]"
        role="dialog"
        aria-modal="true"
        :aria-label="t('library.marks.title')"
      >
        <div class="px-4 pt-2.5 sm:px-[22px] sm:pt-5">
          <div aria-hidden="true" class="mx-auto mb-3 h-[5px] w-10 rounded-full bg-[var(--color-border)] sm:hidden" />
          <div class="flex items-center gap-2.5">
            <span aria-hidden="true" class="font-syriac text-[32px] leading-[0.7] text-[#c9a24c]">܀</span>
            <h2 class="font-serif text-[22px] font-semibold">{{ t('library.marks.title') }}</h2>
            <span v-if="marks.length" class="font-sans text-[13px] text-[var(--color-text-muted)]">{{ marks.length }}</span>
            <button
              ref="closeButton"
              type="button"
              class="ml-auto grid size-8 place-items-center rounded-full bg-[var(--color-bg-subtle)] font-sans text-[15px] font-medium text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
              :aria-label="t('library.marks.close')"
              @click="close"
            >✕</button>
          </div>

          <div v-if="marks.length" class="mt-3.5 flex flex-wrap gap-1.5" role="group" :aria-label="t('library.marks.filterLabel')">
            <button
              type="button"
              class="rounded-full px-[11px] py-[5px] font-sans text-[12.5px] font-medium"
              :class="filter === null ? 'bg-[var(--color-text)] text-[var(--color-bg)]' : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'"
              :aria-pressed="filter === null"
              @click="filter = null"
            >{{ t('library.marks.all') }}</button>
            <button
              v-for="book in LIBRARY_BOOKS"
              :key="book"
              type="button"
              class="inline-flex items-center gap-1.5 rounded-full px-[11px] py-[5px] font-sans text-[12.5px] font-medium"
              :class="filter === book ? 'bg-[var(--color-text)] text-[var(--color-bg)]' : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'"
              :aria-pressed="filter === book"
              @click="filter = book"
            >
              <span aria-hidden="true" class="size-2 rounded-full" :style="{ background: BOOK_COLOURS[book] }" />
              {{ bookName(book) }}
            </button>
          </div>
        </div>

        <div class="min-h-0 flex-1 overflow-y-auto px-4 pt-3.5 pb-5 sm:px-[22px]">
          <p v-if="!marks.length" class="py-6 text-center font-serif text-[15px] leading-relaxed text-[var(--color-text-muted)]">
            {{ t('library.marks.empty') }}
          </p>
          <p v-else-if="!shown.length" class="py-6 text-center font-serif text-[15px] text-[var(--color-text-muted)]">
            {{ t('library.marks.emptyBook') }}
          </p>
          <ul v-else class="grid gap-2">
            <li
              v-for="mark in shown"
              :key="markId(mark.book, mark.itemKey)"
              class="flex items-center gap-1 rounded-xl border border-l-4 border-[var(--color-border)] bg-[var(--color-bg-elevated)] pr-2 transition-opacity"
              :class="isOff(mark) ? 'opacity-55' : 'shadow-[var(--shadow-soft)]'"
              :style="{ borderLeftColor: BOOK_COLOURS[mark.book] }"
            >
              <NuxtLink
                :to="markHref(mark)"
                class="flex min-w-0 flex-1 items-center gap-3 py-[11px] pr-2 pl-3.5 no-underline"
                @click="close"
              >
                <span class="min-w-0 flex-1">
                  <span
                    class="block truncate font-serif text-base font-semibold text-[var(--color-text)]"
                    :class="{ 'line-through decoration-[var(--color-text-faint)]': isOff(mark) }"
                  >{{ itemName(mark) }}</span>
                  <span class="block font-sans text-[12.5px] text-[var(--color-text-muted)]">{{ bookName(mark.book) }} · {{ when(mark) }}</span>
                </span>
                <SyriacText v-if="mark.label?.syriac && mark.label.title" :text="mark.label.syriac" class="shrink-0 !text-2xl text-[var(--color-text)]" />
              </NuxtLink>
              <button
                type="button"
                class="grid size-11 shrink-0 place-items-center"
                :aria-pressed="!isOff(mark)"
                :aria-label="t('library.marks.keep', { item: itemName(mark) })"
                @click="toggleTick(mark)"
              >
                <span
                  aria-hidden="true"
                  class="grid size-7 place-items-center rounded-full border-[1.5px] font-sans text-sm font-bold"
                  :class="isOff(mark) ? 'border-[var(--color-text-faint)] text-transparent' : 'border-[#c9a24c] bg-[#c9a24c] text-white'"
                >✓</span>
              </button>
            </li>
          </ul>

          <p v-if="shown.length" class="mt-3 text-center font-sans text-[12.5px] text-[var(--color-text-faint)]" aria-live="polite">
            {{ unticked.size ? t('library.marks.hintUnticked') : t('library.marks.hint') }}
          </p>
          <p v-if="!signedIn && isConfigured && marks.length" class="mt-3 text-center font-sans text-[12.5px] text-[var(--color-text-muted)]">
            <button type="button" class="underline decoration-[var(--color-border-strong)] underline-offset-2 hover:text-[var(--color-text)]" @click="signIn()">
              {{ t('library.marks.signInLine') }}
            </button>
          </p>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.marked-shade { animation: shade-in 0.18s ease; }
.marked-panel { animation: sheet-up 0.22s ease-out; }

@media (min-width: 640px) {
  .marked-panel { animation-name: panel-in; }
}

@keyframes shade-in { from { opacity: 0; } }
@keyframes sheet-up { from { transform: translateY(24px); opacity: 0; } }
@keyframes panel-in { from { transform: translateX(24px); opacity: 0; } }

@media (prefers-reduced-motion: reduce) {
  .marked-shade, .marked-panel { animation: none; }
}
</style>
