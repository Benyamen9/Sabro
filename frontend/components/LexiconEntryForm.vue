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
  }>(),
  { entry: null, submitting: false },
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

// The three languages required to publish. The backoffice edits exactly these
// at launch; additional content languages can be added without a schema change.
const meaningLanguages = ['en', 'fr', 'nl'] as const

function meaningFor(language: string) {
  return props.entry?.meanings.find(m => m.language === language)?.text ?? ''
}

const syriacUnvocalized = ref(props.entry?.syriacUnvocalized ?? '')
const syriacVocalized = ref(props.entry?.syriacVocalized ?? '')
const sblTransliteration = ref(props.entry?.sblTransliteration ?? '')
const transliterationVariants = ref((props.entry?.transliterationVariants ?? []).join(', '))
const grammaticalCategory = ref<GrammaticalCategory>(props.entry?.grammaticalCategory ?? 'Noun')
const morphology = ref(props.entry?.morphology ?? '')
const meanings = reactive<Record<string, string>>({
  en: meaningFor('en'),
  fr: meaningFor('fr'),
  nl: meaningFor('nl'),
})

// Live preview of the playable length: count Unicode letters in the
// unvocalized form, excluding combining marks (vowel points, seyame). This
// mirrors the server-side computation; the authoritative value is returned
// on the saved entry.
const playableLength = computed(() =>
  [...syriacUnvocalized.value.normalize('NFC')].filter(ch => /\p{L}/u.test(ch)).length,
)
const lengthInRange = computed(() => playableLength.value >= 2 && playableLength.value <= 8)

const canSubmit = computed(() => syriacUnvocalized.value.trim().length > 0 && !props.submitting)

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
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none'
const labelClass = 'block font-sans text-sm font-medium text-[var(--color-text)]'
const hintClass = 'mt-1 font-sans text-xs text-[var(--color-text-faint)]'
</script>

<template>
  <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
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
          :class="[fieldClass, 'mt-1 text-right text-lg']"
          style="font-family: 'Noto Sans Syriac', serif;"
        >
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
          :class="[fieldClass, 'mt-1 text-right text-lg']"
          style="font-family: 'Noto Sans Syriac', serif;"
        >
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
        <input id="sbl" v-model="sblTransliteration" :class="[fieldClass, 'mt-1']">
        <p :class="hintClass">{{ t('admin.lexicon.form.sblTransliterationHint') }}</p>
      </div>
      <div>
        <label for="variants" :class="labelClass">{{ t('admin.lexicon.form.transliterationVariants') }}</label>
        <input id="variants" v-model="transliterationVariants" :class="[fieldClass, 'mt-1']">
        <p :class="hintClass">{{ t('admin.lexicon.form.transliterationVariantsHint') }}</p>
      </div>
    </div>

    <!-- Grammar -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="category" :class="labelClass">{{ t('admin.lexicon.form.grammaticalCategory') }}</label>
        <select id="category" v-model="grammaticalCategory" :class="[fieldClass, 'mt-1']">
          <option v-for="category in categories" :key="category" :value="category">
            {{ t(`admin.lexicon.category.${category}`) }}
          </option>
        </select>
      </div>
      <div>
        <label for="morphology" :class="labelClass">{{ t('admin.lexicon.form.morphology') }}</label>
        <input id="morphology" v-model="morphology" :class="[fieldClass, 'mt-1']">
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
        <input :id="`meaning-${language}`" v-model="meanings[language]" :class="fieldClass">
      </div>
    </fieldset>

    <!-- Actions -->
    <div class="flex items-center gap-3 border-t border-[var(--color-border)] pt-5">
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
