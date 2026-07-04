<script setup lang="ts">
import type { LibrarySort, SortDirection } from '~/types/api'

const { t, locale } = useI18n()
const { listWords } = useMelthoLibrary()
const preferredMeaning = usePreferredMeaning()

const route = useRoute()
const router = useRouter()

// Sort is carried in the URL as a friendly lowercase key; the API enum is PascalCase. Each field
// has a natural direction (recent → newest first, the rest → ascending); re-clicking the active
// field flips it.
const sortOptions: { value: LibrarySort, key: string, natural: SortDirection }[] = [
  { value: 'Recent', key: 'recent', natural: 'Descending' },
  { value: 'Alphabetical', key: 'alphabetical', natural: 'Ascending' },
  { value: 'Length', key: 'length', natural: 'Ascending' },
]

function naturalDirection(value: LibrarySort): SortDirection {
  return sortOptions.find(o => o.value === value)?.natural ?? 'Descending'
}

function sortFromQuery(): LibrarySort {
  const raw = typeof route.query.sort === 'string' ? route.query.sort : ''
  return sortOptions.find(o => o.key === raw)?.value ?? 'Recent'
}

function directionFromQuery(forSort: LibrarySort): SortDirection {
  const raw = typeof route.query.dir === 'string' ? route.query.dir : ''
  if (raw === 'asc') return 'Ascending'
  if (raw === 'desc') return 'Descending'
  return naturalDirection(forSort)
}

const initialPage = Number.parseInt(typeof route.query.page === 'string' ? route.query.page : '', 10)
const initialSort = sortFromQuery()
const initialSearch = typeof route.query.q === 'string' ? route.query.q : ''
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)
const sort = ref<LibrarySort>(initialSort)
const direction = ref<SortDirection>(directionFromQuery(initialSort))
// searchInput is bound to the field; search is the debounced value that actually drives the fetch.
const searchInput = ref(initialSearch)
const search = ref(initialSearch.trim())
const pageSize = 24

const { data, pending, error, refresh } = await useAsyncData(
  'meltho-library',
  () => listWords({ page: page.value, pageSize, sort: sort.value, direction: direction.value, search: search.value || undefined }),
  { watch: [page, sort, direction, search], lazy: true, default: () => null },
)

let searchDebounce: ReturnType<typeof setTimeout> | undefined
watch(searchInput, (value) => {
  clearTimeout(searchDebounce)
  searchDebounce = setTimeout(() => {
    const trimmed = value.trim()
    if (trimmed === search.value) return
    search.value = trimmed
    page.value = 1 // a new filter invalidates the current page offset
    syncQueryString()
  }, 250)
})

function clearSearch() {
  clearTimeout(searchDebounce)
  searchInput.value = ''
  if (search.value === '') return
  search.value = ''
  page.value = 1
  syncQueryString()
}

const totalPages = computed(() => Math.max(1, Math.ceil((data.value?.total ?? 0) / pageSize)))

const status = computed<'loading' | 'failed' | 'empty' | 'noResults' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) return 'failed'
  if ((data.value?.items ?? []).length === 0) return search.value ? 'noResults' : 'empty'
  return 'ready'
})

type LibraryWord = NonNullable<typeof data.value>['items'][number]
interface WordGroup { key: string, label: string, isSyriac: boolean, words: LibraryWord[] }

// The header a word falls under, derived from the active sort: first Syriac letter (alphabetical),
// month it was last shown (recent), or its letter count (length).
function groupOf(word: LibraryWord): { key: string, label: string, isSyriac: boolean } {
  if (sort.value === 'Alphabetical') {
    const first = Array.from(word.syriacUnvocalized)[0] ?? ''
    return { key: `letter:${first}`, label: first, isSyriac: true }
  }
  if (sort.value === 'Length') {
    return {
      key: `length:${word.playableLength}`,
      label: t('library.lettersCount', { count: word.playableLength }),
      isSyriac: false,
    }
  }
  const date = new Date(`${word.lastPlayedOn}T00:00:00`)
  return {
    key: `month:${date.getFullYear()}-${date.getMonth()}`,
    label: date.toLocaleDateString(locale.value, { month: 'long', year: 'numeric' }),
    isSyriac: false,
  }
}

