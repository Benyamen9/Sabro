<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { ChantStatus } from '~/types/api'

// Editorial surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { canEdit, canViewBackoffice, refresh: refreshAccess } = useMyAccess()
const { list, listModes } = useChantsAdmin()

const route = useRoute()
const router = useRouter()

await refreshAdmin()
await refreshAccess()

// A reviewer browses the treasury but adds nothing. Offering only — the API
// refuses independently.
const mayEdit = computed(() => canEdit('Nahlo'))
const mayView = computed(() => canViewBackoffice('Nahlo'))

const pageSizeOptions = [25, 50, 100] as const

function queryString(key: string): string {
  const value = route.query[key]
  return typeof value === 'string' ? value : ''
}

const initialPage = Number.parseInt(queryString('page'), 10)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)

const initialPageSize = Number.parseInt(queryString('pageSize'), 10)
const pageSize = ref(
  pageSizeOptions.includes(initialPageSize as typeof pageSizeOptions[number]) ? initialPageSize : 25,
)

const initialSearch = queryString('q')
// searchInput is bound to the field; search is the debounced value that actually drives the fetch.
const searchInput = ref(initialSearch)
const search = ref(initialSearch.trim())

const status = ref<ChantStatus | ''>(queryString('status') as ChantStatus | '')
const modeId = ref(queryString('mode'))
const playableInNahlo = ref<'' | 'true' | 'false'>(
  queryString('playable') === 'true' || queryString('playable') === 'false'
    ? (queryString('playable') as 'true' | 'false')
    : '',
)

// The modes come from the API rather than a client-side constant: the set grows
// as the owner works through the tradition, and some sets run past eight.
const { data: modes } = await useAsyncData(
  'admin-chant-modes',
  () => listModes(),
  { lazy: true, default: () => [], immediate: isAdmin.value === true },
)

const { data, pending, error, refresh } = await useAsyncData(
  'admin-chants',
  () => list({
    page: page.value,
    pageSize: pageSize.value,
    search: search.value || undefined,
    status: status.value || undefined,
    modeId: modeId.value || undefined,
    playableInNahlo: playableInNahlo.value === '' ? undefined : playableInNahlo.value === 'true',
  }),
  {
    watch: [page, pageSize, search, status, modeId, playableInNahlo],
    lazy: true,
    default: () => null,
    immediate: isAdmin.value === true,
  },
)

const hasActiveFilters = computed(() =>
  Boolean(search.value || status.value || modeId.value || playableInNahlo.value))

const total = computed(() => data.value?.total ?? 0)

const totalPages = computed(() => Math.max(1, Math.ceil((data.value?.total ?? 0) / pageSize.value)))

