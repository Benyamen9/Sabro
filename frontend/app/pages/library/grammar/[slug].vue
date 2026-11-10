<script setup lang="ts">
import { GRAMMAR_CHAPTERS, GRAMMAR_VERBS, grammarChapter } from '~/data/grammar'

/**
 * One chapter of the Grammar book: a set of rules with small examples, then the verbs to see them
 * in. A verb opens its own page in the Lexicon, where its tables are (on the chapter's tense, when
 * the chapter is a tense). Opening a chapter moves the Grammar's red ribbon to it.
 */
const { t, locale } = useI18n()
const route = useRoute()
const { chapterTitle, chapterLine, label, useVerbs } = useGrammar()
const NuxtLink = resolveComponent('NuxtLink')

const slug = computed(() => String(route.params.slug))
const chapter = computed(() => grammarChapter(slug.value))
const number = computed(() => GRAMMAR_CHAPTERS.findIndex(item => item.slug === slug.value) + 1)
const next = computed(() => GRAMMAR_CHAPTERS[number.value] ?? null)

useSeoMeta({
  title: () => (chapter.value ? t('seo.grammarChapter.title', { chapter: chapterTitle(chapter.value) }) : null),
  description: () => (chapter.value ? chapterLine(chapter.value) : null),
  ogTitle: () => (chapter.value ? t('seo.grammarChapter.title', { chapter: chapterTitle(chapter.value) }) : null),
  ogDescription: () => (chapter.value ? chapterLine(chapter.value) : null),
})

if (import.meta.server && !chapter.value) setResponseStatus(useRequestEvent()!, 404)

const { data: links } = useVerbs()
const verbs = computed(() => GRAMMAR_VERBS.map((verb) => {
  const id = links.value.find(item => item.verb === verb.verb)?.id ?? null
  const tense = chapter.value?.tense
  // A tense chapter opens the verb on that tense, when the verb has a table for it.
  const query = tense && verb.tables.some(table => table.tense === tense) ? `?tense=${tense}` : ''
  return { ...verb, to: id ? `/library/lexicon/${id}${query}#conjugation` : null }
}))
const anyLinked = computed(() => verbs.value.some(verb => verb.to))

const { record } = useLibraryPlaces()
watch(slug, (value) => {
  if (import.meta.server || !grammarChapter(value)) return
  record('grammar', value, label(value))
}, { immediate: true })
</script>

<template>
  <section class="mx-auto max-w-3xl" :style="{ '--color-accent': 'var(--color-grammar)', '--color-accent-faint': 'var(--color-grammar-faint)' }">
    <NuxtLink to="/library/grammar" class="font-sans text-sm text-[var(--color-text-muted)] no-underline hover:text-[var(--color-text)]">
      ← {{ t('library.shelf.books.grammar.title') }}
    </NuxtLink>

    <StateMessage v-if="!chapter" variant="notFound" :message="t('grammar.notFound')" class="mt-6" />

    <template v-else>
      <header class="mt-5">
        <p class="font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">{{ t('grammar.chapterNumber', { number }) }}</p>
        <h1 class="mt-2 font-serif text-4xl font-semibold tracking-[-0.02em] text-balance sm:text-[2.75rem]">{{ chapterTitle(chapter) }}</h1>
        <p v-if="chapter.syriacTitle" class="mt-2 text-[19px] leading-relaxed text-[var(--color-text-muted)]"><GrammarWord :text="chapter.syriacTitle" /></p>
        <div class="mt-4">
          <MarkButton book="grammar" :item-key="chapter.slug" :label="label(chapter.slug)" :name="chapterTitle(chapter)" />
        </div>
        <!-- English content for now (Owner, 2026-11-09): the explanations are not interface strings. -->
        <p v-if="locale !== 'en'" class="mt-3 font-sans text-[13px] text-[var(--color-text-faint)]">{{ t('grammar.englishOnly') }}</p>
      </header>

      <section v-for="(section, index) in chapter.sections" :key="index" class="mt-7">
        <h2 v-if="section.heading" class="font-serif text-[22px] font-semibold tracking-[-0.01em]">{{ section.heading }}</h2>
        <p v-if="section.intro" class="font-serif text-[17px] leading-relaxed" :class="section.heading ? 'mt-2' : 'text-[var(--color-text-muted)]'">
          <GrammarProse :text="section.intro" />
        </p>
        <div v-if="section.examples" class="mt-3 grid gap-2.5" :class="section.examples.length > 2 ? 'grid-cols-3' : 'grid-cols-2'">
          <figure v-for="example in section.examples" :key="example.form" class="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-2 py-3 text-center">
            <GrammarWord :text="example.form" :tense="example.tense" class="text-[34px] leading-[1.6]" />
            <figcaption class="mt-1 font-sans text-[12.5px] leading-snug text-[var(--color-text-muted)]">{{ example.caption }}</figcaption>
          </figure>
        </div>
        <ol v-if="section.points" class="mt-3 grid list-decimal gap-2.5 pl-5 font-serif text-[16.5px] leading-relaxed marker:font-sans marker:text-sm marker:text-[var(--color-accent)]">
          <li v-for="point in section.points" :key="point.lead">
            <strong class="font-semibold"><GrammarProse :text="point.lead" /></strong>
            <template v-if="point.text">{{ ' ' }}<GrammarProse :text="point.text" /></template>
          </li>
        </ol>
      </section>

      <!-- The tables are with the words: each verb opens its page in the Lexicon. -->
      <section class="mt-9 rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-5 py-4">
        <h2 class="font-serif text-[20px] font-semibold tracking-[-0.01em]">{{ t('grammar.seeInVerb') }}</h2>
        <p class="mt-1 font-serif text-[15.5px] text-[var(--color-text-muted)]">{{ t(anyLinked ? 'grammar.seeInVerbLine' : 'grammar.verbsComing') }}</p>
        <ul class="mt-3 flex flex-wrap gap-1.5">
          <li v-for="verb in verbs" :key="verb.verb">
            <component
              :is="verb.to ? NuxtLink : 'span'"
              :to="verb.to ?? undefined"
              class="block rounded-xl border px-3 pt-0.5 pb-1 text-[23px] leading-snug no-underline"
              :class="verb.to
                ? 'border-[var(--color-border-strong)] text-[var(--color-meltho)] transition-colors hover:border-[var(--color-meltho)] hover:bg-[var(--color-meltho-faint)]'
                : 'border-dashed border-[var(--color-border)] text-[var(--color-text-muted)]'"
              :title="t(`grammar.categories.${verb.category}`)"
              :aria-label="verb.to ? t('grammar.openVerb', { kind: t(`grammar.categories.${verb.category}`) }) : undefined"
            ><GrammarWord :text="verb.model" /></component>
          </li>
        </ul>
      </section>

      <NuxtLink
        v-if="next"
        :to="`/library/grammar/${next.slug}`"
        class="mt-5 flex items-center gap-3 rounded-2xl border border-[var(--color-border)] px-5 py-3.5 no-underline transition-colors hover:border-[var(--color-accent)]"
      >
        <span class="min-w-0 flex-1">
          <span class="block font-sans text-xs text-[var(--color-text-muted)]">{{ t('grammar.nextChapter') }}</span>
          <span class="block font-serif text-[17px] font-semibold text-[var(--color-text)]">{{ chapterTitle(next) }}</span>
        </span>
        <span aria-hidden="true" class="text-[var(--color-accent)]">→</span>
      </NuxtLink>
    </template>
  </section>
</template>
