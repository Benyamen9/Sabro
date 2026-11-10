<script setup lang="ts">
import { GRAMMAR_TENSES, chapterForTense, type GrammarVerb } from '~/data/grammar'
import type { Tense } from '~/utils/tenseDots'

/**
 * A verb's conjugation on its own page in the Lexicon (Owner, 2026-11-10: the grammar is an
 * extension of the Lexicon, the tables belong to the words). One tense at a time, the table as the
 * grammar book sets it out, and under it the way to that tense's rules in the Grammar book.
 * `?tense=` opens on a tense, which is how a Grammar chapter links here.
 */
const props = defineProps<{ verb: GrammarVerb }>()

const { t } = useI18n()
const route = useRoute()

const tenses = computed(() => GRAMMAR_TENSES.filter(tense => props.verb.tables.some(table => table.tense === tense)))

function asked(): Tense {
  const value = typeof route.query.tense === 'string' ? route.query.tense : ''
  return tenses.value.find(tense => tense === value) ?? tenses.value[0] ?? 'past'
}

const tense = ref<Tense>(asked())
watch(() => [route.query.tense, props.verb.verb], () => { tense.value = asked() })

const table = computed(() => props.verb.tables.find(item => item.tense === tense.value) ?? props.verb.tables[0]!)
const chapter = computed(() => chapterForTense(table.value.tense))
</script>

<template>
  <section id="conjugation" class="mt-11 scroll-mt-24" :style="{ '--color-accent': 'var(--color-grammar)', '--color-accent-faint': 'var(--color-grammar-faint)' }">
    <div class="flex flex-wrap items-baseline gap-x-3.5 gap-y-1">
      <h2 class="font-serif text-[26px] font-semibold tracking-[-0.01em]">{{ t('grammar.conjugation') }}</h2>
      <span class="font-sans text-[12.5px] text-[var(--color-text-faint)]">
        {{ t(`grammar.categories.${verb.category}`) }} · <GrammarWord :text="verb.categorySyriac" class="text-[15px]" />
      </span>
    </div>

    <div v-if="tenses.length > 1" class="mt-3 flex flex-wrap gap-1.5" role="group" :aria-label="t('grammar.tensesLabel')">
      <button
        v-for="item in tenses"
        :key="item"
        type="button"
        class="rounded-full border px-3.5 py-1.5 font-sans text-sm transition-colors"
        :class="item === tense
          ? 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] font-medium text-[var(--color-accent)]'
          : 'border-[var(--color-border-strong)] text-[var(--color-text-muted)] hover:bg-[var(--color-bg-subtle)]'"
        :aria-pressed="item === tense"
        @click="tense = item"
      >{{ t(`grammar.tenses.${item}`) }}</button>
    </div>

    <div class="mt-3 overflow-hidden rounded-[20px] border border-[var(--color-border)] bg-[var(--color-bg-elevated)] shadow-[var(--shadow-soft)]">
      <GrammarTable :table="table" :caption="`${t(`grammar.tenses.${table.tense}`)} · ${t(`grammar.categories.${table.category}`)}`" />
      <p class="border-t border-[var(--color-border)] px-5 py-3 font-sans text-[12.5px] text-[var(--color-text-faint)]">
        {{ t(table.source === 'book' ? 'grammar.sourceBook' : 'grammar.sourceRules') }}
      </p>
    </div>

    <!-- The rules live in the Grammar book: one tap from the table they explain. -->
    <NuxtLink
      :to="`/library/grammar/${chapter.slug}`"
      class="mt-3 flex items-center gap-3 rounded-2xl border border-[color-mix(in_oklab,var(--color-accent)_25%,var(--color-border))] bg-[var(--color-accent-faint)] px-5 py-3.5 font-sans text-sm text-[var(--color-accent)] no-underline transition-colors hover:border-[var(--color-accent)]"
    >
      <span aria-hidden="true" class="h-[22px] w-4 shrink-0 rounded-[2px_4px_4px_2px] bg-[var(--color-accent)] shadow-[inset_3px_0_0_rgb(0_0_0/0.25)]" />
      <span class="min-w-0 flex-1">
        <span class="block font-medium">{{ t('grammar.howBuilt', { tense: t(`grammar.tenses.${table.tense}`) }) }}</span>
        <span class="block text-[12.5px] opacity-80">{{ t('grammar.rulesInGrammar') }}</span>
      </span>
      <span aria-hidden="true">→</span>
    </NuxtLink>
  </section>
</template>
