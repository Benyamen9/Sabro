<script setup lang="ts">
import { GRAMMAR_TENSES, GRAMMAR_TEXT, GRAMMAR_TITLES, grammarTablesOf } from '~/data/grammar'

/**
 * The Grammar book (ܓܪܰܡܰܛܺܝܩܺܝ; Owner, 2026-11-01, first edition 2026-11-10): the verb in the past,
 * the present and the future, as the Owner teaches it from his grammar. This page is its table of
 * contents: each tense, and under it one table per kind of root. A table is what the red ribbon
 * and a ܀ mark point at.
 */
const { t, locale } = useI18n()
const { categoryName, label } = useGrammar()

useSeoMeta({
  title: () => t('seo.grammar.title'),
  description: () => t('seo.grammar.description'),
  ogTitle: () => t('seo.grammar.title'),
  ogDescription: () => t('seo.grammar.description'),
})

const tenses = computed(() => GRAMMAR_TENSES.map(tense => ({
  tense,
  title: GRAMMAR_TITLES[tense],
  line: GRAMMAR_TEXT[tense].line,
  tables: grammarTablesOf(tense),
})))

// "Where you left off": the table the red ribbon keeps (see useLibraryPlaces).
const { places, load: loadPlaces } = useLibraryPlaces()
onMounted(loadPlaces)
const place = computed(() => {
  const kept = places.value.grammar
  const named = kept ? label(kept.itemKey) : null
  return kept && named ? { itemKey: kept.itemKey, label: named } : null
})
</script>

<template>
  <section class="mx-auto max-w-4xl" :style="{ '--color-accent': 'var(--color-grammar)', '--color-accent-faint': 'var(--color-grammar-faint)' }">
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

    <ol class="grid gap-9">
      <li v-for="(item, index) in tenses" :key="item.tense">
        <div class="flex items-baseline gap-3 border-b border-[var(--color-border)] pb-2">
          <span aria-hidden="true" class="font-serif text-sm font-semibold tabular-nums text-[var(--color-accent)]">{{ index + 1 }}</span>
          <h2 class="font-serif text-[26px] font-semibold tracking-[-0.01em]">{{ t(`grammar.tenses.${item.tense}`) }}</h2>
          <span class="font-sans text-xs text-[var(--color-text-faint)]">{{ t('grammar.tableCount', { count: item.tables.length }, item.tables.length) }}</span>
        </div>
        <p class="mt-2 text-[19px] leading-relaxed text-[var(--color-text-muted)]"><GrammarWord :text="item.title" /></p>
        <!-- English content for now (Owner, 2026-11-09): the explanations are not interface strings. -->
        <p class="mt-1 font-serif text-[15.5px] text-[var(--color-text-muted)]">{{ item.line }}</p>

        <ul class="mt-4 grid grid-cols-1 gap-2.5 sm:grid-cols-2">
          <li v-for="table in item.tables" :key="table.slug" class="relative">
            <NuxtLink
              :to="`/library/grammar/${table.slug}`"
              class="flex items-center gap-4 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] py-3 pr-14 pl-5 no-underline shadow-[var(--shadow-soft)] transition-[border-color,transform] hover:-translate-y-px hover:border-[color-mix(in_oklab,var(--color-accent)_45%,var(--color-border))]"
            >
              <GrammarWord :text="table.model" class="min-w-[4.5rem] text-[2rem] leading-tight text-[var(--color-accent)]" />
              <span class="min-w-0 flex-1">
                <span class="block truncate font-serif text-[16.5px] text-[var(--color-text)]">{{ categoryName(table) }}</span>
                <span class="block truncate text-[15px] text-[var(--color-text-faint)]"><GrammarWord :text="table.categorySyriac" /></span>
              </span>
            </NuxtLink>
            <!-- Beside the link, not inside it: marking a table must not open it. -->
            <MarkButton
              class="absolute top-1/2 right-2.5 -translate-y-1/2"
              variant="icon"
              book="grammar"
              :item-key="table.slug"
              :label="label(table.slug)"
              :name="`${t(`grammar.tenses.${item.tense}`)}, ${categoryName(table)}`"
            />
          </li>
        </ul>
      </li>
    </ol>
  </section>
</template>
