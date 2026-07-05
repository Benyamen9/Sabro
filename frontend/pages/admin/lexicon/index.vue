<script setup lang="ts">
import type { FetchError } from 'ofetch'

// Editorial / deferred surface — keep out of search indexes.
useSeoMeta({ robots: 'noindex, nofollow' })

const { t } = useI18n()
const { isAdmin, refresh: refreshAdmin } = useAdmin()
const { list } = useLexiconAdmin()

const route = useRoute()
const router = useRouter()

await refreshAdmin()

const initialPage = Number.parseInt(typeof route.query.page === 'string' ? route.query.page : '', 10)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)
const pageSize = 20

const { data, pending, error, refresh } = await useAsyncData(
  'admin-lexicon',
  () => list({ page: page.value, pageSize }),
  { watch: [page], lazy: true, default: () => null, immediate: isAdmin.value === true },
)

const totalPages = computed(() => {
  const total = data.value?.total ?? 0
  return Math.max(1, Math.ceil(total / pageSize))
})

const status = computed<'loading' | 'unauthorized' | 'failed' | 'empty' | 'ready'>(() => {
  if (isAdmin.value === false) return 'unauthorized'
  if (isAdmin.value === null || pending.value) return 'loading'
  if (error.value) {
    const fetchError = error.value as FetchError
    if (fetchError.statusCode === 401 || fetchError.statusCode === 403) return 'unauthorized'
    return 'failed'
  }
  const items = data.value?.items ?? []
  if (items.length === 0) return 'empty'
  return 'ready'
})

function lengthInRange(length: number) {
  return length >= 2 && length <= 8
}

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
        v-if="status === 'ready' || status === 'empty'"
        to="/admin/lexicon/new"
        class="inline-flex items-center gap-2 rounded-md bg-[var(--color-accent)] px-4 py-2 font-sans text-sm font-medium text-white no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-accent-hover)]"
      >
        <span aria-hidden="true">+</span> {{ t('admin.lexicon.newEntry') }}
      </NuxtLink>
    </header>

    <StateMessage
      v-if="status === 'loading'"
      variant="loading"
      :message="t('common.loading')"
    />
    <StateMessage
      v-else-if="status === 'unauthorized'"
      variant="unauthorized"
      :message="t('admin.adminRequired')"
      :hint="t('admin.adminRequiredHint')"
    />
    <StateMessage
      v-else-if="status === 'failed'"
      variant="failed"
      :message="t('admin.lexicon.loadFailed')"
      :action-label="t('common.retry')"
      @action="refresh()"
    />
    <StateMessage
      v-else-if="status === 'empty'"
      variant="empty"
      :message="t('admin.lexicon.empty')"
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

      <nav v-if="totalPages > 1" class="mt-6 flex items-center gap-4 font-sans text-sm">
        <button type="button" :disabled="page <= 1" class="disabled:opacity-40" @click="goTo(page - 1)">
          {{ t('pagination.previous') }}
        </button>
        <span class="text-[var(--color-text-muted)]">{{ t('pagination.pageOf', { page, total: totalPages }) }}</span>
        <button type="button" :disabled="page >= totalPages" class="disabled:opacity-40" @click="goTo(page + 1)">
          {{ t('pagination.next') }}
        </button>
      </nav>
    </template>
  </section>
</template>
