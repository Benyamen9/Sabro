<script setup lang="ts">
import type {
  CreateLexiconEntryRequest,
  GrammaticalCategory,
  LexiconEntryDto,
} from '~/types/api'

const props = withDefaults(
  defineProps<{
    entry?: LexiconEntryDto | null
    submitting?: boolean
    submitLabel: string
    /**
     * An accepted proposal being applied: the named field opens holding the
     * proposed value instead of the stored one. Everything else is unchanged, and
     * nothing is saved until the editor submits — the proposal supplies the text,
     * the human still commits it.
     */
    prefill?: { field: string, value: string } | null
    /**
     * Read-only: the fields are shown but cannot be changed and there is nothing
     * to submit. A reviewer has to see the entry to have an opinion about it, and
     * proposes their correction through the panel below rather than saving here.
     */
    readonly?: boolean
    /**
     * Per-field validation errors from the server, keyed by camelCase property
     * path exactly as `ValidationProblemDetails` returns them:
     * `syriacUnvocalized`, `transliterationVariants[1]`, `meanings[0].text`.
     */
    fieldErrors?: Record<string, string[]> | null
  }>(),
  { entry: null, submitting: false, prefill: null, readonly: false, fieldErrors: null },
)

const emit = defineEmits<{
  (e: 'submit', payload: CreateLexiconEntryRequest): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const categories: GrammaticalCategory[] = [
  'Noun',
  'Verb',
  'Adjective',
  'Adverb',
  'Pronoun',
  'Preposition',
  'Conjunction',
  'Particle',
  'Numeral',
  'Interjection',
  'Other',
]

// The five languages required to publish. The backoffice edits exactly these;
// additional content languages can be added without a schema change.
const meaningLanguages = ['en', 'fr', 'nl', 'de', 'sv'] as const

function meaningFor(language: string) {
  return props.entry?.meanings.find(m => m.language === language)?.text ?? ''
}

/** The stored value, unless a proposal is being applied to this exact field. */
function initial(field: string, stored: string) {
  return props.prefill?.field === field ? props.prefill.value : stored
}

const syriacUnvocalized = ref(initial('syriacUnvocalized', props.entry?.syriacUnvocalized ?? ''))
const syriacVocalized = ref(initial('syriacVocalized', props.entry?.syriacVocalized ?? ''))
const sblTransliteration = ref(initial('sblTransliteration', props.entry?.sblTransliteration ?? ''))
const transliterationVariants = ref((props.entry?.transliterationVariants ?? []).join(', '))
const grammaticalCategory = ref<GrammaticalCategory>(
  initial('grammaticalCategory', props.entry?.grammaticalCategory ?? 'Noun') as GrammaticalCategory,
)
const morphology = ref(initial('morphology', props.entry?.morphology ?? ''))
const meanings = reactive<Record<string, string>>(
  Object.fromEntries(
    meaningLanguages.map(language => [language, initial(`meaning.${language}`, meaningFor(language))]),
  ) as Record<string, string>,
)

// Live preview of the playable length: count Unicode letters in the
// unvocalized form, excluding combining marks (vowel points, seyame). This
// mirrors the server-side computation; the authoritative value is returned
// on the saved entry.
const playableLength = computed(() =>
  [...syriacUnvocalized.value.normalize('NFC')].filter(ch => /\p{L}/u.test(ch)).length,
)
const lengthInRange = computed(() => playableLength.value >= 2 && playableLength.value <= 8)

const canSubmit = computed(() =>
  syriacUnvocalized.value.trim().length > 0 && !props.submitting && !props.readonly)

/**
 * The languages sent in the last submission, in order. The server names a bad
 * meaning by its position in the payload (`meanings[0].text`), and the payload
 * carries only the languages that had text — so without this, position 0 would
 * be blamed on English whatever was actually sent.
 */
const submittedMeaningLanguages = ref<string[]>([])

/** Server keys are property paths, so an indexed or nested key belongs to its root field. */
function matchesField(key: string, field: string) {
  return key === field || key.startsWith(`${field}[`) || key.startsWith(`${field}.`)
}

function errorsFor(field: string): string[] {
  return Object.entries(props.fieldErrors ?? {})
    .filter(([key]) => matchesField(key, field))
    .flatMap(([, messages]) => messages)
}

function meaningErrorsFor(language: string): string[] {
  const index = submittedMeaningLanguages.value.indexOf(language)
  if (index === -1) return []
  return Object.entries(props.fieldErrors ?? {})
    .filter(([key]) => key.startsWith(`meanings[${index}]`))
    .flatMap(([, messages]) => messages)
}

const scalarFields = [
  'syriacUnvocalized',
  'syriacVocalized',
  'sblTransliteration',
  'transliterationVariants',
  'grammaticalCategory',
  'morphology',
] as const

/**
 * Anything the server rejected that no field on this form owns — a rule on a
 * property the backoffice does not edit, or a name that has since changed.
 * Shown rather than dropped: an invisible error is how "check the highlighted
 * fields" ends up pointing at nothing.
 */
const unmappedErrors = computed(() =>
  Object.entries(props.fieldErrors ?? {})
    .filter(([key]) =>
      !scalarFields.some(field => matchesField(key, field))
      && !(key.startsWith('meanings[') && submittedMeaningLanguages.value.length > 0))
    .flatMap(([, messages]) => messages))

function toNullable(value: string): string | null {
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : null
}

function onSubmit() {
  if (!canSubmit.value) return

  const variants = transliterationVariants.value
    .split(',')
    .map(v => v.trim())
    .filter(Boolean)

  const meaningPayload = meaningLanguages
    .map(language => ({ language, text: (meanings[language] ?? '').trim() }))
    .filter(m => m.text.length > 0)

  submittedMeaningLanguages.value = meaningPayload.map(m => m.language)

  emit('submit', {
    syriacUnvocalized: syriacUnvocalized.value.normalize('NFC').trim(),
    syriacVocalized: toNullable(syriacVocalized.value.normalize('NFC')),
    sblTransliteration: toNullable(sblTransliteration.value),
    grammaticalCategory: grammaticalCategory.value,
    rootId: props.entry?.rootId ?? null,
    transliterationVariants: variants,
    morphology: toNullable(morphology.value),
    meanings: meaningPayload,
  })
}

const fieldClass
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none read-only:bg-[var(--color-bg-subtle)] read-only:text-[var(--color-text-muted)] disabled:bg-[var(--color-bg-subtle)] disabled:text-[var(--color-text-muted)]'
const labelClass = 'block font-sans text-sm font-medium text-[var(--color-text)]'
const hintClass = 'mt-1 font-sans text-xs text-[var(--color-text-faint)]'

// The highlight itself. The house accent rather than a red, because the palette
// has no danger colour and every other error surface here already uses the
// accent — inventing one belongs to a design pass, not to this fix. The accent
// is also the focus colour, so the field carries a tinted background too, and
// the state is doubled by `aria-invalid` and the message below it: it survives
// for a screen reader and for anyone who cannot separate the two borders.
const errorFieldClass
  = 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] focus:border-[var(--color-accent)]'
