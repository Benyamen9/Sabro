<script setup lang="ts">
import { GRAMMAR_CHAPTERS } from '~/data/grammar'

/**
 * The Grammar book (ܓܪܰܡܰܛܺܝܩܺܝ; Owner, 2026-11-10): the rules of the verb, a few chapters of them.
 * The conjugation tables are not here: they belong to the words, on each verb's page in the Lexicon,
 * and every chapter points at the verbs to see its rules in. This page is the table of contents.
 */
const { t, locale } = useI18n()
const { chapterTitle, chapterLine, label } = useGrammar()

useSeoMeta({
  title: () => t('seo.grammar.title'),
  description: () => t('seo.grammar.description'),
  ogTitle: () => t('seo.grammar.title'),
  ogDescription: () => t('seo.grammar.description'),
})

// "Where you left off": the chapter the red ribbon keeps (see useLibraryPlaces).
const { places, load: loadPlaces } = useLibraryPlaces()
onMounted(loadPlaces)
const place = computed(() => {
  const kept = places.value.grammar
  const named = kept ? label(kept.itemKey) : null
  return kept && named ? { itemKey: kept.itemKey, label: named } : null
})
</script>

<template>
  <section class="mx-auto max-w-3xl" :style="{ '--color-accent': 'var(--color-grammar)', '--color-accent-faint': 'var(--color-grammar-faint)' }">
    <header class="mb-6 pt-2">
      <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('library.shelf.books.grammar.title') }}
        <span dir="rtl" lang="syc" class="ml-1 font-syriac text-[15px] normal-case tracking-normal">ܓܪܰܡܰܛܺܝܩܺܝ</span>
      </p>
      <h1 class="mt-3 font-serif text-4xl font-semibold tracking-[-0.02em] text-balance sm:text-[2.75rem]">{{ t('grammar.heading') }}</h1>
      <p class="mt-3 max-w-2xl font-serif text-[17px] text-[var(--color-text-muted)]">{{ t('grammar.lede') }}</p>
      <p v-if="locale !== 'en'" class="mt-2 font-sans text-[13px] text-[var(--color-text-faint)]">{{ t('grammar.englishOnly') }}</p>
    </header>

    <ClientOnly>
      <LibraryResume v-if="place" :label="place.label" :to="`/library/grammar/${place.itemKey}`" />
    </ClientOnly>

    <ol>
      <li v-for="(chapter, index) in GRAMMAR_CHAPTERS" :key="chapter.slug" class="border-t border-[var(--color-border)]">
        <NuxtLink :to="`/library/grammar/${chapter.slug}`" class="group flex items-center gap-3.5 py-4 no-underline">
          <span aria-hidden="true" class="w-5 shrink-0 text-right font-serif text-sm font-semibold tabular-nums text-[var(--color-accent)]">{{ index + 1 }}</span>
          <span class="min-w-0 flex-1">
            <span class="block font-serif text-[20px] font-semibold text-[var(--color-text)] group-hover:text-[var(--color-accent)]">{{ chapterTitle(chapter) }}</span>
            <span class="mt-0.5 block font-serif text-[15px] leading-snug text-[var(--color-text-muted)]">{{ chapterLine(chapter) }}</span>
          </span>
          <svg aria-hidden="true" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" class="size-4 shrink-0 text-[var(--color-text-faint)]"><path d="m6 3 5 5-5 5" stroke-linecap="round" stroke-linejoin="round" /></svg>
        </NuxtLink>
      </li>
    </ol>

    <!-- The other half of the grammar: the tables, with the words. -->
    <NuxtLink
      to="/library/lexicon"
      class="mt-6 flex items-center gap-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-meltho-faint)] px-5 py-3.5 font-sans text-sm text-[var(--color-meltho)] no-underline transition-colors hover:border-[var(--color-meltho)]"
    >
      <span aria-hidden="true" class="h-[22px] w-4 shrink-0 rounded-[2px_4px_4px_2px] bg-[var(--color-meltho)] shadow-[inset_3px_0_0_rgb(0_0_0/0.25)]" />
      <span class="min-w-0 flex-1">{{ t('grammar.tablesInLexicon') }}</span>
      <span aria-hidden="true">→</span>
    </NuxtLink>
  </section>
</template>
