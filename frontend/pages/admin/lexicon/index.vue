<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { GrammaticalCategory, LexiconAdminSort, LexiconEntryStatus, SortDirection } from '~/types/api'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { list } = useLexiconAdmin()

const route = useRoute()
const router = useRouter()

await refreshAdmin()

const categories: GrammaticalCategory[] = [
  'Noun',
  'Verb',
  'Adjective',
  'Adverb',
  'Pronoun',
  'Preposition',
  'Conjunction',
  'Particle',
  'Numeral',
  'Interjection',
  'Other',
]

const sorts: LexiconAdminSort[] = ['Recent', 'Syriac', 'Status', 'Length']

const pageSizeOptions = [20, 50, 100, 200] as const

function queryString(key: string): string {
  const value = route.query[key]
  return typeof value === 'string' ? value : ''
}

const initialPage = Number.parseInt(queryString('page'), 10)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)

const initialPageSize = Number.parseInt(queryString('pageSize'), 10)
const pageSize = ref(pageSizeOptions.includes(initialPageSize as typeof pageSizeOptions[number]) ? initialPageSize : 50)

const initialSearch = queryString('q')
// searchInput is bound to the field; search is the debounced value that actually drives the fetch.
const searchInput = ref(initialSearch)
const search = ref(initialSearch.trim())

const status = ref<LexiconEntryStatus | ''>(queryString('status') as LexiconEntryStatus | '')
const grammaticalCategory = ref<GrammaticalCategory | ''>(queryString('category') as GrammaticalCategory | '')
const playableInMeltho = ref<'' | 'true' | 'false'>(
  queryString('playable') === 'true' || queryString('playable') === 'false' ? (queryString('playable') as 'true' | 'false') : '',
)
const sort = ref<LexiconAdminSort>((queryString('sort') as LexiconAdminSort) || 'Recent')
const direction = ref<SortDirection | ''>(queryString('direction') as SortDirection | '')

const { data, pending, error, refresh } = await useAsyncData(
  'admin-lexicon',
  () => list({
    page: page.value,
    pageSize: pageSize.value,
    search: search.value || undefined,
    status: status.value || undefined,
    grammaticalCategory: grammaticalCategory.value || undefined,
    playableInMeltho: playableInMeltho.value === '' ? undefined : playableInMeltho.value === 'true',
    sort: sort.value,
    direction: direction.value || undefined,
  }),
  {
    watch: [page, pageSize, search, status, grammaticalCategory, playableInMeltho, sort, direction],
    lazy: true,
    default: () => null,
    immediate: isAdmin.value === true,
  },
)

const hasActiveFilters = computed(() =>
  Boolean(search.value || status.value || grammaticalCategory.value || playableInMeltho.value),
)

const totalPages = computed(() => {
  const total = data.value?.total ?? 0
  return Math.max(1, Math.ceil(total / pageSize.value))
})

const viewState = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'noResults' | 'ready'>(() => {
  if (isAdmin.value === false) return 'unauthorized'
  if (isAdmin.value === null || pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    return 'failed'
  }
  const items = data.value?.items ?? []
  if (items.length === 0) return hasActiveFilters.value ? 'noResults' : 'empty'
  return 'ready'
})

function lengthInRange(length: number) {
  return length >= 2 && length <= 8
}

function syncQueryString() {
  const query: Record<string, string> = {}
  if (page.value > 1) query.page = String(page.value)
  if (pageSize.value !== 50) query.pageSize = String(pageSize.value)
  if (search.value) query.q = search.value
  if (status.value) query.status = status.value
  if (grammaticalCategory.value) query.category = grammaticalCategory.value
  if (playableInMeltho.value) query.playable = playableInMeltho.value
  if (sort.value !== 'Recent') query.sort = sort.value
  if (direction.value) query.direction = direction.value
  router.replace({ query })
}

function goTo(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value) return
  page.value = nextPage
  syncQueryString()
}

// Any filter/search/sort change re-triages from page 1 — a stale page number
// past the new (smaller) result set would otherwise render an empty page.
function onFilterChange() {
  page.value = 1
  syncQueryString()
}

let searchDebounce: ReturnType<typeof setTimeout> | undefined
watch(searchInput, (value) => {
  clearTimeout(searchDebounce)
  searchDebounce = setTimeout(() => {
    const trimmed = value.trim()
    if (trimmed === search.value) return
    search.value = trimmed
    onFilterChange()
  }, 250)
})