const errorTextClass = 'mt-1 font-sans text-xs font-medium text-[var(--color-accent)]'
</script>

<template>
  <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
    <!-- Rejected for a reason no field on this form owns. Never silently dropped. -->
    <p
      v-if="unmappedErrors.length"
      role="alert"
      class="rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
    >{{ unmappedErrors.join(' ') }}</p>

    <!-- Syriac forms -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="syriac-unvocalized" :class="labelClass">
          {{ t('admin.lexicon.form.syriacUnvocalized') }} <span class="text-[var(--color-accent)]">*</span>
        </label>
        <input
          id="syriac-unvocalized"
          v-model="syriacUnvocalized"
          dir="rtl"
          required
          :readonly="readonly"
          :aria-invalid="errorsFor('syriacUnvocalized').length > 0"
          :aria-describedby="errorsFor('syriacUnvocalized').length ? 'syriac-unvocalized-error' : undefined"
          :class="[fieldClass, 'mt-1 text-right text-lg', errorsFor('syriacUnvocalized').length ? errorFieldClass : '']"
          style="font-family: 'Noto Sans Syriac', serif;"
        >
        <p
          v-if="errorsFor('syriacUnvocalized').length"
          id="syriac-unvocalized-error"
          role="alert"
          :class="errorTextClass"
        >{{ errorsFor('syriacUnvocalized').join(' ') }}</p>
        <p :class="hintClass">{{ t('admin.lexicon.form.syriacUnvocalizedHint') }}</p>
      </div>
      <div>
        <label for="syriac-vocalized" :class="labelClass">
          {{ t('admin.lexicon.form.syriacVocalized') }}
        </label>
        <input
          id="syriac-vocalized"
          v-model="syriacVocalized"
          dir="rtl"
          :readonly="readonly"
          :aria-invalid="errorsFor('syriacVocalized').length > 0"
          :aria-describedby="errorsFor('syriacVocalized').length ? 'syriac-vocalized-error' : undefined"
          :class="[fieldClass, 'mt-1 text-right text-lg', errorsFor('syriacVocalized').length ? errorFieldClass : '']"
          style="font-family: 'Noto Sans Syriac', serif;"
        >
        <p
          v-if="errorsFor('syriacVocalized').length"
          id="syriac-vocalized-error"
          role="alert"
          :class="errorTextClass"
        >{{ errorsFor('syriacVocalized').join(' ') }}</p>
        <p :class="hintClass">{{ t('admin.lexicon.form.syriacVocalizedHint') }}</p>
      </div>
    </div>

    <!-- Playable length preview -->
    <div
      class="flex items-center gap-3 rounded-md border border-[var(--color-border)] bg-[var(--color-bg-subtle)] px-4 py-3"
    >
      <span class="font-sans text-sm font-medium text-[var(--color-text)]">
        {{ t('admin.lexicon.form.playableLength') }}:
      </span>
      <span
        class="inline-flex h-6 min-w-6 items-center justify-center rounded-full px-2 font-sans text-sm font-semibold"
        :class="
          lengthInRange
            ? 'bg-[var(--color-accent-faint)] text-[var(--color-accent)]'
            : 'bg-[var(--color-bg-elevated)] text-[var(--color-text-muted)] ring-1 ring-[var(--color-border-strong)]'
        "
      >{{ playableLength }}</span>
      <span class="font-sans text-xs text-[var(--color-text-faint)]">
        {{ lengthInRange ? t('admin.lexicon.form.playableLengthInRange') : t('admin.lexicon.form.playableLengthOutOfRange') }}
      </span>
    </div>

    <!-- Transliteration -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="sbl" :class="labelClass">{{ t('admin.lexicon.form.sblTransliteration') }}</label>
        <input
          id="sbl"
          v-model="sblTransliteration"
          :readonly="readonly"
          :aria-invalid="errorsFor('sblTransliteration').length > 0"
          :aria-describedby="errorsFor('sblTransliteration').length ? 'sbl-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('sblTransliteration').length ? errorFieldClass : '']"
        >
        <p
          v-if="errorsFor('sblTransliteration').length"
          id="sbl-error"
          role="alert"
          :class="errorTextClass"
        >{{ errorsFor('sblTransliteration').join(' ') }}</p>
        <p :class="hintClass">{{ t('admin.lexicon.form.sblTransliterationHint') }}</p>
      </div>
      <div>
        <label for="variants" :class="labelClass">{{ t('admin.lexicon.form.transliterationVariants') }}</label>
        <input
          id="variants"
          v-model="transliterationVariants"
          :readonly="readonly"
          :aria-invalid="errorsFor('transliterationVariants').length > 0"
          :aria-describedby="errorsFor('transliterationVariants').length ? 'variants-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('transliterationVariants').length ? errorFieldClass : '']"
        >
        <p
          v-if="errorsFor('transliterationVariants').length"
          id="variants-error"
          role="alert"
          :class="errorTextClass"
        >{{ errorsFor('transliterationVariants').join(' ') }}</p>
        <p :class="hintClass">{{ t('admin.lexicon.form.transliterationVariantsHint') }}</p>
      </div>
    </div>

    <!-- Grammar -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="category" :class="labelClass">{{ t('admin.lexicon.form.grammaticalCategory') }}</label>
        <!-- A select has no read-only state, so a reviewer's is disabled. -->
        <select
          id="category"
          v-model="grammaticalCategory"
          :disabled="readonly"
          :aria-invalid="errorsFor('grammaticalCategory').length > 0"
          :aria-describedby="errorsFor('grammaticalCategory').length ? 'category-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('grammaticalCategory').length ? errorFieldClass : '']"
        >
          <option v-for="category in categories" :key="category" :value="category">
            {{ t(`admin.lexicon.category.${category}`) }}
          </option>
        </select>
        <p
          v-if="errorsFor('grammaticalCategory').length"
          id="category-error"
          role="alert"
          :class="errorTextClass"
        >{{ errorsFor('grammaticalCategory').join(' ') }}</p>
      </div>
      <div>
        <label for="morphology" :class="labelClass">{{ t('admin.lexicon.form.morphology') }}</label>
        <input
          id="morphology"
          v-model="morphology"
          :readonly="readonly"
          :aria-invalid="errorsFor('morphology').length > 0"
          :aria-describedby="errorsFor('morphology').length ? 'morphology-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('morphology').length ? errorFieldClass : '']"
        >
        <p
          v-if="errorsFor('morphology').length"
          id="morphology-error"
          role="alert"
          :class="errorTextClass"
        >{{ errorsFor('morphology').join(' ') }}</p>
        <p :class="hintClass">{{ t('admin.lexicon.form.morphologyHint') }}</p>
      </div>
    </div>

    <!-- Meanings -->
    <fieldset class="flex flex-col gap-4">
      <legend :class="labelClass">{{ t('admin.lexicon.form.meanings') }}</legend>
      <p :class="[hintClass, 'mt-0']">{{ t('admin.lexicon.form.meaningsHint') }}</p>
      <div v-for="language in meaningLanguages" :key="language" class="flex flex-col gap-1">
        <label :for="`meaning-${language}`" class="font-sans text-xs font-medium uppercase tracking-wider text-[var(--color-text-muted)]">
          {{ t(`admin.lexicon.meaning.${language}`) }}
        </label>
        <input
          :id="`meaning-${language}`"
          v-model="meanings[language]"
          :readonly="readonly"
          :aria-invalid="meaningErrorsFor(language).length > 0"
          :aria-describedby="meaningErrorsFor(language).length ? `meaning-${language}-error` : undefined"
          :class="[fieldClass, meaningErrorsFor(language).length ? errorFieldClass : '']"
        >
        <p
          v-if="meaningErrorsFor(language).length"
          :id="`meaning-${language}-error`"
          role="alert"
          :class="errorTextClass"
        >{{ meaningErrorsFor(language).join(' ') }}</p>
      </div>
    </fieldset>

    <!-- Actions. A reviewer has nothing to submit here; their surface is the
         propose panel below. -->
    <div v-if="!readonly" class="flex items-center gap-3 border-t border-[var(--color-border)] pt-5">
      <button
        type="submit"
        :disabled="!canSubmit"
        class="inline-flex items-center gap-2 rounded-md bg-[var(--color-accent)] px-5 py-2.5 font-sans text-sm font-medium text-white no-underline shadow-[var(--shadow-soft)] transition-colors hover:bg-[var(--color-accent-hover)] disabled:cursor-not-allowed disabled:opacity-50"
      >
        {{ submitting ? t('common.saving') : submitLabel }}
      </button>
      <button
        type="button"
        class="inline-flex items-center rounded-md border border-[var(--color-border-strong)] px-4 py-2 font-sans text-sm font-medium text-[var(--color-text-muted)] transition-colors hover:text-[var(--color-text)]"
        @click="emit('cancel')"
      >
        {{ t('common.cancel') }}
      </button>
    </div>
  </form>
</template>
