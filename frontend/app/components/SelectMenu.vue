<script setup lang="ts">
/**
 * A small accessible select dropdown: a compact trigger button that opens a
 * styled listbox menu, replacing the unstyleable native <select> popup so the
 * menu matches the editorial theme. The active option is tinted with the accent
 * and marked with a check.
 *
 * Keyboard: the trigger opens on Enter / Space / ArrowDown / ArrowUp; inside the
 * menu, arrows + Home/End move the highlight, Enter/Space selects, Escape closes
 * and returns focus to the trigger, Tab closes. Outside clicks close it too.
 *
 * Slots:
 *  - `trigger`  — the compact facade shown in the header (a glyph or code).
 *  - `option`   — the row content for one option ({ option }); the check is appended.
 *  - `footer`   — optional fine print under the options (e.g. a scope note).
 */
interface SelectOption {
  value: string
  label: string
}

const props = defineProps<{
  modelValue: string
  options: readonly SelectOption[]
  menuLabel: string
}>()

const emit = defineEmits<{ 'update:modelValue': [string] }>()

const open = ref(false)
const highlighted = ref(0)
const root = ref<HTMLElement | null>(null)
const trigger = ref<HTMLButtonElement | null>(null)
const list = ref<HTMLUListElement | null>(null)
const listId = useId()

const selectedIndex = computed(() => {
  const i = props.options.findIndex(o => o.value === props.modelValue)
  return i < 0 ? 0 : i
})

function openMenu() {
  highlighted.value = selectedIndex.value
  open.value = true
  nextTick(() => list.value?.focus())
}

function closeMenu(refocus = true) {
  if (!open.value) return
  open.value = false
  if (refocus) nextTick(() => trigger.value?.focus())
}

function toggle() {
  if (open.value) closeMenu()
  else openMenu()
}

function choose(index: number) {
  const option = props.options[index]
  if (option && option.value !== props.modelValue) emit('update:modelValue', option.value)
  closeMenu()
}

function onListKeydown(event: KeyboardEvent) {
  const count = props.options.length
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault()
      highlighted.value = (highlighted.value + 1) % count
      break
    case 'ArrowUp':
      event.preventDefault()
      highlighted.value = (highlighted.value - 1 + count) % count
      break
    case 'Home':
      event.preventDefault()
      highlighted.value = 0
      break
    case 'End':
      event.preventDefault()
      highlighted.value = count - 1
      break
    case 'Enter':
    case ' ':
      event.preventDefault()
      choose(highlighted.value)
      break
    case 'Escape':
      event.preventDefault()
      closeMenu()
      break
    case 'Tab':
      closeMenu(false)
      break
  }
}

function onFocusOut(event: FocusEvent) {
  if (!root.value?.contains(event.relatedTarget as Node | null)) closeMenu(false)
}

function onDocumentClick(event: MouseEvent) {
  if (open.value && root.value && !root.value.contains(event.target as Node)) closeMenu(false)
}

onMounted(() => document.addEventListener('click', onDocumentClick))
onBeforeUnmount(() => document.removeEventListener('click', onDocumentClick))
</script>

<template>
  <div ref="root" class="relative inline-flex" @focusout="onFocusOut">
    <button
      ref="trigger"
      type="button"
      :aria-label="menuLabel"
      :title="menuLabel"
      aria-haspopup="listbox"
      :aria-expanded="open"
      class="group flex h-8 items-center justify-center rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-2.5 leading-none text-[var(--color-text-muted)] transition-colors hover:text-[var(--color-text)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-accent-faint)]"
      :class="{ 'text-[var(--color-text)]': open }"
      @click.stop="toggle"
      @keydown.down.prevent="openMenu"
      @keydown.up.prevent="openMenu"
    >
      <slot name="trigger" />
    </button>

    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0 -translate-y-1"
      leave-active-class="transition duration-100 ease-in"
      leave-to-class="opacity-0 -translate-y-1"
    >
      <!-- The panel wraps the listbox so the optional footer can sit below the
           options without becoming an (invalid) listbox child. -->
      <div
        v-show="open"
        class="absolute right-0 top-full z-50 mt-1.5 min-w-[10.5rem] rounded-xl border border-[var(--color-border)] bg-[var(--color-bg-elevated)] p-1 shadow-[0_12px_32px_-12px_rgba(28,25,23,0.3),0_2px_6px_-2px_rgba(28,25,23,0.12)]"
      >
        <ul
          :id="listId"
          ref="list"
          role="listbox"
          tabindex="-1"
          :aria-label="menuLabel"
          :aria-activedescendant="`${listId}-${highlighted}`"
          class="focus:outline-none"
          @keydown="onListKeydown"
        >
          <li
            v-for="(option, index) in options"
            :id="`${listId}-${index}`"
            :key="option.value"
            role="option"
            :aria-selected="option.value === modelValue"
            class="flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm transition-colors"
            :class="option.value === modelValue
              ? 'bg-[var(--color-accent-faint)] font-medium text-[var(--color-accent)]'
              : index === highlighted ? 'bg-[var(--color-bg-subtle)] text-[var(--color-text)]' : 'text-[var(--color-text)]'"
            @click="choose(index)"
            @mousemove="highlighted = index"
          >
            <slot name="option" :option="option" />
            <svg
              class="ml-auto size-4 shrink-0 text-[var(--color-accent)]"
              :class="{ 'opacity-0': option.value !== modelValue }"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2.5"
              stroke-linecap="round"
              stroke-linejoin="round"
              aria-hidden="true"
            >
              <path d="M20 6 9 17l-5-5" />
            </svg>
          </li>
        </ul>
        <div
          v-if="$slots.footer"
          class="mt-1 max-w-[11.5rem] border-t border-[var(--color-border)] px-2 pb-1 pt-1.5 font-sans text-[0.7rem] leading-snug text-[var(--color-text-muted)]"
        >
          <slot name="footer" />
        </div>
      </div>
    </Transition>
  </div>
</template>
