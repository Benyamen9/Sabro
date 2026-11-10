<script setup lang="ts">
import { GRAMMAR_TENSES, GRAMMAR_TEXT, GRAMMAR_TITLES, grammarTable, grammarTablesOf, startsPlural } from '~/data/grammar'
import type { Tense } from '~/utils/tenseDots'

/**
 * One table of the Grammar book: a tense for one kind of root, as the book sets it out, read right
 * to left, with the tense dots in red. Under it, what is particular to this kind of root, how the
 * tense is built, and what the dots are doing. Opening a table moves the Grammar's red ribbon to it.
 */
const { t, locale } = useI18n()
const route = useRoute()
const { tenseName, categoryName, label } = useGrammar()

const slug = computed(() => String(route.params.slug))
const table = computed(() => grammarTable(slug.value))
const text = computed(() => (table.value ? GRAMMAR_TEXT[table.value.tense] : null))
const siblings = computed(() => (table.value ? grammarTablesOf(table.value.tense) : []))

/** The same verb in another tense, or that tense's first table when it has no table for this verb yet. */
function inTense(tense: Tense) {
  const model = slug.value.slice(slug.value.indexOf('-') + 1)
  const tables = grammarTablesOf(tense)
  return (tables.find(item => item.slug === `${tense}-${model}`) ?? tables[0])?.slug ?? null
}

const title = computed(() => (table.value ? `${tenseName(table.value)} · ${categoryName(table.value)}` : ''))

useSeoMeta({
  title: () => (table.value ? t('seo.grammarTable.title', { table: title.value }) : null),
  description: () => (table.value ? t('seo.grammarTable.description', { tense: tenseName(table.value), category: categoryName(table.value) }) : null),
  ogTitle: () => (table.value ? t('seo.grammarTable.title', { table: title.value }) : null),
  ogDescription: () => (table.value ? t('seo.grammarTable.description', { tense: tenseName(table.value), category: categoryName(table.value) }) : null),
})

if (import.meta.server && !table.value) setResponseStatus(useRequestEvent()!, 404)

const { record } = useLibraryPlaces()
watch(slug, (value) => {
  if (import.meta.server || !grammarTable(value)) return
  record('grammar', value, label(value))
}, { immediate: true })
</script>

