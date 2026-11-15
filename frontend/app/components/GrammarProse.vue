<script setup lang="ts">
import { grammarLinkFor } from '~/data/grammar'

/**
 * A sentence of the Grammar book's explanations: plain text with Syriac in {braces}, which is set in
 * the book's own font (see data/grammar.ts).
 *
 * A Syriac word that is a form of a model verb, or the name of a kind of root, opens that verb in
 * the Lexicon, on the tense the form belongs to (Owner, 2026-11-15: the books are linked together).
 * The page gives the way to each published verb (`grammarVerbPath`); a verb not published yet, and
 * a word that is no verb (a letter, a pronoun), stay plain text.
 */
const props = defineProps<{ text: string }>()

const verbPath = inject<(verb: string, tense?: string) => string | null>('grammarVerbPath', () => null)

const parts = computed(() => props.text.split(/\{([^}]*)\}/).map((value, index) => {
  const syriac = index % 2 === 1
  const link = syriac ? grammarLinkFor(value) : null
  return { value, syriac, to: link ? verbPath(link.verb, link.tense) : null }
}))
</script>

<template>
  <span><template v-for="(part, index) in parts" :key="index"><NuxtLink v-if="part.to" :to="part.to" class="prose-verb"><GrammarWord :text="part.value" class="prose-syriac" /></NuxtLink><GrammarWord v-else-if="part.syriac" :text="part.value" class="prose-syriac" /><template v-else>{{ part.value }}</template></template></span>
</template>

<style scoped>
.prose-syriac {
  font-size: 1.3em;
  line-height: 1;
}

/* A verb in the text reads as text: the Lexicon's colour shows only in a fine dotted line under
   it, and in the word itself once the reader points at it. */
.prose-verb {
  color: inherit;
  text-decoration: underline dotted var(--color-meltho);
  text-decoration-thickness: 1.5px;
  text-underline-offset: 0.45em;
  transition: color 0.15s ease;
}

.prose-verb:hover,
.prose-verb:focus-visible {
  color: var(--color-meltho);
}
</style>
