<script setup lang="ts">
const { t } = useI18n()
const { listWords } = useDictionary()
const preferredMeaning = usePreferredMeaning()

const route = useRoute()
const router = useRouter()

const initialSearch = typeof route.query.q === 'string' ? route.query.q : ''
// searchInput is bound to the field; search is the debounced value that actually drives the fetch.
const searchInput = ref(initialSearch)
const search = ref(initialSearch.trim())
const pageSize = 24

// The first page loads through useAsyncData (SSR-friendly, refetches whenever
// the search term changes). Every subsequent page is appended client-side by
// loadMore() below — the library scrolls continuously rather than paging.
const { data, pending, error, refresh } = await useAsyncData(
  'library-dictionary',
  () => listWords({ page: 1, pageSize, search: search.value || undefined }),
  { watch: [search], lazy: true, default: () => null },
)

type Word = NonNullable<typeof data.value>['items'][number]

const loadedWords = ref<Word[]>([])
const loadedPage = ref(1)
const total = ref(0)
const loadingMore = ref(false)
const loadMoreFailed = ref(false)

watch(data, (value) => {
  loadedWords.value = value?.items ?? []
  loadedPage.value = 1
  total.value = value?.total ?? 0
  loadMoreFailed.value = false
}, { immediate: true })

const hasMore = computed(() => loadedWords.value.length < total.value)

async function loadMore() {
  if (loadingMore.value || !hasMore.value) return
  loadingMore.value = true
  loadMoreFailed.value = false
  try {
    const result = await listWords({ page: loadedPage.value + 1, pageSize, search: search.value || undefined })
    loadedWords.value = [...loadedWords.value, ...result.items]
    loadedPage.value += 1
  }
  catch {
    loadMoreFailed.value = true
  }
  finally {
    loadingMore.value = false
  }
}

// Fires loadMore() once the sentinel below the list scrolls into view.
const sentinel = useTemplateRef<HTMLElement>('sentinel')
let observer: IntersectionObserver | undefined
onMounted(() => {
  observer = new IntersectionObserver((entries) => {
    if (entries.some(entry => entry.isIntersecting)) loadMore()
  })
  watch(sentinel, (el, prevEl) => {
    if (prevEl) observer!.unobserve(prevEl)
    if (el) observer!.observe(el)
  }, { immediate: true })
})
onUnmounted(() => observer?.disconnect())

let searchDebounce: ReturnType<typeof setTimeout> | undefined
watch(searchInput, (value) => {
  clearTimeout(searchDebounce)
  searchDebounce = setTimeout(() => {
    const trimmed = value.trim()
    if (trimmed === search.value) return
    search.value = trimmed
    syncQueryString()
  }, 250)
})

function clearSearch() {
  clearTimeout(searchDebounce)
  searchInput.value = ''
  if (search.value === '') return
  search.value = ''
  syncQueryString()
}

const status = computed<'loading' | 'failed' | 'empty' | 'noResults' | 'ready'>(() => {
  if (pending.value) return 'loading'
  if (error.value) return 'failed'
  if (loadedWords.value.length === 0) return search.value ? 'noResults' : 'empty'
  return 'ready'
})

interface WordGroup { key: string, label: string, words: Word[] }

// Browsing is alphabetical, so words group under their first Syriac letter.
// Search results are relevance-ordered — grouping them by letter would shuffle
// the ranking, so they render as one flat list.
const groups = computed<WordGroup[]>(() => {
  const items = loadedWords.value
  if (search.value) {
    return items.length ? [{ key: 'results', label: '', words: items }] : []
  }

  const result: WordGroup[] = []
  for (const word of items) {
    const first = Array.from(word.syriacUnvocalized)[0] ?? ''
    const current = result.at(-1)
    if (current && current.key === `letter:${first}`) {
      current.words.push(word)
    }
    else {
      result.push({ key: `letter:${first}`, label: first, words: [word] })
    }
  }
  return result
})

function syncQueryString() {
  const query: Record<string, string> = {}
  if (search.value) query.q = search.value
  router.replace({ query })
}

// The page header shows the living total once an unfiltered page has loaded.
const emit = defineEmits<{ total: [value: number] }>()
watch(data, (value) => {
  if (value && !search.value) emit('total', value.total)
})
</script>

<template>
  <div>
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
      :message="t('library.dictionary.empty')"
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
        <h2 v-if="group.label" class="mb-3.5 flex items-baseline gap-2.5 border-b border-[var(--color-border)] pb-2">
          <SyriacText
            :text="group.label"
            class="!text-2xl text-[var(--color-accent)]"
          />
          <span class="font-sans text-xs text-[var(--color-text-faint)]">
            {{ t('library.count', { count: group.words.length }) }}
          </span>
        </h2>
        <ul class="grid grid-cols-1 gap-3 sm:grid-cols-2" :class="{ 'mt-6': !group.label }">
          <li v-for="word in group.words" :key="word.id">
            <!-- Same card vocabulary as the Meltho view: the word leads, the
                 gloss follows, the letter count sits as a square tile. -->
            <NuxtLink
              :to="`/library/${word.id}`"
              class="flex items-center gap-4 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-5 py-4 no-underline shadow-[var(--shadow-soft)] transition-[border-color,transform] hover:-translate-y-px hover:border-[color-mix(in_oklab,var(--color-accent)_45%,var(--color-border))]"
            >
              <SyriacText :text="word.syriacVocalized ?? word.syriacUnvocalized" class="min-w-[4.75rem] !text-[2.1rem] leading-tight text-[var(--color-accent)]" />
              <span class="min-w-0 flex-1">
                <span class="block truncate font-serif text-[17px]">
                  {{ preferredMeaning(word.meanings) }}
                  <span v-if="word.sblTransliteration" class="text-[15px] text-[var(--color-text-muted)] italic"> · {{ word.sblTransliteration }}</span>
                </span>
                <span class="mt-0.5 block font-sans text-[11.5px] text-[var(--color-text-faint)]">
                  {{ t(`categories.${word.grammaticalCategory}`) }}
                </span>
              </span>
              <span
                class="grid size-[34px] shrink-0 place-items-center rounded-[9px] border border-[var(--color-border)] bg-[var(--color-bg-subtle)] font-sans text-[12.5px] font-semibold text-[var(--color-text-muted)]"
                :title="t('library.lettersCount', { count: word.letterCount })"
                :aria-label="t('library.lettersCount', { count: word.letterCount })"
              >
                {{ word.letterCount }}
              </span>
            </NuxtLink>
          </li>
        </ul>
      </section>

      <!-- Sentinel: an IntersectionObserver watches this element and calls
           loadMore() as it scrolls into view, so the library grows
           continuously instead of splitting into numbered pages. -->
      <div v-if="hasMore || loadMoreFailed" ref="sentinel" class="mt-8 flex items-center justify-center">
        <StateMessage
          v-if="loadMoreFailed"
          variant="failed"
          :message="t('library.loadFailed')"
          :action-label="t('common.retry')"
          @action="loadMore()"
        />
        <StateMessage
          v-else
          variant="loading"
          :message="t('common.loading')"
        />
      </div>
    </template>
  </div>
</template>