// Walk the already-sorted page, opening a new section each time the group key changes. Grouping is
// presentational over the current page; with the small word pool a group never spans pages.
const groups = computed<WordGroup[]>(() => {
  const result: WordGroup[] = []
  for (const word of data.value?.items ?? []) {
    const { key, label, isSyriac } = groupOf(word)
    const current = result.at(-1)
    if (current && current.key === key) {
      current.words.push(word)
    }
    else {
      result.push({ key, label, isSyriac, words: [word] })
    }
  }
  return result
})

function syncQueryString() {
  const query: Record<string, string> = {}
  if (page.value > 1) query.page = String(page.value)
  const option = sortOptions.find(o => o.value === sort.value)
  if (option && option.key !== 'recent') query.sort = option.key
  // Only persist direction when it deviates from the field's natural default.
  if (direction.value !== naturalDirection(sort.value)) {
    query.dir = direction.value === 'Ascending' ? 'asc' : 'desc'
  }
  if (search.value) query.q = search.value
  router.replace({ query })
}

function setSort(next: LibrarySort) {
  if (sort.value === next) {
    // Re-clicking the active field toggles its direction.
    direction.value = direction.value === 'Ascending' ? 'Descending' : 'Ascending'
  }
  else {
    sort.value = next
    direction.value = naturalDirection(next)
  }
  page.value = 1 // a new ordering invalidates the current page offset
  syncQueryString()
}

function goTo(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value) return
  page.value = nextPage
  syncQueryString()
}
</script>