function clearSearch() {
  clearTimeout(searchDebounce)
  searchInput.value = ''
  if (search.value === '') return
  search.value = ''
  onFilterChange()
}

function clearFilters() {
  clearSearch()
  status.value = ''
  grammaticalCategory.value = ''
  playableInMeltho.value = ''
  onFilterChange()
}
</script>

<template>
  <section>
    <header class="mb-8 flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="mb-2 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
          {{ t('nav.admin') }}
        </p>
        <h1 class="font-serif text-3xl font-semibold tracking-[-0.015em]">{{ t('admin.lexicon.title') }}</h1>
        <p class="mt-2 font-sans text-sm text-[var(--color-text-muted)]">
          {{ t('admin.lexicon.subtitle') }}
        </p>
      </div>
      <NuxtLink
        v-if="viewState === 'ready' || viewState === 'empty' || viewState === 'noResults'"
        to="/admin/lexicon/new"
        class="inline-flex items-center gap-2 rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-accent-hover)]"
      >
        <span aria-hidden="true">+</span> {{ t('admin.lexicon.newEntry') }}
      </NuxtLink>
    </header>

    <StateMessage
      v-if="viewState === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="viewState === 'unauthorized'"
      variant="unauthorized"
      :message="t('admin.adminRequired')"
      :hint="t('admin.adminRequiredHint')"
    />
    <StateMessage
      v-else-if="viewState === 'failed'"
      variant="failed"
      :message="t('admin.lexicon.loadFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <template v-else>
      <div class="mb-6 flex flex-wrap items-end gap-3">
        <div class="flex min-w-[240px] flex-1 items-center gap-2 rounded-xl border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-4 py-2.5 focus-within:border-[var(--color-accent)]">
          <svg class="size-4 shrink-0 text-[var(--color-text-faint)]" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
            <circle cx="9" cy="9" r="6" />
            <path d="m17 17-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input
            v-model="searchInput"
            type="search"
            :placeholder="t('admin.lexicon.filters.searchPlaceholder')"
            :aria-label="t('admin.lexicon.filters.searchPlaceholder')"
            class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none placeholder:text-[var(--color-text-faint)]"
          >
          <button
            v-if="searchInput"
            type="button"
            class="shrink-0 font-sans text-sm text-[var(--color-text-faint)] hover:text-[var(--color-text)]"
            :aria-label="t('admin.lexicon.filters.searchClear')"
            @click="clearSearch"
          >✕</button>
        </div>

        <div>
          <label for="filter-status" class="sr-only">{{ t('admin.lexicon.filters.statusLabel') }}</label>
          <select
            id="filter-status"
            v-model="status"
            class="rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]"
            @change="onFilterChange"
          >
            <option value="">{{ t('admin.lexicon.filters.statusAll') }}</option>
            <option value="Draft">{{ t('admin.lexicon.status.Draft') }}</option>
            <option value="Published">{{ t('admin.lexicon.status.Published') }}</option>
          </select>
        </div>

        <div>
          <label for="filter-category" class="sr-only">{{ t('admin.lexicon.filters.categoryLabel') }}</label>
          <select
            id="filter-category"
            v-model="grammaticalCategory"
            class="rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]"
            @change="onFilterChange"
          >
            <option value="">{{ t('admin.lexicon.filters.categoryAll') }}</option>
            <option v-for="category in categories" :key="category" :value="category">
              {{ t(`admin.lexicon.category.${category}`) }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-playable" class="sr-only">{{ t('admin.lexicon.filters.playableLabel') }}</label>
          <select
            id="filter-playable"
            v-model="playableInMeltho"
            class="rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]"
            @change="onFilterChange"
          >
            <option value="">{{ t('admin.lexicon.filters.playableAll') }}</option>
            <option value="true">{{ t('admin.lexicon.filters.playableYes') }}</option>
            <option value="false">{{ t('admin.lexicon.filters.playableNo') }}</option>
          </select>
        </div>

        <button
          v-if="hasActiveFilters"
          type="button"
          class="font-sans text-sm text-[var(--color-text-muted)] underline hover:text-[var(--color-text)]"
          @click="clearFilters"
        >{{ t('admin.lexicon.filters.clear') }}</button>

        <div class="ml-auto flex items-end gap-2">
          <div>
            <label for="filter-sort" class="sr-only">{{ t('admin.lexicon.filters.sortLabel') }}</label>
            <select
              id="filter-sort"
              v-model="sort"
              class="rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]"
              @change="onFilterChange"
            >
              <option v-for="s in sorts" :key="s" :value="s">{{ t(`admin.lexicon.sort.${s}`) }}</option>
            </select>
          </div>
          <button
            type="button"
            class="rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]"
            :aria-label="direction === 'Descending' ? t('admin.lexicon.filters.directionDescending') : t('admin.lexicon.filters.directionAscending')"
            @click="direction = direction === 'Descending' ? 'Ascending' : 'Descending'; onFilterChange()"
          >{{ direction === 'Descending' ? '↓' : '↑' }}</button>
        </div>
      </div>

      <StateMessage
        v-if="viewState === 'empty'"
        variant="empty"
        :message="t('admin.lexicon.empty')"
      />
      <StateMessage
        v-else-if="viewState === 'noResults'"
        variant="empty"
        :message="t('admin.lexicon.filters.noResults')"
        :action-label="t('admin.lexicon.filters.clear')"
        @action="clearFilters"
      />
      <template v-else>
        <div class="overflow-x-auto rounded-lg border border-[var(--color-border)]">
          <table class="w-full border-collapse text-left">
            <thead class="border-b border-[var(--color-border)] bg-[var(--color-bg-subtle)]">
              <tr class="font-sans text-xs uppercase tracking-wider text-[var(--color-text-muted)]">
                <th class="px-4 py-3 font-medium">{{ t('admin.lexicon.columns.syriac') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.lexicon.columns.transliteration') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.lexicon.columns.category') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.lexicon.columns.length') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.lexicon.columns.status') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.lexicon.columns.playable') }}</th>
                <th class="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="entry in data?.items ?? []"
                :key="entry.id"
                class="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-bg-subtle)]"
              >
                <td class="px-4 py-3">
                  <SyriacText :text="entry.syriacUnvocalized" class="!text-xl" />
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ entry.sblTransliteration || '—' }}
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ t(`admin.lexicon.category.${entry.grammaticalCategory}`) }}
                </td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex h-6 min-w-6 items-center justify-center rounded-full px-2 font-sans text-xs font-semibold"
                    :class="
                      lengthInRange(Number(entry.playableLength))
                        ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                        : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)] ring-1 ring-[var(--color-border-strong)]'
                    "
                  >{{ entry.playableLength }}</span>
                </td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
                    :class="
                      entry.status === 'Published'
                        ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                        : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'
                    "
                  >{{ t(`admin.lexicon.status.${entry.status}`) }}</span>
                </td>
                <td class="px-4 py-3 font-sans text-sm">
                  <span v-if="entry.playableInMeltho" :title="t('admin.lexicon.lifecycle.inPool')">✓</span>
                  <span v-else class="text-[var(--color-text-faint)]">—</span>
                </td>
                <td class="px-4 py-3 text-right">
                  <NuxtLink
                    :to="`/admin/lexicon/${entry.id}`"
                    class="font-sans text-sm font-medium text-[var(--color-accent)] no-underline hover:underline"
                  >{{ t('admin.lexicon.actions.edit') }}</NuxtLink>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <nav class="mt-6 flex flex-wrap items-center gap-4 font-sans text-sm">
          <template v-if="totalPages > 1">
            <button type="button" :disabled="page <= 1" class="disabled:opacity-40" @click="goTo(page - 1)">
              {{ t('pagination.previous') }}
            </button>
            <span class="text-[var(--color-text-muted)]">{{ t('pagination.pageOf', { page, total: totalPages }) }}</span>
            <button type="button" :disabled="page >= totalPages" class="disabled:opacity-40" @click="goTo(page + 1)">
              {{ t('pagination.next') }}
            </button>
          </template>
          <label class="ml-auto flex items-center gap-2 text-[var(--color-text-muted)]">
            {{ t('admin.lexicon.filters.pageSizeLabel') }}
            <select
              v-model.number="pageSize"
              class="rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2 py-1 font-sans text-sm text-[var(--color-text)]"
              @change="onFilterChange"
            >
              <option v-for="size in pageSizeOptions" :key="size" :value="size">{{ size }}</option>
            </select>
          </label>
        </nav>
      </template>
    </template>
  </section>
</template>
