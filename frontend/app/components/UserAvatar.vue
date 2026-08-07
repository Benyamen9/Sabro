<script setup lang="ts">
// Renders the signed-in user's avatar: their Logto profile picture when present,
// otherwise an accent tile with the first letter of their name. Shared by the
// header menu (small) and the profile page (large) so they always match.
withDefaults(
  defineProps<{
    initial: string
    src?: string
    label?: string
    size?: 'sm' | 'lg'
  }>(),
  { src: '', label: '', size: 'sm' },
)
</script>

<template>
  <span
    :class="[
      'inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full font-sans font-semibold uppercase leading-none',
      size === 'lg' ? 'size-14 text-xl' : 'size-8 text-sm',
    ]"
    :aria-label="label || undefined"
  >
    <img
      v-if="src"
      :src="src"
      :alt="label"
      class="size-full object-cover"
    >
    <span
      v-else
      aria-hidden="true"
      class="flex size-full items-center justify-center bg-[var(--color-accent)] text-white"
    >{{ initial }}</span>
  </span>
</template>