<template>
  <section class="mx-auto max-w-3xl" :style="{ '--color-accent': 'var(--color-grammar)', '--color-accent-faint': 'var(--color-grammar-faint)' }">
    <NuxtLink to="/library/grammar" class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]">
      ← {{ t('library.shelf.books.grammar.title') }}
    </NuxtLink>

    <StateMessage v-if="!table || !text" variant="notFound" :message="t('grammar.notFound')" class="mt-6" />

    <template v-else>
      <!-- The three tenses, then the kinds of root this tense has a table for. -->
      <nav class="mt-5 flex flex-wrap gap-1.5" :aria-label="t('grammar.tensesLabel')">
        <NuxtLink
          v-for="tense in GRAMMAR_TENSES"
          :key="tense"
          :to="`/library/grammar/${inTense(tense)}`"
          class="rounded-full border px-3.5 py-1.5 font-sans text-sm no-underline transition-colors"
          :class="tense === table.tense
            ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] font-medium text-[var(--color-accent)]'
            : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)]'"
          :aria-current="tense === table.tense ? 'page' : undefined"
        >{{ t(`grammar.tenses.${tense}`) }}</NuxtLink>
      </nav>
      <nav v-if="siblings.length > 1" class="mt-2.5 flex max-w-full gap-1.5 overflow-x-auto pb-1 [scrollbar-width:none]" :aria-label="t('grammar.rootsLabel')">
        <NuxtLink
          v-for="item in siblings"
          :key="item.slug"
          :to="`/library/grammar/${item.slug}`"
          class="shrink-0 rounded-xl border px-3 pt-0.5 pb-1 text-[21px] leading-snug no-underline transition-colors"
          :class="item.slug === table.slug
            ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
            : 'border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)]'"
          :title="categoryName(item)"
          :aria-label="categoryName(item)"
          :aria-current="item.slug === table.slug ? 'page' : undefined"
        ><GrammarWord :text="item.model" /></NuxtLink>
      </nav>

      <article class="relative mt-4 overflow-hidden rounded-[20px] border border-[var(--color-border)] bg-[var(--color-bg-elevated)] shadow-[var(--shadow-soft)]">
        <!-- The ribbon hangs on the table being read: leave, and the Grammar reopens here. -->
        <span aria-hidden="true" class="absolute right-5 top-0 h-[62px] w-6 bg-[#8c2f39] drop-shadow-[0_2px_2px_rgb(0_0_0/0.25)] [clip-path:polygon(0_0,100%_0,100%_100%,50%_82%,0_100%)]" />
        <header class="px-5 pt-7 pb-5 text-center sm:px-9">
          <p class="font-sans text-[11px] font-semibold uppercase tracking-[0.14em] text-[var(--color-accent)]">{{ tenseName(table) }}</p>
          <h1 class="mt-1 text-[3rem] leading-[1.35] text-[var(--color-accent)] sm:text-[3.6rem]"><GrammarWord :text="table.model" /></h1>
          <p class="font-serif text-[19px] text-[var(--color-text)]">{{ categoryName(table) }}</p>
          <p class="text-[19px] text-[var(--color-text-muted)]"><GrammarWord :text="table.categorySyriac" /></p>
          <div class="mt-4 flex justify-center">
            <MarkButton book="grammar" :item-key="table.slug" :label="label(table.slug)" :name="title" />
          </div>
        </header>

        <!-- As in the book: read right to left, the persons down the right-hand side. -->
        <div class="overflow-x-auto border-t border-[var(--color-border)]">
          <table class="grammar-table w-full border-collapse" dir="rtl">
            <caption class="sr-only">{{ title }}</caption>
            <thead v-if="table.columns.length > 1">
              <tr>
                <td class="w-[30%] sm:w-[24%]" />
                <th v-for="column in table.columns" :key="column" scope="col" dir="ltr" class="font-sans text-[11.5px] font-semibold uppercase tracking-[0.08em] text-[var(--color-text-muted)]">
                  {{ t(`grammar.columns.${column}`) }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in table.rows" :key="row.person" :class="{ 'plural-start': startsPlural(row.person) }">
                <th scope="row" class="w-[30%] bg-[var(--color-bg-subtle)] sm:w-[24%]">
                  <GrammarWord :text="row.pronoun" class="block text-[20px] leading-snug" />
                  <span dir="ltr" class="block font-sans text-[11.5px] font-normal text-[var(--color-text-muted)]">{{ t(`grammar.persons.${row.person}`) }}</span>
                </th>
                <td v-for="(forms, index) in row.cells" :key="index">
                  <span v-if="!forms.length" class="text-[var(--color-text-faint)]">—</span>
                  <template v-for="(form, n) in forms" :key="form">
                    <span v-if="n > 0" aria-hidden="true" class="mx-2 text-[var(--color-text-faint)]">/</span>
                    <GrammarWord :text="form" :tense="table.tense" class="form" />
                  </template>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p class="border-t border-[var(--color-border)] px-5 py-3 font-sans text-[12.5px] text-[var(--color-text-faint)] sm:px-9">
          {{ t(table.source === 'book' ? 'grammar.sourceBook' : 'grammar.sourceRules') }}
        </p>
      </article>

      <!-- English content for now (Owner, 2026-11-09): the explanations are not interface strings. -->
      <p v-if="locale !== 'en'" class="mt-5 font-sans text-[13px] text-[var(--color-text-faint)]">{{ t('grammar.englishOnly') }}</p>

      <section v-if="text.categories[table.category]" class="mt-8">
        <h2 class="font-serif text-[22px] font-semibold tracking-[-0.01em]">{{ t('grammar.aboutRoot') }}</h2>
        <p class="mt-2 font-serif text-[16.5px] leading-relaxed"><GrammarProse :text="text.categories[table.category]!" /></p>
      </section>

      <section class="mt-8">
        <h2 class="font-serif text-[22px] font-semibold tracking-[-0.01em]">{{ t('grammar.howBuilt', { tense: tenseName(table) }) }}</h2>
        <p class="mt-1 text-[18px] leading-relaxed text-[var(--color-text-muted)]"><GrammarWord :text="GRAMMAR_TITLES[table.tense]" /></p>
        <ol class="mt-3 grid list-decimal gap-2.5 pl-5 font-serif text-[16.5px] leading-relaxed marker:font-sans marker:text-sm marker:text-[var(--color-accent)]">
          <li v-for="point in text.points" :key="point.lead">
            <strong class="font-semibold"><GrammarProse :text="point.lead" /></strong>
            <template v-if="point.text">{{ ' ' }}<GrammarProse :text="point.text" /></template>
          </li>
        </ol>
      </section>

      <section class="mt-8 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-5 py-4">
        <h2 class="flex items-center gap-2 font-serif text-[20px] font-semibold tracking-[-0.01em]">
          <span aria-hidden="true" class="size-2.5 rounded-full bg-[var(--color-tense-dot)]" />
          {{ t('grammar.dotsHeading') }}
        </h2>
        <p class="mt-2 font-serif text-[16.5px] leading-relaxed"><GrammarProse :text="text.dots" /></p>
      </section>
    </template>
  </section>
</template>

<style scoped>
.grammar-table th,
.grammar-table td {
  padding: 9px 8px;
  border-bottom: 1px solid var(--color-border);
  text-align: center;
  vertical-align: middle;
}

.grammar-table thead th,
.grammar-table thead td { padding-block: 8px; }
.grammar-table tbody tr:last-child th,
.grammar-table tbody tr:last-child td { border-bottom: 0; }
.grammar-table tbody th { border-left: 1px solid var(--color-border); }
.grammar-table td + td { border-right: 1px solid var(--color-border); }

/* The book's heavier line where the plural begins. */
.grammar-table tr.plural-start th,
.grammar-table tr.plural-start td { border-top: 2px solid var(--color-border-strong); }

.grammar-table .form {
  font-size: 27px;
  line-height: 1.7;
}

@media (max-width: 480px) {
  .grammar-table .form { font-size: 24px; }
}
</style>
