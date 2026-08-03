<script setup lang="ts">
/**
 * What is waiting, above the doors.
 *
 * The hub used to open with four identical doors, which made "grant somebody
 * access" — done twice ever — look exactly as urgent as the one thing actually
 * blocked on a decision. This says what the state of things is first.
 *
 * It reports, it does not nag. There is no count of unfinished work here: the
 * owner's answer is that the backoffice is not where their hours currently go,
 * and a panel that tallies undone work becomes something to scroll past —
 * taking the number that did matter with it.
 *
 * Anything that could not be read is simply absent. The proposals count is
 * Owner-only, the word count comes through Meilisearch, and neither is worth a
 * broken front page.
 */
const props = defineProps<{
  pendingProposals: number | null
  playableWords: number | null
  poolTarget: number
}>()

const { t } = useI18n()

const hasProposals = computed(() => (props.pendingProposals ?? 0) > 0)

/** Capped at 100%: passing the target is good news, not a broken bar. */
const poolPercent = computed(() => {
  if (props.playableWords === null || props.poolTarget <= 0) return 0
  return Math.min(100, Math.round((props.playableWords / props.poolTarget) * 100))
})

const cardClass
  = 'flex flex-col gap-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-5'
</script>

<template>
  <section
    v-if="props.pendingProposals !== null || props.playableWords !== null"
    class="mb-8 grid gap-4 sm:grid-cols-2"
  >
    <!-- Waiting on a decision. Carries the accent only when there is something
         to decide; an empty queue is good news and should look calm. -->
    <div
      v-if="props.pendingProposals !== null"
      :class="[cardClass, hasProposals ? 'border-l-[3px] border-l-[var(--color-mno)]' : '']"
    >
      <span class="font-serif text-3xl font-semibold tabular-nums tracking-[-0.02em]">
        {{ props.pendingProposals }}
      </span>
      <span class="font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('admin.hub.pendingProposals', props.pendingProposals) }}
      </span>
      <NuxtLink
        v-if="hasProposals"
        to="/admin/proposals"
        class="mt-1 self-start font-sans text-sm font-medium text-[var(--color-accent)] no-underline hover:underline"
      >{{ t('admin.hub.reviewProposals') }}</NuxtLink>
    </div>

    <!-- Where the pool stands. A number with nothing to compare it to says
         nothing, so it carries the target it is heading for. -->
    <div v-if="props.playableWords !== null" :class="cardClass">
      <span class="font-serif text-3xl font-semibold tabular-nums tracking-[-0.02em]">
        {{ props.playableWords }}
      </span>
      <span class="font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('admin.hub.playableWords', { target: props.poolTarget }) }}
      </span>
      <div
        class="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-[var(--color-bg-subtle)]"
        role="img"
        :aria-label="t('admin.hub.poolProgress', { count: props.playableWords, target: props.poolTarget })"
      >
        <div class="h-full rounded-full bg-[var(--color-meltho)]" :style="{ width: `${poolPercent}%` }" />
      </div>
    </div>
  </section>
</template>
