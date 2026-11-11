<script setup lang="ts">
import { GRAMMAR_TENSES, type GrammarVerb } from '~/data/grammar'

/**
 * A verb's examples on its page in the Lexicon (Owner, 2026-11-10: "for now"): the "he" form of each
 * tense the Grammar has for it, straight from its tables, with what it says in the reader's language.
 * Forms, not sentences: real sentences wait for the Owner to dictate them.
 *
 * The translations follow the verb's meanings in the Lexicon, one per meaning, but they live in the
 * locale files (`grammar.examples.<verb>.<tense>`): when a model verb's meanings change in the
 * backoffice, change them here too.
 */
const props = defineProps<{ verb: GrammarVerb }>()

const { t, te } = useI18n()

const examples = computed(() => GRAMMAR_TENSES.flatMap((tense) => {
  const table = props.verb.tables.find(item => item.tense === tense)
  const form = table?.rows.find(row => row.person === 'he')?.cells[0]?.[0]
  const key = `grammar.examples.${props.verb.verb}.${tense}`
  return form && te(key) ? [{ tense, form, says: t(key) }] : []
}))
</script>

<template>
  <div v-if="examples.length" class="border-t border-[var(--color-border)] px-6 py-4 sm:px-9">
    <h2 class="font-sans text-[11px] font-bold uppercase tracking-[0.08em] text-[var(--color-text-faint)]">{{ t('grammar.examplesHeading') }}</h2>
    <ul class="mt-2 flex list-none flex-col gap-1.5 p-0">
      <li v-for="example in examples" :key="example.tense" class="flex items-center gap-4">
        <GrammarWord :text="example.form" :tense="example.tense" class="w-[5.5rem] shrink-0 text-[26px] leading-[1.7]" />
        <span class="min-w-0 flex-1">
          <span class="block font-serif text-[16.5px]">{{ example.says }}</span>
          <span class="block font-sans text-[12px] text-[var(--color-text-faint)]">{{ t(`grammar.tenses.${example.tense}`) }}</span>
        </span>
      </li>
    </ul>
  </div>
</template>
