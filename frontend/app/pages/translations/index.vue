<script setup lang="ts">
import type { FetchError } from 'ofetch'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { listSources } = useTranslations()

const route = useRoute()
const router = useRouter()

const initialPage = Number.parseInt(typeof route.query.page === 'string' ? route.query.page : '', 10)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)
const pageSize = 20

const { data, pending, error, refresh } = await useAsyncData(
  'translations-sources',
  () => listSources({ page: page.value, pageSize }),
  { watch: [page], lazy: true, default: () => null },
)

const totalPages = computed(() => {
  const total = data.value?.total ?? 0
  return Math.max(1, Math.ceil(total / pageSize))
})

const status = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    return 'failed'
  }
  const items = data.value?.items ?? []
  if (items.length === 0) return 'empty'
  return 'ready'
})

function syncQueryString() {
  const query: Record<string, string> = {}
  if (page.value > 1) query.page = String(page.value)
  router.replace({ query })
}

function goTo(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value) return
  page.value = nextPage
  syncQueryString()
}
</script>

<template>
  <section class="sources-list">
    <header class="sources-list__header">
      <h1>{{ t('translations.title') }}</h1>
    </header>

    <StateMessage
      v-if="status === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="status === 'unauthorized'"
      variant="unauthorized"
      :message="t('translations.signInRequired')"
      :hint="t('auth.signInHint')"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('translations.loadFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <StateMessage
      v-else-if="status === 'empty'"
      variant="empty"
      :message="t('translations.empty')"
    />
    <template v-else>
      <ul class="sources-list__items">
        <li v-for="source in data?.items ?? []" :key="source.id" class="sources-list__item">
          <NuxtLink :to="`/translations/${source.id}`" class="sources-list__link">
            <span class="sources-list__title">{{ source.title }}</span>
            <span v-if="source.originalLanguageCode" class="sources-list__lang">
              {{ source.originalLanguageCode }}
            </span>
            <span v-if="source.description" class="sources-list__description">
              {{ source.description }}
            </span>
          </NuxtLink>
        </li>
      </ul>

      <nav v-if="totalPages > 1" class="sources-list__pagination">
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
.sources-list__header {
  margin-bottom: 1.5rem;
}

.sources-list__items {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.sources-list__item {
  border-bottom: 1px solid #eee;
}

.sources-list__link {
  display: grid;
  grid-template-columns: minmax(220px, 2fr) auto 3fr;
  gap: 1rem;
  align-items: baseline;
  padding: 0.75rem 0.5rem;
  text-decoration: none;
  color: inherit;
}

/* Phones: the three columns stack into a single readable block. */
@media (max-width: 640px) {
  .sources-list__link {
    grid-template-columns: 1fr;
    gap: 0.25rem;
  }
}

.sources-list__link:hover {
  background: #f6f6f6;
}

.sources-list__title {
  font-weight: 600;
}

.sources-list__lang {
  font-size: 0.875rem;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.sources-list__description {
  color: #333;
}

.sources-list__pagination {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1.5rem;
}

.sources-list__pagination button {
  padding: 0.4rem 0.8rem;
}
</style>