const viewState = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'noResults' | 'ready'>(() => {
  // Both locks: the admin scope says staff, the Nahlo grant says this area.
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
  if (pageSize.value !== 25) query.pageSize = String(pageSize.value)
  if (search.value) query.q = search.value
  if (status.value) query.status = status.value
  if (modeId.value) query.mode = modeId.value
  if (playableInNahlo.value) query.playable = playableInNahlo.value
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
  modeId.value = ''
  playableInNahlo.value = ''
  onFilterChange()
}

const selectClass
  = 'rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)]'
</script>

<template>
  <section>
    <AdminBreadcrumb section-key="admin.sections.chants.label" section-to="/admin/chants" />
    <AdminSectionNav />

    <AdminPageHeader :title="t('admin.chants.title')" :subtitle="t('admin.chants.subtitle')">
      <template v-if="viewState === 'ready' || viewState === 'noResults'" #stats>
        <AdminStat :value="total" :label="t('admin.chants.statMatching', total)" />
      </template>

      <template v-if="mayEdit && (viewState === 'ready' || viewState === 'empty' || viewState === 'noResults')" #actions>
        <NuxtLink
          to="/admin/chants/new"
          class="inline-flex items-center gap-2 rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-accent-hover)]"
        >
          <span aria-hidden="true">+</span> {{ t('admin.chants.newChant') }}
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
      :message="t('admin.chants.loadFailed')"
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
            :placeholder="t('admin.chants.filters.searchPlaceholder')"
            :aria-label="t('admin.chants.filters.searchPlaceholder')"
            class="min-w-0 flex-1 bg-transparent font-sans text-[15px] text-[var(--color-text)] outline-none placeholder:text-[var(--color-text-faint)]"
          >
          <button
            v-if="searchInput"
            type="button"
            class="shrink-0 font-sans text-sm text-[var(--color-text-faint)] hover:text-[var(--color-text)]"
            :aria-label="t('admin.chants.filters.searchClear')"
            @click="clearSearch"
          >✕</button>
        </div>

        <div>
          <label for="filter-status" class="sr-only">{{ t('admin.chants.filters.statusLabel') }}</label>
          <select id="filter-status" v-model="status" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.chants.filters.statusAll') }}</option>
            <option value="Draft">{{ t('admin.chants.status.Draft') }}</option>
            <option value="Published">{{ t('admin.chants.status.Published') }}</option>
          </select>
        </div>

        <div>
          <label for="filter-mode" class="sr-only">{{ t('admin.chants.filters.modeLabel') }}</label>
          <select id="filter-mode" v-model="modeId" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.chants.filters.modeAll') }}</option>
            <option v-for="mode in modes" :key="mode.id" :value="mode.id">
              {{ mode.position }} · {{ mode.name }}
            </option>
          </select>
        </div>

        <div>
          <label for="filter-playable" class="sr-only">{{ t('admin.chants.filters.playableLabel') }}</label>
          <select id="filter-playable" v-model="playableInNahlo" :class="selectClass" @change="onFilterChange">
            <option value="">{{ t('admin.chants.filters.playableAll') }}</option>
            <option value="true">{{ t('admin.chants.filters.playableYes') }}</option>
            <option value="false">{{ t('admin.chants.filters.playableNo') }}</option>
          </select>
        </div>

        <button
          v-if="hasActiveFilters"
          type="button"
          class="font-sans text-sm text-[var(--color-text-muted)] underline hover:text-[var(--color-text)]"
          @click="clearFilters"
        >{{ t('admin.chants.filters.clear') }}</button>
      </div>

      <StateMessage
        v-if="viewState === 'empty'"
        variant="empty"
        :message="t('admin.chants.empty')"
      />
      <StateMessage
        v-else-if="viewState === 'noResults'"
        variant="empty"
        :message="t('admin.chants.filters.noResults')"
        :action-label="t('admin.chants.filters.clear')"
        @action="clearFilters"
      />
      <template v-else>
        <div class="overflow-x-auto rounded-lg border border-[var(--color-border)]">
          <table class="w-full border-collapse text-left">
            <thead class="border-b border-[var(--color-border)] bg-[var(--color-bg-subtle)]">
              <tr class="font-sans text-xs uppercase tracking-wider text-[var(--color-text-muted)]">
                <th class="px-4 py-3 font-medium">{{ t('admin.chants.columns.melody') }}</th>
                <th class="hidden sm:table-cell px-4 py-3 font-medium">{{ t('admin.chants.columns.syriac') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.chants.columns.mode') }}</th>
                <th class="hidden md:table-cell px-4 py-3 font-medium">{{ t('admin.chants.columns.shuhlofo') }}</th>
                <th class="hidden md:table-cell px-4 py-3 font-medium">{{ t('admin.chants.columns.recording') }}</th>
                <th class="px-4 py-3 font-medium">{{ t('admin.chants.columns.status') }}</th>
                <th class="hidden md:table-cell px-4 py-3 font-medium">{{ t('admin.chants.columns.playable') }}</th>
                <th class="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="chant in data?.items ?? []"
                :key="chant.id"
                class="border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-bg-subtle)]"
              >
                <td class="px-4 py-3 font-serif text-[15px] text-[var(--color-text)]">
                  {{ chant.transliteration }}
                  <!-- A solqin borrows its melody, and which melody is the thing
                       to know about it at a glance. -->
                  <span
                    v-if="chant.inheritsMelodyFromTransliteration"
                    class="mt-0.5 block font-sans text-xs text-[var(--color-text-faint)]"
                  >{{ t('admin.chants.inheritsFrom', { melody: chant.inheritsMelodyFromTransliteration }) }}</span>
                </td>
                <td class="hidden sm:table-cell px-4 py-3">
                  <SyriacText :text="chant.syriacIncipit" class="!text-xl" />
                </td>
                <td class="px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ chant.modeName }}
                </td>
                <td class="hidden md:table-cell px-4 py-3 font-sans text-sm text-[var(--color-text-muted)]">
                  {{ chant.shuhlofo || '—' }}
                </td>
                <td class="hidden md:table-cell px-4 py-3 font-sans text-sm">
                  <span v-if="chant.audioUrl" :title="t('admin.chants.recording.present')">✓</span>
                  <span v-else class="text-[var(--color-text-faint)]" :title="t('admin.chants.recording.none')">—</span>
                </td>
                <td class="px-4 py-3">
                  <span
                    class="inline-flex items-center rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
                    :class="
                      chant.status === 'Published'
                        ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
                        : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'
                    "
                  >{{ t(`admin.chants.status.${chant.status}`) }}</span>
                </td>
                <td class="hidden md:table-cell px-4 py-3 font-sans text-sm">
                  <span v-if="chant.playableInNahlo" :title="t('admin.chants.lifecycle.inPool')">✓</span>
                  <span v-else class="text-[var(--color-text-faint)]">—</span>
                </td>
                <td class="px-4 py-3 text-right">
                  <NuxtLink
                    :to="`/admin/chants/${chant.id}`"
                    class="font-sans text-sm font-medium text-[var(--color-accent)] no-underline hover:underline"
                  >{{ t('admin.chants.actions.edit') }}</NuxtLink>
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
            {{ t('admin.chants.filters.pageSizeLabel') }}
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
