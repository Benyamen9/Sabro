<script setup lang="ts">
/**
 * A small set of mutually exclusive choices, all visible at once.
 *
 * Replaces a `<select>` for the access levels. A dropdown hides the options
 * until you open it, so granting access meant opening a menu to find out what
 * you were not granting — on the one screen where the choice is consequential
 * and rare. Three buttons show the whole shape of the decision.
 *
 * Radio semantics rather than buttons with `aria-pressed`: this is one choice
 * among several, not several independent toggles, and screen readers announce
 * "2 of 3" for free.
 */
const props = defineProps<{
  modelValue: string
  options: { value: string, label: string }[]
  /** Accessible name — the control sits next to a visual label, not a real one. */
  label: string
  /** CSS custom property for the selected tint, e.g. '--color-meltho'. */
  accent?: string
  disabled?: boolean
  name: string
}>()

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>()

function pick(value: string) {
  if (props.disabled || value === props.modelValue) return
  emit('update:modelValue', value)
}
</script>

<template>
  <div
    role="radiogroup"
    :aria-label="label"
    class="inline-flex rounded-lg border border-[var(--color-border-strong)] bg-[var(--color-bg-subtle)] p-0.5"
    :class="disabled ? 'opacity-60' : ''"
  >
    <label
      v-for="option in options"
      :key="option.value"
      class="cursor-pointer rounded-md px-2.5 py-1 font-sans text-xs transition-colors sm:text-sm"
      :class="option.value === modelValue
        ? 'bg-[var(--color-bg-elevated)] font-semibold shadow-[var(--shadow-soft)]'
        : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)]'"
      :style="option.value === modelValue && accent ? { color: `var(${accent})` } : undefined"
    >
      <input
        type="radio"
        :name="name"
        :value="option.value"
        :checked="option.value === modelValue"
        :disabled="disabled"
        class="sr-only"
        @change="pick(option.value)"
      >
      {{ option.label }}
    </label>
  </div>
</template>
