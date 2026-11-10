<script setup lang="ts">
import type { BethGazoBookDto, HistoricalFigureListItem, PagedResult, ShmoCollectionDto } from '~/types/api'
import { GRAMMAR_CHAPTERS } from '~/data/grammar'
import { chantCount } from '~/utils/bethGazo'
import type { LibraryBook } from '~/utils/libraryPlaces'

/**
 * The Library shelf (Owner, 2026-11-01): one place to choose a book, each keeping its own page and
 * style. The covers carry live counts of what is published and, where the reader has a place, the
 * red ribbon. The Grammar joined the shelf with its first edition (2026-11-10).
 */
const { t } = useI18n()
const api = useSabroApi()
const { isConfigured, isSignedIn } = useAuth()
const { places, load } = useLibraryPlaces()

useSeoMeta({
  title: () => t('seo.libraryShelf.title'),
  description: () => t('seo.libraryShelf.description'),
  ogTitle: () => t('seo.libraryShelf.title'),
  ogDescription: () => t('seo.libraryShelf.description'),
})

/** What each book holds today, read from the same public endpoints the books themselves use. */
const { data: counts } = useAsyncData('library-shelf-counts', async () => {
  const [words, book, figures] = await Promise.allSettled([
    api<PagedResult<unknown>>('/dictionary', { query: { page: 1, pageSize: 1 } }),
    api<BethGazoBookDto>('/chants/book'),
    api<PagedResult<HistoricalFigureListItem>>('/historical-figures', { query: { page: 1, pageSize: 1 } }),
  ])
  return {
    words: words.status === 'fulfilled' ? Number(words.value.total) : null,
    chants: book.status === 'fulfilled' ? chantCount(book.value.sections) : null,
    figures: figures.status === 'fulfilled' ? Number(figures.value.total) : null,
  }
})

/** For a signed-in reader, how many of Beth Ṣalme's figures they have found in Shmo. */
const found = ref<number | null>(null)

onMounted(async () => {
  load()
  if (!isConfigured.value || !isSignedIn.value) return
  try {
    const collection = await api<ShmoCollectionDto>('/play/shmo/collection')
    found.value = collection.cards.filter(card => card.status === 'Found').length
  }
  catch {
    // The cover simply shows the roster's size.
  }
})

const books = computed(() => {
  const c = counts.value
  const figureCount = c?.figures ?? null
  return [
    {
      book: 'lexicon' as LibraryBook,
      title: t('library.shelf.books.lexicon.title'),
      syriacTitle: 'ܠܶܟܣܺܝܩܳܘܢ',
      subtitle: t('library.shelf.books.lexicon.subtitle'),
      count: c?.words != null ? t('library.shelf.counts.words', { count: c.words }, c.words) : null,
    },
    {
      book: 'grammar' as LibraryBook,
      title: t('library.shelf.books.grammar.title'),
      syriacTitle: 'ܓܪܰܡܰܛܺܝܩܺܝ',
      subtitle: t('library.shelf.books.grammar.subtitle'),
      count: t('library.shelf.counts.chapters', { count: GRAMMAR_CHAPTERS.length }, GRAMMAR_CHAPTERS.length),
    },
    {
      book: 'beth-gazo' as LibraryBook,
      title: t('library.shelf.books.bethGazo.title'),
      syriacTitle: 'ܒܝܬ ܓܙܐ',
      subtitle: t('library.shelf.books.bethGazo.subtitle'),
      count: c?.chants != null ? t('library.shelf.counts.chants', { count: c.chants }, c.chants) : null,
    },
    {
      book: 'beth-salme' as LibraryBook,
      title: t('library.shelf.books.bethSalme.title'),
      syriacTitle: 'ܒܝܬ ܨܠܡ̈ܐ',
      subtitle: t('library.shelf.books.bethSalme.subtitle'),
      count: figureCount == null
        ? null
        : found.value != null
          ? t('library.shelf.counts.found', { found: found.value, total: figureCount })
          : t('library.shelf.counts.figures', { count: figureCount }, figureCount),
    },
  ]
})

const anyPlace = computed(() => Object.keys(places.value).length > 0)
</script>

<template>
  <section class="mx-auto max-w-5xl">
    <header class="mb-6 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('library.shelf.eyebrow') }}
      </p>
      <!-- "܀ Marked n" sits by the title (Owner, 2026-11-01), so the reader's marks are never below the books. -->
      <div class="mt-3 flex flex-wrap items-end gap-x-3 gap-y-2.5">
        <h1 class="flex-1 font-serif text-4xl font-semibold tracking-[-0.02em] sm:text-[2.75rem]">{{ t('library.shelf.heading') }}</h1>
        <div class="mb-1"><MarkedLauncher /></div>
      </div>
    </header>

    <div class="grid gap-3.5 md:grid-cols-4 md:gap-3.5">
      <LibraryCover
        v-for="item in books"
        :key="item.book"
        :book="item.book"
        :to="`/library/${item.book}`"
        :title="item.title"
        :syriac-title="item.syriacTitle"
        :subtitle="item.subtitle"
        :count="item.count"
        :placed="Boolean(places[item.book])"
        :place="places[item.book]?.label ?? null"
      />
    </div>

    <ClientOnly>
      <p v-if="anyPlace" class="mt-4 text-center font-sans text-[12.5px] text-[var(--color-text-faint)]">
        {{ t('library.shelf.hint') }}
      </p>
    </ClientOnly>
  </section>
</template>
