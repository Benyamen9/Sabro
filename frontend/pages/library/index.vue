<script setup lang="ts">
const { t } = useI18n()
const { listWords } = useMelthoLibrary()
const preferredMeaning = usePreferredMeaning()

const route = useRoute()
const router = useRouter()

const initialPage = Number.parseInt(typeof route.query.page === 'string' ? route.query.page : '', 10)
const page = ref(Number.isFinite(initialPage) && initialPage > 0 ? initialPage : 1)
const pageSize = 24

const { data, pending, error, refresh } = await useAsyncData(
  'meltho-library',
  () => listWords({ page: page.value, pageSize }),
  { watch: [page], lazy: true, default: () => null },
)

const totalPages = computed(() => Math.max(1, Math.ceil((data.value?.total ?? 0) / pageSize)))

const status = computed<'loading' | 'failed' | 'empty' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) return 'failed'
  if ((data.value?.items ?? []).length === 0) return 'empty'
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
  <section class="mx-auto max-w-4xl">
    <header class="mb-8">
      <h1 class="font-serif text-3xl sm:text-4xl">{{ t('library.heading') }}</h1>
      <p class="mt-3 font-serif text-[var(--color-text-muted)]">{{ t('library.lede') }}</p>
    </header>

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
    <template v-else>
      <ul class="grid gap-3 sm:grid-cols-2">
        <li v-for="word in data?.items ?? []" :key="word.lexiconEntryId">
          <NuxtLink
            :to="`/library/${word.lexiconEntryId}`"
            class="flex items-center gap-4 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-5 no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-bg-subtle)]"
          >
            <SyriacText :text="word.syriacUnvocalized" class="!text-3xl text-[var(--color-accent)]" />
            <span class="min-w-0 flex-1">
              <span class="block truncate font-serif text-lg">{{ preferredMeaning(word.meanings) }}</span>
              <span class="mt-0.5 block font-sans text-xs text-[var(--color-text-faint)]">
                {{ t('library.lastPlayed', { date: word.lastPlayedOn }) }}
              </span>
            </span>
          </NuxtLink>
        </li>
      </ul>

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
