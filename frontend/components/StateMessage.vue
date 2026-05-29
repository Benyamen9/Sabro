<script setup lang="ts">
type Variant = 'loading' | 'unauthorized' | 'failed' | 'empty' | 'noMatches' | 'notFound'

const props = withDefaults(
  defineProps<{
    variant: Variant
    message: string
    hint?: string
    actionLabel?: string
    compact?: boolean
  }>(),
  { compact: false },
)

defineEmits<{ (e: 'action'): void }>()

const tone = computed(() => {
  switch (props.variant) {
    case 'unauthorized':
      return 'accent'
    case 'failed':
      return 'danger'
    default:
      return 'muted'
  }
})

const badgeClass = computed(() => {
  const base
    = 'mx-auto flex h-12 w-12 items-center justify-center rounded-full ring-1'
  if (tone.value === 'accent') {
    return `${base} bg-[var(--color-accent-faint)] text-[var(--color-accent)] ring-[color-mix(in_oklab,var(--color-accent)_25%,transparent)]`
  }
  if (tone.value === 'danger') {
    return `${base} bg-[var(--color-accent-faint)] text-[var(--color-accent)] ring-[color-mix(in_oklab,var(--color-accent)_25%,transparent)]`
  }
  return `${base} bg-[var(--color-bg-subtle)] text-[var(--color-text-muted)] ring-[var(--color-border)]`
})

const messageClass = computed(() => {
  if (tone.value === 'danger') {
    return 'mt-5 font-sans text-base font-medium tracking-tight text-[var(--color-accent)]'
  }
  return 'mt-5 font-sans text-base font-medium tracking-tight text-[var(--color-text)]'
})
</script>

<template>
  <div
    role="status"
    aria-live="polite"
    :class="[
      'grid place-items-center',
      compact ? 'py-8' : 'py-16',
    ]"
  >
    <div
      class="w-full max-w-md rounded-2xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] px-8 py-10 text-center shadow-[var(--shadow-soft)]"
    >
      <div :class="badgeClass" aria-hidden="true">
        <!-- loading: three bouncing dots -->
        <svg
          v-if="variant === 'loading'"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="currentColor"
        >
          <circle cx="5" cy="12" r="2">
            <animate
              attributeName="opacity"
              values="0.2;1;0.2"
              dur="1.2s"
              begin="0s"
              repeatCount="indefinite"
            />
          </circle>
          <circle cx="12" cy="12" r="2">
            <animate
              attributeName="opacity"
              values="0.2;1;0.2"
              dur="1.2s"
              begin="0.2s"
              repeatCount="indefinite"
            />
          </circle>
          <circle cx="19" cy="12" r="2">
            <animate
              attributeName="opacity"
              values="0.2;1;0.2"
              dur="1.2s"
              begin="0.4s"
              repeatCount="indefinite"
            />
          </circle>
        </svg>

        <!-- unauthorized: lock -->
        <svg
          v-else-if="variant === 'unauthorized'"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <rect x="4" y="10" width="16" height="11" rx="2" />
          <path d="M8 10V7a4 4 0 0 1 8 0v3" />
          <circle cx="12" cy="15.5" r="1.25" fill="currentColor" stroke="none" />
        </svg>

        <!-- failed: exclamation in circle -->
        <svg
          v-else-if="variant === 'failed'"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <circle cx="12" cy="12" r="9" />
          <path d="M12 7.5v5" />
          <circle cx="12" cy="16" r="0.6" fill="currentColor" stroke="none" />
        </svg>

        <!-- empty: open book -->
        <svg
          v-else-if="variant === 'empty'"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path d="M3 5.5a1.5 1.5 0 0 1 1.5-1.5H10a2 2 0 0 1 2 2v13a2 2 0 0 0-2-2H4.5A1.5 1.5 0 0 1 3 15.5Z" />
          <path d="M21 5.5a1.5 1.5 0 0 0-1.5-1.5H14a2 2 0 0 0-2 2v13a2 2 0 0 1 2-2h5.5a1.5 1.5 0 0 0 1.5-1.5Z" />
        </svg>

        <!-- noMatches: magnifying glass with slash -->
        <svg
          v-else-if="variant === 'noMatches'"
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <circle cx="11" cy="11" r="6" />
          <path d="m20 20-4-4" />
          <path d="m8.5 13.5 5-5" />
        </svg>

        <!-- notFound: question mark in circle -->
        <svg
          v-else
          class="h-5 w-5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <circle cx="12" cy="12" r="9" />
          <path d="M9.5 9.5a2.5 2.5 0 0 1 5 0c0 1.5-2.5 2-2.5 3.5" />
          <circle cx="12" cy="16.5" r="0.6" fill="currentColor" stroke="none" />
        </svg>
      </div>

      <p :class="messageClass">{{ message }}</p>
      <p
        v-if="hint"
        class="mt-2 font-sans text-sm text-[var(--color-text-muted)]"
      >{{ hint }}</p>

      <button
        v-if="actionLabel"
        type="button"
        class="mt-6 inline-flex cursor-pointer items-center gap-2 rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-4 py-2 font-sans text-sm font-medium transition-colors hover:bg-[var(--color-bg-subtle)]"
        @click="$emit('action')"
      >
        <svg
          class="h-3.5 w-3.5"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          <path d="M21 12a9 9 0 1 1-3-6.7" />
          <path d="M21 4v5h-5" />
        </svg>
        {{ actionLabel }}
      </button>
    </div>
  </div>
</template>
