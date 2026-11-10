<script setup lang="ts">
/**
 * A sentence of the Grammar book's explanations: plain text with Syriac in {braces}, which is set in
 * the book's own font (see data/grammar.ts).
 */
const props = defineProps<{ text: string }>()

const parts = computed(() => props.text.split(/\{([^}]*)\}/).map((value, index) => ({ value, syriac: index % 2 === 1 })))
</script>

<template>
  <span><template v-for="(part, index) in parts" :key="index"><GrammarWord v-if="part.syriac" :text="part.value" class="prose-syriac" /><template v-else>{{ part.value }}</template></template></span>
</template>

<style scoped>
.prose-syriac {
  font-size: 1.3em;
  line-height: 1;
}
</style>