<template>
  <section class="mx-auto max-w-4xl">
    <header class="mb-7 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('library.title') }}
      </p>
      <h1 class="mt-3 font-serif text-4xl font-semibold tracking-[-0.02em] sm:text-[2.75rem]">{{ t('library.heading') }}</h1>
      <!-- The lede carries the living count once the first page has loaded. -->
      <p class="mt-3 max-w-2xl font-serif text-[17px] text-[var(--color-text-muted)]">
        <template v-if="data?.total">
          <strong class="font-semibold text-[var(--color-text)]">{{ t('library.count', { count: data.total }) }}</strong>
          {{ t('library.ledeCount') }}
        </template>
        <template v-else>{{ t('library.lede') }}</template>
      </p>
    </header>

    <!-- One toolbar: search grows, sort sits right. -->
    <div class="mb-2 flex flex-wrap items-center gap-3">
      <div class="flex min-w-[260px] flex-1 items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-4 py-2.5 focus-within:border-[var(--color-accent)]">
        <svg class="size-4 shrink-0 text-[var(--color-text-faint)]" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
          <circle cx="9" cy="9" r="6" />
          <path d="m17 17-3.5-3.5" stroke-linecap="round" />
        </svg>
        <input
          v-model="searchInput"
          type="search"
          :placeholder="t('library.searchPlaceholder')"
          :aria-label="t('library.searchPlaceholder')"
          class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none placeholder:text-[var(--color-text-faint)]"
        >
        <button
          v-if="searchInput"
          type="button"
          class="shrink-0 font-sans text-sm text-[var(--color-text-faint)] hover:text-[var(--color-text)]"
          :aria-label="t('library.searchClear')"
          @click="clearSearch"
        >✕</button>
      </div>
      <div class="flex flex-wrap gap-1.5" role="group" :aria-label="t('library.sort.label')">
        <button
          v-for="option in sortOptions"
          :key="option.key"
          type="button"
          class="inline-flex items-center gap-1 rounded-full border px-3.5 py-1.5 font-sans text-sm transition-colors"
          :class="sort === option.value
            ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] text-[var(--color-accent)] font-medium'
            : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)]'"
          :title="sort === option.value ? t(`library.direction.${direction === 'Ascending' ? 'ascending' : 'descending'}`) : undefined"
          :aria-pressed="sort === option.value"
          @click="setSort(option.value)"
        >
          {{ t(`library.sort.${option.key}`) }}
          <span v-if="sort === option.value" aria-hidden="true">{{ direction === 'Ascending' ? '↑' : '↓' }}</span>
        </button>
      </div>
    </div>

    <StateMessage
      v-if="status === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('library.loadFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <StateMessage
      v-else-if="status === 'empty'"
      variant="empty"
      :message="t('library.empty')"
    />
    <StateMessage
      v-else-if="status === 'noResults'"
      variant="empty"
      :message="t('library.noResults', { query: search })"
      :action-label="t('library.searchClear')"
      @action="clearSearch"
    />
    <template v-else>
      <section v-for="group in groups" :key="group.key" class="mb-9 mt-8">
        <h2 class="mb-3.5 flex items-baseline gap-2.5 border-b border-[var(--color-border)] pb-2">
          <SyriacText
            v-if="group.isSyriac"
            :text="group.label"
            class="!text-2xl text-[var(--color-accent)]"
          />
          <span v-else class="font-sans text-[12.5px] font-semibold uppercase tracking-[0.1em] text-[var(--color-text-muted)]">
            {{ group.label }}
          </span>
          <span class="font-sans text-xs text-[var(--color-text-faint)]">
            {{ t('library.count', { count: group.words.length }) }}
          </span>
        </h2>
        <ul class="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <li v-for="word in group.words" :key="word.lexiconEntryId">
            <!-- The word leads at full size; the date whispers; the letter
                 count is a small square tile — the game's own vocabulary. -->
            <NuxtLink
              :to="`/library/${word.lexiconEntryId}`"
              class="flex items-center gap-4 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-5 py-4 no-underline shadow-[var(--shadow-soft)] transition-[border-color,transform] hover:-translate-y-px hover:border-[color-mix(in_oklab,var(--color-accent)_45%,var(--color-border))]"
            >
              <SyriacText :text="word.syriacUnvocalized" class="min-w-[4.75rem] !text-[2.1rem] leading-tight text-[var(--color-accent)]" />
              <span class="min-w-0 flex-1">
                <span class="block truncate font-serif text-[17px]">
                  {{ preferredMeaning(word.meanings) }}
                  <span v-if="word.sblTransliteration" class="text-[15px] text-[var(--color-text-muted)] italic"> · {{ word.sblTransliteration }}</span>
                </span>
                <span class="mt-0.5 block font-sans text-[11.5px] text-[var(--color-text-faint)]">
                  {{ t('library.lastPlayed', { date: word.lastPlayedOn }) }}
                  <span v-if="Number(word.timesPlayed) > 1"> · {{ t('library.timesPlayed', { count: word.timesPlayed }) }}</span>
                </span>
              </span>
              <span
                class="grid size-[34px] shrink-0 place-items-center rounded-[9px] border border-[var(--color-border)] bg-[var(--color-bg-subtle)] font-sans text-[12.5px] font-semibold text-[var(--color-text-muted)]"
                :title="t('library.lettersCount', { count: word.playableLength })"
                :aria-label="t('library.lettersCount', { count: word.playableLength })"
              >
                {{ word.playableLength }}
              </span>
            </NuxtLink>
          </li>
        </ul>
      </section>

      <nav v-if="totalPages > 1" class="mt-8 flex items-center justify-center gap-4 font-sans text-sm">
        <button
          type="button"
          class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 disabled:opacity-40"
          :disabled="page <= 1"
          @click="goTo(page - 1)"
        >{{ t('pagination.previous') }}</button>
        <span class="text-[var(--color-text-muted)]">{{ t('pagination.pageOf', { page, total: totalPages }) }}</span>
        <button
          type="button"
          class="rounded-md border border-[var(--color-border-strong)] px-3 py-1.5 disabled:opacity-40"
          :disabled="page >= totalPages"
          @click="goTo(page + 1)"
        >{{ t('pagination.next') }}</button>
      </nav>
    </template>
  </section>
</template>
