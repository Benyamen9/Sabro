<script setup lang="ts">
const { t } = useI18n()
const { board, loading, loaded, load } = useLeaderboard()

onMounted(() => load())

const top = computed(() => board.value?.top ?? [])
const me = computed(() => board.value?.me ?? null)
const hasBoard = computed(() => top.value.length > 0)
</script>

<template>
  <section
    class="rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-6 shadow-[var(--shadow-soft)]"
  >
    <div>
      <h2 class="font-serif text-lg font-semibold tracking-tight text-[var(--color-text)]">
        {{ t('account.leaderboard.heading') }}
      </h2>
      <p class="mt-0.5 font-sans text-sm text-[var(--color-text-muted)]">
        {{ t('account.leaderboard.subtitle') }}
      </p>
    </div>

    <!-- Your standing — always shown so the player sees their own streak + rank. -->
    <div
      v-if="me"
      class="mt-5 flex items-center justify-between gap-4 rounded-xl border border-[var(--color-accent)]/30 bg-[var(--color-accent-faint)] px-4 py-3"
    >
      <div class="min-w-0">
        <p class="font-sans text-xs font-medium uppercase tracking-wide text-[var(--color-accent)]">
          {{ t('account.leaderboard.you') }}
        </p>
        <p class="mt-0.5 font-sans text-sm text-[var(--color-text-muted)]">
          <template v-if="me.rank">{{ t('account.leaderboard.yourRank', { rank: me.rank }) }}</template>
          <template v-else-if="me.onLeaderboard">{{ t('account.leaderboard.optedInUnranked') }}</template>
          <template v-else>{{ t('account.leaderboard.notOptedIn') }}</template>
        </p>
      </div>
      <div class="shrink-0 text-right">
        <p class="font-serif text-2xl font-semibold tabular-nums text-[var(--color-accent)]">{{ me.longestStreak }}</p>
        <p class="font-sans text-xs text-[var(--color-text-muted)]">{{ t('account.leaderboard.dayStreak') }}</p>
      </div>
    </div>

    <p
      v-if="loading && !loaded"
      class="mt-5 font-sans text-sm text-[var(--color-text-muted)]"
    >{{ t('common.loading') }}</p>

    <!-- Empty: nobody has opted in with a streak yet. -->
    <div
      v-else-if="!hasBoard"
      class="mt-5 rounded-xl border border-dashed border-[var(--color-border-strong)] px-5 py-6 text-center"
    >
      <p class="font-sans text-sm text-[var(--color-text-muted)]">{{ t('account.leaderboard.empty') }}</p>
    </div>

    <!-- Top players. -->
    <ol v-else class="mt-5 space-y-1">
      <li
        v-for="entry in top"
        :key="entry.rank"
        class="flex items-center gap-3 rounded-lg px-3 py-2"
        :class="entry.isMe ? 'bg-[var(--color-accent-faint)]' : ''"
      >
        <span
          class="flex size-6 shrink-0 items-center justify-center rounded-full font-sans text-xs font-semibold tabular-nums"
          :class="
            Number(entry.rank) <= 3
              ? 'bg-[var(--color-accent)] text-white'
              : 'bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)]'
          "
        >{{ entry.rank }}</span>
        <span
          class="min-w-0 flex-1 truncate font-sans text-sm"
          :class="entry.isMe ? 'font-semibold text-[var(--color-text)]' : 'text-[var(--color-text)]'"
        >
          {{ entry.displayName }}
          <span v-if="entry.isMe" class="font-sans text-xs font-normal text-[var(--color-accent)]"> · {{ t('account.leaderboard.you') }}</span>
        </span>
        <span class="shrink-0 font-sans text-xs text-[var(--color-text-muted)] tabular-nums">
          {{ t('account.leaderboard.days', { count: entry.longestStreak }) }}
        </span>
      </li>
    </ol>
  </section>
</template>
