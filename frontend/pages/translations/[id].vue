<script setup lang="ts">
import type { FetchError } from 'ofetch'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { getSourceById, searchSegments } = useTranslations()

const id = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))

const initialQuery = typeof route.query.q === 'string' ? route.query.q : ''
const initialPage = Number.parseInt(typeof route.query.page === 'string' ? route.query.page : '', 10)

const q = ref(initialQuery)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)
const pageSize = 25

const { data: source, pending: sourcePending, error: sourceError } = await useAsyncData(
  () => `translations-source-${id.value}`,
  () => getSourceById(id.value),
  { watch: [id], lazy: true, default: () => null },
)

const { data: segments, pending: segmentsPending, error: segmentsError, refresh: refreshSegments } = await useAsyncData(
  () => `translations-segments-${id.value}`,
  () => searchSegments({ sourceId: id.value, q: q.value.trim() || undefined, page: page.value, pageSize }),
  { watch: [id, q, page], lazy: true, default: () => null },
)

const sourceStatus = computed<'loading' | 'unauthorized' | 'not-found' | 'failed' | 'ready'>(() => {
  if (sourcePending.value) return 'loading'
  if (sourceError.value) {
    const fetchError = sourceError.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    if (fetchError.statusCode === 404) return 'not-found'
    return 'failed'
  }
  if (!source.value) return 'not-found'
  return 'ready'
})

const segmentsStatus = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'no-matches' | 'ready'>(() => {
  if (segmentsPending.value) return 'loading'
  if (segmentsError.value) {
    const fetchError = segmentsError.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    return 'failed'
  }
  const items = segments.value?.items ?? []
  if (items.length === 0) {
    return q.value.trim().length > 0 ? 'no-matches' : 'empty'
  }
  return 'ready'
})

const totalPages = computed(() => {
  const total = segments.value?.total ?? 0
  return Math.max(1, Math.ceil(total / pageSize))
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
</script>

<template>
  <section class="source-detail">
    <p class="source-detail__back">
      <NuxtLink to="/translations">← {{ t('translations.back') }}</NuxtLink>
    </p>

    <StateMessage
      v-if="sourceStatus === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="sourceStatus === 'unauthorized'"
      variant="unauthorized"
      :message="t('translations.signInRequired')"
      :hint="t('auth.signInHint')"
    />
    <StateMessage
      v-else-if="sourceStatus === 'not-found'"
      variant="notFound"
      :message="t('translations.notFound')"
    />
    <StateMessage
      v-else-if="sourceStatus === 'failed'"
      variant="failed"
      :message="t('translations.loadFailed')"
    />
    <template v-else-if="source">
      <header class="source-detail__header">
        <h1>{{ source.title }}</h1>
        <dl class="source-detail__meta">
          <template v-if="source.originalLanguageCode">
            <dt>{{ t('translations.fields.originalLanguage') }}</dt>
            <dd>{{ source.originalLanguageCode }}</dd>
          </template>
          <template v-if="source.description">
            <dt>{{ t('translations.fields.description') }}</dt>
            <dd>{{ source.description }}</dd>
          </template>
        </dl>
      </header>

      <section class="source-detail__segments">
        <header class="source-detail__segments-header">
          <h2>{{ t('translations.segments.heading') }}</h2>
          <form class="source-detail__search" @submit.prevent="onSubmit">
            <input
              v-model="q"
              type="search"
              :placeholder="t('translations.searchPlaceholder')"
              :aria-label="t('translations.searchPlaceholder')"
            >
          </form>
        </header>

        <StateMessage
          v-if="segmentsStatus === 'loading'"
          variant="loading"
          :message="t('common.loading')"
          compact
        />
        <StateMessage
          v-else-if="segmentsStatus === 'unauthorized'"
          variant="unauthorized"
          :message="t('translations.signInRequired')"
          :hint="t('auth.signInHint')"
          compact
        />
        <StateMessage
          v-else-if="segmentsStatus === 'failed'"
          variant="failed"
          :message="t('translations.segments.loadFailed')"
          :action-label="t('common.retry')"
          compact
          @action="refreshSegments()"
        />
        <StateMessage
          v-else-if="segmentsStatus === 'empty'"
          variant="empty"
          :message="t('translations.segments.empty')"
          compact
        />
        <StateMessage
          v-else-if="segmentsStatus === 'no-matches'"
          variant="noMatches"
          :message="t('translations.segments.noMatches')"
          compact
        />
        <template v-else>
          <ol class="source-detail__segment-list">
            <li v-for="segment in segments?.items ?? []" :key="segment.id" class="source-detail__segment">
              <span class="source-detail__segment-ref">
                {{ t('translations.segments.reference', { chapter: segment.chapterNumber, verse: segment.verseNumber }) }}
              </span>
              <span class="source-detail__segment-content">{{ segment.content }}</span>
            </li>
          </ol>

          <nav v-if="totalPages > 1" class="source-detail__pagination">
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
  </section>
</template>

<style scoped>
.source-detail__back {
  margin-bottom: 1rem;
}

.source-detail__back a {
  color: inherit;
  text-decoration: none;
}

.source-detail__back a:hover {
  text-decoration: underline;
}

.source-detail__header {
  margin-bottom: 2rem;
}

.source-detail__meta {
  margin-top: 1rem;
  display: grid;
  grid-template-columns: 220px 1fr;
  gap: 0.5rem 1.5rem;
}

/* Phones: label-over-value instead of a fixed 220px label column. */
@media (max-width: 640px) {
  .source-detail__meta {
    grid-template-columns: 1fr;
    gap: 0.25rem;
  }

  .source-detail__meta dd {
    margin-bottom: 0.75rem;
  }
}

.source-detail__meta dt {
  font-weight: 600;
  color: #444;
}

.source-detail__meta dd {
  margin: 0;
}

.source-detail__segments-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
  margin-bottom: 1rem;
}

.source-detail__search {
  flex: 1;
  min-width: 240px;
}

.source-detail__search input {
  width: 100%;
  padding: 0.5rem 0.75rem;
  font-size: 1rem;
  border: 1px solid #d0d0d0;
  border-radius: 4px;
}

.source-detail__segment-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.source-detail__segment {
  display: grid;
  grid-template-columns: 4rem 1fr;
  gap: 1rem;
  align-items: baseline;
  padding: 0.5rem 0;
  border-bottom: 1px solid #eee;
}

.source-detail__segment-ref {
  font-variant-numeric: tabular-nums;
  color: #666;
  font-size: 0.875rem;
}

.source-detail__segment-content {
  line-height: 1.5;
}

.source-detail__pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1.5rem;
}

.source-detail__pagination button {
  padding: 0.4rem 0.8rem;
}

</style>
