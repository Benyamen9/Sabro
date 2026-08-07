<script setup lang="ts">
/**
 * One count in a section header's stat line: a number and what it counts.
 *
 * The number is set in the serif and tabular, matching the hub's cards, so the
 * figures across the backoffice read as the same family of thing. `tone` marks
 * the one that is waiting on somebody — the same gold the hub uses, kept away
 * from the accent so "act on this" and "this is waiting" stay distinguishable.
 */
withDefaults(
  defineProps<{
    value: number | string
    label: string
    tone?: 'plain' | 'waiting'
  }>(),
  { tone: 'plain' },
)
</script>

<template>
  <span class="inline-flex items-baseline gap-1.5">
    <span
      class="font-serif text-base font-semibold tabular-nums"
      :class="tone === 'waiting' ? 'text-[var(--color-mno)]' : 'text-[var(--color-text)]'"
    >{{ value }}</span>
    <span>{{ label }}</span>
  </span>
</template>
