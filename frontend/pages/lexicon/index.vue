<script setup lang="ts">
import type { FetchError } from 'ofetch'
import type { LexiconEntryDto, LexiconSearchHitDto } from '~/types/api'

interface LexiconRow {
  id: string
  syriacUnvocalized: string
  sblTransliteration: string
  category: string
  firstMeaning: string | null
  rootForm: string | null
}

const { t } = useI18n()
const { list, search } = useLexicon()

const route = useRoute()
const router = useRouter()

const initialQuery = typeof route.query.q === 'string' ? route.query.q : ''
const initialPage = Number.parseInt(typeof route.query.page === 'string' ? route.query.page : '', 10)

const q = ref(initialQuery)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)
const pageSize = 20

function mapEntry(entry: LexiconEntryDto): LexiconRow {
  return {
    id: entry.id,
    syriacUnvocalized: entry.syriacUnvocalized,
    sblTransliteration: entry.sblTransliteration,
    category: entry.grammaticalCategory,
    firstMeaning: entry.meanings[0]?.text ?? null,
    rootForm: null,
  }
}

function mapHit(hit: LexiconSearchHitDto): LexiconRow {
  return {
    id: hit.id,
    syriacUnvocalized: hit.syriacUnvocalized,
    sblTransliteration: hit.sblTransliteration,
    category: hit.grammaticalCategory,
    firstMeaning: hit.meaningTexts[0] ?? null,
    rootForm: hit.rootForm,
  }
}

const { data, pending, error, refresh } = await useAsyncData(
  'lexicon-list',
  async () => {
    const trimmed = q.value.trim()
    if (trimmed.length === 0) {
      const result = await list({ page: page.value, pageSize })
      return {
        rows: result.items.map(mapEntry),
        total: result.total,
        page: result.page,
        pageSize: result.pageSize,
      }
    }
    const result = await search({ q: trimmed, page: page.value, pageSize })
    return {
      rows: result.items.map(mapHit),
      total: result.total,
      page: result.page,
      pageSize: result.pageSize,
    }
  },
  { watch: [q, page], lazy: true, default: () => null },
)

const totalPages = computed(() => {
  const total = data.value?.total ?? 0
  return Math.max(1, Math.ceil(total / pageSize))
})

const status = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'no-matches' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    return 'failed'
  }
  const rows = data.value?.rows ?? []
  if (rows.length === 0) {
    return q.value.trim().length > 0 ? 'no-matches' : 'empty'
  }
  return 'ready'
})

function syncQueryString() {
  const query: Record<string, string> = {}
  const trimmed = q.value.trim()
  if (trimmed) query.q = trimmed
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}

function onSubmit() {
  page.value = 1
  syncQueryString()
}

function goTo(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value) return
  page.value = nextPage
  syncQueryString()
}

watch(error, () => {
  // Surface a refresh on transient errors when the query changes
  if (!error.value) return
})
</script>

<template>
  <section class="lexicon-list">
    <header class="lexicon-list__header">
      <h1>{{ t('lexicon.title') }}</h1>
      <form class="lexicon-list__search" @submit.prevent="onSubmit">
        <input
          v-model="q"
          type="search"
          :placeholder="t('lexicon.searchPlaceholder')"
          :aria-label="t('lexicon.searchPlaceholder')"
        >
      </form>
    </header>

    <p v-if="status === 'loading'" class="lexicon-list__state">…</p>
    <p v-else-if="status === 'unauthorized'" class="lexicon-list__state">
      {{ t('lexicon.signInRequired') }}
    </p>
    <p v-else-if="status === 'failed'" class="lexicon-list__state lexicon-list__state--error">
      {{ t('lexicon.loadFailed') }}
      <button type="button" @click="refresh()">{{ t('pagination.next') }}</button>
    </p>
    <p v-else-if="status === 'empty'" class="lexicon-list__state">
      {{ t('lexicon.empty') }}
    </p>
    <p v-else-if="status === 'no-matches'" class="lexicon-list__state">
      {{ t('lexicon.noMatches') }}
    </p>
    <template v-else>
      <ul class="lexicon-list__items">
        <li v-for="row in data?.rows ?? []" :key="row.id" class="lexicon-list__item">
          <NuxtLink :to="`/lexicon/${row.id}`" class="lexicon-list__link">
            <SyriacText :text="row.syriacUnvocalized" />
            <span class="lexicon-list__transliteration">{{ row.sblTransliteration }}</span>
            <span class="lexicon-list__category">{{ t(`category.${row.category}`) }}</span>
            <span v-if="row.firstMeaning" class="lexicon-list__meaning">{{ row.firstMeaning }}</span>
          </NuxtLink>
        </li>
      </ul>

      <nav v-if="totalPages > 1" class="lexicon-list__pagination">
        <button type="button" :disabled="page <= 1" @click="goTo(page - 1)">
          {{ t('pagination.previous') }}
        </button>
        <span>{{ t('pagination.pageOf', { page, total: totalPages }) }}</span>
        <button type="button" :disabled="page >= totalPages" @click="goTo(page + 1)">
          {{ t('pagination.next') }}
        </button>
      </nav>
    </template>
  </section>
</template>

<style scoped>
.lexicon-list__header {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
  margin-bottom: 1.5rem;
}

.lexicon-list__search {
  flex: 1;
  min-width: 240px;
}

.lexicon-list__search input {
  width: 100%;
  padding: 0.5rem 0.75rem;
  font-size: 1rem;
  border: 1px solid #d0d0d0;
  border-radius: 4px;
}

.lexicon-list__state {
  padding: 1.5rem 0;
  color: #555;
}

.lexicon-list__state--error {
  color: #b00020;
}

.lexicon-list__items {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.lexicon-list__item {
  border-bottom: 1px solid #eee;
}

.lexicon-list__link {
  display: grid;
  grid-template-columns: minmax(120px, 1fr) minmax(120px, 1fr) auto 2fr;
  gap: 1rem;
  align-items: baseline;
  padding: 0.75rem 0.5rem;
  text-decoration: none;
  color: inherit;
}

.lexicon-list__link:hover {
  background: #f6f6f6;
}

.lexicon-list__transliteration {
  font-style: italic;
}

.lexicon-list__category {
  font-size: 0.875rem;
  color: #666;
}

.lexicon-list__meaning {
  color: #333;
}

.lexicon-list__pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1.5rem;
}

.lexicon-list__pagination button {
  padding: 0.4rem 0.8rem;
}
</style>
