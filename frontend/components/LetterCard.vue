<script setup lang="ts">
import type { SyriacLetter } from '~/types/api'

const props = defineProps<{ letter: SyriacLetter }>()
const { t } = useI18n()

const letterName = computed(() => t(`library.letters.${props.letter.code}`))
const vowelLabel = computed(() =>
  props.letter.vowel ? t(`library.vowels.${props.letter.vowel}`) : null,
)
const hardeningLabel = computed(() =>
  props.letter.hardening ? t(`library.hardening.${props.letter.hardening}`) : null,
)
const isSoft = computed(() => props.letter.hardening === 'Rukkokho')

// Short rule label: authoritative when marked in the vocalization, otherwise the
// heuristic that produced the first-pass guess.
const ruleLabel = computed(() => {
  if (!props.letter.hardening) return null
  if (props.letter.hardeningSource === 'Marked') return t('library.rule.marked')
  return isSoft.value ? t('library.rule.computedSoft') : t('library.rule.computedHard')
})
</script>

<template>
  <div
    class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-4 text-center shadow-[var(--shadow-soft)]"
  >
    <SyriacText :text="letter.letter" class="!text-4xl text-[var(--color-accent)]" />
    <p class="mt-2 font-sans text-sm font-semibold">{{ letterName }}</p>
    <p v-if="vowelLabel" class="mt-0.5 font-sans text-xs text-[var(--color-text-muted)]">
      {{ vowelLabel }}
    </p>

    <div v-if="hardeningLabel" class="mt-3 border-t border-[var(--color-border)] pt-3">
      <span
        class="inline-block rounded-full px-2.5 py-0.5 font-sans text-xs font-medium"
        :class="
          isSoft
            ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
            : 'bg-[var(--color-bg-subtle)] text-[var(--color-text)]'
        "
      >{{ hardeningLabel }}</span>
      <p class="mt-1.5 font-sans text-[0.7rem] leading-snug text-[var(--color-text-muted)]">
        {{ ruleLabel }}
      </p>
    </div>
  </div>
</template>
