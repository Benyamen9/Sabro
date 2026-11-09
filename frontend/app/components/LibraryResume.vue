<script setup lang="ts">
import type { LibraryPlaceLabel } from '~/utils/libraryPlaces'

/**
 * "Where you left off" at the top of a book (Owner, 2026-11-01): the red ribbon and the place it
 * keeps, one tap away. A link when the place has its own page (a word), a button when the book opens
 * it in place (a figure card).
 */
const props = defineProps<{
  label: LibraryPlaceLabel
  to?: string
}>()
const emit = defineEmits<{ open: [] }>()

const { t } = useI18n()
const NuxtLink = resolveComponent('NuxtLink')
</script>

<template>
  <component
    :is="props.to ? NuxtLink : 'button'"
    :to="props.to"
    :type="props.to ? undefined : 'button'"
    class="relative mb-5 flex w-full items-center gap-3 overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] py-3 pl-3 pr-3.5 text-left no-underline transition-colors hover:border-[var(--color-border-strong)]"
    @click="props.to ? undefined : emit('open')"
  >
    <span aria-hidden="true" class="-mt-3 h-[34px] w-3.5 shrink-0 self-start bg-[#8c2f39] [clip-path:polygon(0_0,100%_0,100%_100%,50%_80%,0_100%)]" />
    <span class="min-w-0 flex-1">
      <span class="block font-sans text-xs text-[var(--color-text-muted)]">{{ t('library.shelf.leftOff') }}</span>
      <span class="block truncate font-serif text-base font-semibold text-[var(--color-text)]">
        <span v-if="props.label.syriac" dir="rtl" lang="syc" class="font-syriac text-[19px] font-normal">{{ props.label.syriac }}</span>
        <template v-if="props.label.syriac && props.label.title"> · </template>{{ props.label.title }}<template v-if="props.label.subtitle"> · <span class="font-normal text-[var(--color-text-muted)]">{{ props.label.subtitle }}</span></template>
      </span>
    </span>
    <span class="shrink-0 font-sans text-[13px] font-medium text-[var(--color-accent)]">{{ t('library.shelf.continue') }} →</span>
  </component>
</template>
