<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type {
  HistoricalFigureCategory,
  HistoricalFigureRegion,
  HistoricalFigureRole,
  HistoricalFigureStatus,
  HistoricalPeriod,
} from '~/types/api'
import {
  HISTORICAL_FIGURE_CATEGORIES,
  HISTORICAL_FIGURE_REGIONS,
  HISTORICAL_FIGURE_ROLES,
  HISTORICAL_PERIODS,
  formatEra,
} from '~/utils/historicalFigures'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { canEdit, canViewBackoffice, refresh: refreshAccess } = useMyAccess()
const { list } = useHistoricalFiguresAdmin()

const route = useRoute()
const router = useRouter()

await refreshAdmin()
await refreshAccess()

// A reviewer browses the roster but creates nothing. Offering only — the API
// refuses independently.
const mayEdit = computed(() => canEdit('Shmo'))
const mayView = computed(() => canViewBackoffice('Shmo'))

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

const status = ref<HistoricalFigureStatus | ''>(queryString('status') as HistoricalFigureStatus | '')
const category = ref<HistoricalFigureCategory | ''>(queryString('category') as HistoricalFigureCategory | '')
const period = ref<HistoricalPeriod | ''>(queryString('period') as HistoricalPeriod | '')
const role = ref<HistoricalFigureRole | ''>(queryString('role') as HistoricalFigureRole | '')
const region = ref<HistoricalFigureRegion | ''>(queryString('region') as HistoricalFigureRegion | '')
const playableInShmo = ref<'' | 'true' | 'false'>(
  queryString('playable') === 'true' || queryString('playable') === 'false' ? (queryString('playable') as 'true' | 'false') : '',
)

const { data, pending, error, refresh } = await useAsyncData(
  'admin-historical-figures',
  () => list({
    page: page.value,
    pageSize: pageSize.value,
    search: search.value || undefined,
    status: status.value || undefined,
    category: category.value || undefined,
    period: period.value || undefined,
    role: role.value || undefined,
    region: region.value || undefined,
    playableInShmo: playableInShmo.value === '' ? undefined : playableInShmo.value === 'true',
  }),
  {
    watch: [page, pageSize, search, status, category, period, role, region, playableInShmo],
    lazy: true,
    default: () => null,
    immediate: isAdmin.value === true,
  },
)

const hasActiveFilters = computed(() =>
  Boolean(search.value || status.value || category.value || period.value || role.value || region.value || playableInShmo.value),
)

// The count under the title: how many entries match what is being looked at,
// which is the whole section when no filter is set.
const total = computed(() => data.value?.total ?? 0)

const totalPages = computed(() => {
  const total = data.value?.total ?? 0
  return Math.max(1, Math.ceil(total / pageSize.value))
})

const viewState = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'noResults' | 'ready'>(() => {
  // Both locks: the admin scope says staff, the Shmo grant says this area.
  if (isAdmin.value === false || !mayView.value) return 'unauthorized'
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

function syncQueryString() {
  const query: Record<string, string> = {}
  if (page.value > 1) query.page = String(page.value)
  if (pageSize.value !== 50) query.pageSize = String(pageSize.value)
  if (search.value) query.q = search.value
  if (status.value) query.status = status.value
  if (category.value) query.category = category.value
  if (period.value) query.period = period.value
  if (role.value) query.role = role.value
  if (region.value) query.region = region.value
  if (playableInShmo.value) query.playable = playableInShmo.value
  router.replace({ query })
}

function goTo(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value) return
  page.value = nextPage
  syncQueryString()
}

// Any filter/search change re-triages from page 1 — a stale page number past
// the new (smaller) result set would otherwise render an empty page.
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
  category.value = ''
  period.value = ''
  role.value = ''
  region.value = ''
  playableInShmo.value = ''
  onFilterChange()
}

const selectClass
  = 'rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]'
</script>

