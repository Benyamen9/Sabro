<script setup lang="ts">
/**
 * The header every backoffice section wears.
 *
 * The same eyebrow / title / subtitle block was written out on six pages. It
 * looked consistent because it had been copied carefully, which is the state a
 * component exists to make permanent rather than lucky.
 *
 * `stats` is the part that follows the hub's lead: a section should say what is
 * in it before it shows a table of it. Deliberately a thin line rather than the
 * hub's cards — on the front page the counts are the content, here the table is.
 */
defineProps<{
  title: string
  subtitle?: string
}>()

const { t } = useI18n()
</script>

<template>
  <header class="mb-8 flex flex-wrap items-end justify-between gap-4">
    <div>
      <p class="mb-2 font-sans text-xs font-medium uppercase tracking-[0.16em] text-[var(--color-accent)]">
        {{ t('nav.admin') }}
      </p>
      <h1 class="font-serif text-3xl font-semibold tracking-[-0.015em]">{{ title }}</h1>
      <p v-if="subtitle" class="mt-2 max-w-prose font-sans text-sm text-[var(--color-text-muted)]">
        {{ subtitle }}
      </p>

      <!-- Counts sit under the subtitle so the eye lands on what the section is
           before what is in it. Absent by default: a page with nothing worth
           counting should not grow an empty row. -->
      <div
        v-if="$slots.stats"
        class="mt-3 flex flex-wrap items-center gap-x-3 gap-y-1 font-sans text-sm text-[var(--color-text-muted)]"
      >
        <slot name="stats" />
      </div>
    </div>

    <slot name="actions" />
  </header>
</template>