<template>
  <section>
    <AdminBreadcrumb section-key="admin.sections.figures.label" section-to="/admin/historical-figures" />
    <AdminSectionNav />

    <AdminPageHeader :title="t('admin.historicalFigures.title')" :subtitle="t('admin.historicalFigures.subtitle')">
      <template v-if="viewState === 'ready' || viewState === 'noResults'" #stats>
        <AdminStat :value="total" :label="t('admin.historicalFigures.statMatching', total)" />
      </template>

      <template v-if="mayEdit && (viewState === 'ready' || viewState === 'empty' || viewState === 'noResults')" #actions>
        <NuxtLink
          to="/admin/historical-figures/new"
          class="inline-flex items-center gap-2 rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-accent-hover)]"
        >
          <span aria-hidden="true">+</span> {{ t('admin.historicalFigures.newFigure') }}
        </NuxtLink>
      </template>
    </AdminPageHeader>

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
      :message="t('admin.historicalFigures.loadFailed')"
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
            :placeholder="t('admin.historicalFigures.filters.searchPlaceholder')"
            :aria-label="t('admin.historicalFigures.filters.searchPlaceholder')"
            class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none placeholder:text-[var(--color-text-faint)]"
          >
          <button
            v-if="searchInput"
            type="button"
            class="shrink-0 font-sans text-sm text-[var(--color-text-faint)] hover:text-[var(--color-text)]"
            :aria-label="t('admin.historicalFigures.filters.searchClear')"
            @click="clearSearch"
          >✕</button>
        </div>

        <div>
          <label for="filter-status" class="sr-only">{{ t('admin.historicalFigures.filters.statusLabel') }}</label>
          <select id="filter-status" v-model="status" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.historicalFigures.filters.statusAll') }}</option>
            <option value="Draft">{{ t('admin.historicalFigures.status.Draft') }}</option>
            <option value="Published">{{ t('admin.historicalFigures.status.Published') }}</option>
          </select>
        </div>

        <div>
          <label for="filter-category" class="sr-only">{{ t('admin.historicalFigures.filters.categoryLabel') }}</label>
          <select id="filter-category" v-model="category" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.historicalFigures.filters.categoryAll') }}</option>
            <option v-for="value in HISTORICAL_FIGURE_CATEGORIES" :key="value" :value="value">
              {{ t(`admin.historicalFigures.category.${value}`) }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-period" class="sr-only">{{ t('admin.historicalFigures.filters.periodLabel') }}</label>
          <select id="filter-period" v-model="period" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.historicalFigures.filters.periodAll') }}</option>
            <option v-for="value in HISTORICAL_PERIODS" :key="value" :value="value">
              {{ t(`admin.historicalFigures.period.${value}`) }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-role" class="sr-only">{{ t('admin.historicalFigures.filters.roleLabel') }}</label>
          <select id="filter-role" v-model="role" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.historicalFigures.filters.roleAll') }}</option>
            <option v-for="value in HISTORICAL_FIGURE_ROLES" :key="value" :value="value">
              {{ t(`admin.historicalFigures.role.${value}`) }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-region" class="sr-only">{{ t('admin.historicalFigures.filters.regionLabel') }}</label>
          <select id="filter-region" v-model="region" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.historicalFigures.filters.regionAll') }}</option>
            <option v-for="value in HISTORICAL_FIGURE_REGIONS" :key="value" :value="value">
              {{ t(`admin.historicalFigures.region.${value}`) }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-playable" class="sr-only">{{ t('admin.historicalFigures.filters.playableLabel') }}</label>
          <select id="filter-playable" v-model="playableInShmo" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.historicalFigures.filters.playableAll') }}</option>
            <option value="true">{{ t('admin.historicalFigures.filters.playableYes') }}</option>
            <option value="false">{{ t('admin.historicalFigures.filters.playableNo') }}</option>
          </select>
        </div>

        <button
          v-if="hasActiveFilters"
          type="button"
          class="font-sans text-sm text-[var(--color-text-muted)] underline hover:text-[var(--color-text)]"
          @click="clearFilters"
        >{{ t('admin.historicalFigures.filters.clear') }}</button>
      </div>

      <StateMessage
        v-if="viewState === 'empty'"
        variant="empty"
        :message="t('admin.historicalFigures.empty')"
      />
      <StateMessage
        v-else-if="viewState === 'noResults'"
        variant="empty"
        :message="t('admin.historicalFigures.filters.noResults')"
        :action-label="t('admin.historicalFigures.filters.clear')"
        @action="clearFilters"
      />
      <template v-else>
        <div class="overflow-x-auto rounded-lg border border-[var(--color-border)]">
          <table class="w-full border-collapse text-left">
            <thead class="border-b border-[var(--color-border)] bg-[var(--color-bg-subtle)]">
              <tr class="font-sans text-xs uppercase tracking-wider text-[var(--color-text-muted)]">
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.name') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.category') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.era') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.period') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.role') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.region') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.status') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.historicalFigures.columns.playable') }}</th>
                <th class="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="figure in data?.items ?? []"
                :key="figure.id"
                class="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-bg-subtle)]"
              >
                <td class="px-4 py-3 font-serif text-[15px] text-[var(--color-text)]">
                  {{ figure.name }}
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ t(`admin.historicalFigures.category.${figure.category}`) }}
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ formatEra(Number(figure.era), t) }}
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ t(`admin.historicalFigures.period.${figure.period}`) }}
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ t(`admin.historicalFigures.role.${figure.role}`) }}
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ t(`admin.historicalFigures.region.${figure.region}`) }}
                </td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
                    :class="
                      figure.status === 'Published'
                        ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                        : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'
                    "
                  >{{ t(`admin.historicalFigures.status.${figure.status}`) }}</span>
                </td>
                <td class="px-4 py-3 font-sans text-sm">
                  <span v-if="figure.playableInShmo" :title="t('admin.historicalFigures.lifecycle.inPool')">✓</span>
                  <span v-else class="text-[var(--color-text-faint)]">—</span>
                </td>
                <td class="px-4 py-3 text-right">
                  <NuxtLink
                    :to="`/admin/historical-figures/${figure.id}`"
                    class="font-sans text-sm font-medium text-[var(--color-accent)] no-underline hover:underline"
                  >{{ t('admin.historicalFigures.actions.edit') }}</NuxtLink>
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
            {{ t('admin.historicalFigures.filters.pageSizeLabel') }}
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
