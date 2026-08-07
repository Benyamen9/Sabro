<script setup lang="ts">
import type { BethGazoModeDto, ChantDto, CreateChantRequest } from '~/types/api'

const props = withDefaults(
  defineProps<{
    chant?: ChantDto | null
    /**
     * The modes, as the API serves them. A prop rather than a fetch inside the
     * form: the set grows as the owner works through the tradition — some sets
     * run past eight — so it can never be a constant, and the page that owns the
     * data owns the request.
     */
    modes: BethGazoModeDto[]
    /**
     * Every chant that could be the melody this one borrows. The page passes the
     * whole treasury; the form drops this chant itself, which the domain refuses
     * anyway.
     */
    melodySources?: ChantDto[]
    submitting?: boolean
    submitLabel: string
    /**
     * Read-only: the fields are shown but cannot be changed and there is nothing
     * to submit. A Nahlo reviewer has to see the chant to have an opinion about
     * it; chants carry no proposal workflow yet, so a reviewer reads and no more.
     */
    readonly?: boolean
    /**
     * Per-field validation errors from the server, keyed by camelCase property
     * name exactly as `ValidationProblemDetails` returns them: `syriacIncipit`,
     * `transliteration`, `modeId`.
     */
    fieldErrors?: Record<string, string[]> | null
  }>(),
  { chant: null, melodySources: () => [], submitting: false, readonly: false, fieldErrors: null },
)

const emit = defineEmits<{
  (e: 'submit', payload: CreateChantRequest): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const syriacIncipit = ref(props.chant?.syriacIncipit ?? '')
const syriacIncipitVocalized = ref(props.chant?.syriacIncipitVocalized ?? '')
const transliteration = ref(props.chant?.transliteration ?? '')
const shuhlofo = ref(props.chant?.shuhlofo ?? '')

// Empty string is "none": no mode chosen yet on a new chant, and no borrowed
// melody at all on one that carries its own.
const modeId = ref(props.chant?.modeId ?? '')
const inheritsMelodyFromId = ref(props.chant?.inheritsMelodyFromId ?? '')

// Matches Chant.MaxTransliterationLength / MaxShuhlofoLength on the backend, so
// the box stops before the server has to.
const maxTransliterationLength = 256
const maxShuhlofoLength = 128

/**
 * A chant cannot borrow its own melody — the domain refuses it, and offering it
 * would be offering a save that fails.
 */
const melodyOptions = computed(() =>
  props.melodySources.filter(candidate => candidate.id !== props.chant?.id))

const canSubmit = computed(() =>
  syriacIncipit.value.trim().length > 0
  && transliteration.value.trim().length > 0
  && modeId.value !== ''
  && !props.submitting
  && !props.readonly)

/** Server keys are property paths, so an indexed or nested key belongs to its root field. */
function matchesField(key: string, field: string) {
  return key === field || key.startsWith(`${field}[`) || key.startsWith(`${field}.`)
}

function errorsFor(field: string): string[] {
  return Object.entries(props.fieldErrors ?? {})
    .filter(([key]) => matchesField(key, field))
    .flatMap(([, messages]) => messages)
}

const formFields = [
  'syriacIncipit',
  'syriacIncipitVocalized',
  'transliteration',
  'modeId',
  'shuhlofo',
  'inheritsMelodyFromId',
] as const

/**
 * Anything the server rejected that no field on this form owns. Shown rather
 * than dropped: an invisible error is an error the editor cannot act on.
 */
const unmappedErrors = computed(() =>
  Object.entries(props.fieldErrors ?? {})
    .filter(([key]) => !formFields.some(field => matchesField(key, field)))
    .flatMap(([, messages]) => messages))

function onSubmit() {
  if (!canSubmit.value) return

  emit('submit', {
    syriacIncipit: syriacIncipit.value.trim(),
    // Blank boxes are sent as null rather than as empty strings: the optional
    // fields mean "this chant has none", which is not the same as "empty text".
    syriacIncipitVocalized: syriacIncipitVocalized.value.trim() || null,
    transliteration: transliteration.value.trim(),
    modeId: modeId.value,
    shuhlofo: shuhlofo.value.trim() || null,
    inheritsMelodyFromId: inheritsMelodyFromId.value || null,
  })
}

const fieldClass
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none read-only:bg-[var(--color-bg-subtle)] read-only:text-[var(--color-text-muted)] disabled:bg-[var(--color-bg-subtle)] disabled:text-[var(--color-text-muted)]'
const syriacFieldClass = 'text-right text-lg'
const labelClass = 'block font-sans text-sm font-medium text-[var(--color-text)]'
const hintClass = 'mt-1 font-sans text-xs text-[var(--color-text-faint)]'
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

    <!-- The Syriac incipit: the opening words, which are what identify a chant
         in the tradition. -->
    <div>
      <label for="chant-syriac" :class="labelClass">
        {{ t('admin.chants.form.syriacIncipit') }} <span class="text-[var(--color-accent)]">*</span>
      </label>
      <input
        id="chant-syriac"
        v-model="syriacIncipit"
        dir="rtl"
        lang="syc"
        required
        maxlength="512"
        :readonly="readonly"
        :aria-invalid="errorsFor('syriacIncipit').length > 0"
        :aria-describedby="errorsFor('syriacIncipit').length ? 'chant-syriac-error' : undefined"
        :class="[fieldClass, syriacFieldClass, 'mt-1', errorsFor('syriacIncipit').length ? errorFieldClass : '']"
      >
      <p v-if="errorsFor('syriacIncipit').length" id="chant-syriac-error" role="alert" :class="errorTextClass">
        {{ errorsFor('syriacIncipit').join(' ') }}
      </p>
      <p :class="hintClass">{{ t('admin.chants.form.syriacIncipitHint') }}</p>
    </div>

    <div>
      <label for="chant-syriac-vocalized" :class="labelClass">
        {{ t('admin.chants.form.syriacIncipitVocalized') }}
      </label>
      <input
        id="chant-syriac-vocalized"
        v-model="syriacIncipitVocalized"
        dir="rtl"
        lang="syc"
        maxlength="512"
        :readonly="readonly"
        :aria-invalid="errorsFor('syriacIncipitVocalized').length > 0"
        :aria-describedby="errorsFor('syriacIncipitVocalized').length ? 'chant-syriac-vocalized-error' : undefined"
        :class="[fieldClass, syriacFieldClass, 'mt-1', errorsFor('syriacIncipitVocalized').length ? errorFieldClass : '']"
      >
      <p
        v-if="errorsFor('syriacIncipitVocalized').length"
        id="chant-syriac-vocalized-error"
        role="alert"
        :class="errorTextClass"
      >{{ errorsFor('syriacIncipitVocalized').join(' ') }}</p>
      <p :class="hintClass">{{ t('admin.chants.form.syriacIncipitVocalizedHint') }}</p>
    </div>

    <!-- The three parts of the answer: melody name, mode, and the shuḥlofo where
         there is one. -->
    <div>
      <label for="chant-transliteration" :class="labelClass">
        {{ t('admin.chants.form.transliteration') }} <span class="text-[var(--color-accent)]">*</span>
      </label>
      <input
        id="chant-transliteration"
        v-model="transliteration"
        required
        :maxlength="maxTransliterationLength"
        :readonly="readonly"
        :aria-invalid="errorsFor('transliteration').length > 0"
        :aria-describedby="errorsFor('transliteration').length ? 'chant-transliteration-error' : undefined"
        :class="[fieldClass, 'mt-1 text-lg', errorsFor('transliteration').length ? errorFieldClass : '']"
      >
      <p
        v-if="errorsFor('transliteration').length"
        id="chant-transliteration-error"
        role="alert"
        :class="errorTextClass"
      >{{ errorsFor('transliteration').join(' ') }}</p>
      <p :class="hintClass">{{ t('admin.chants.form.transliterationHint') }}</p>
    </div>

    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="chant-mode" :class="labelClass">
          {{ t('admin.chants.form.mode') }} <span class="text-[var(--color-accent)]">*</span>
        </label>
        <!-- A select has no read-only state, so a reviewer's is disabled. -->
        <select
          id="chant-mode"
          v-model="modeId"
          required
          :disabled="readonly"
          :aria-invalid="errorsFor('modeId').length > 0"
          :aria-describedby="errorsFor('modeId').length ? 'chant-mode-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('modeId').length ? errorFieldClass : '']"
        >
          <option value="">{{ t('admin.chants.form.modeUnset') }}</option>
          <option v-for="mode in modes" :key="mode.id" :value="mode.id">
            {{ mode.position }} · {{ mode.name }}
          </option>
        </select>
        <p v-if="errorsFor('modeId').length" id="chant-mode-error" role="alert" :class="errorTextClass">
          {{ errorsFor('modeId').join(' ') }}
        </p>
        <p :class="hintClass">{{ t('admin.chants.form.modeHint') }}</p>
      </div>

      <div>
        <label for="chant-shuhlofo" :class="labelClass">{{ t('admin.chants.form.shuhlofo') }}</label>
        <input
          id="chant-shuhlofo"
          v-model="shuhlofo"
          :maxlength="maxShuhlofoLength"
          :readonly="readonly"
          :aria-invalid="errorsFor('shuhlofo').length > 0"
          :aria-describedby="errorsFor('shuhlofo').length ? 'chant-shuhlofo-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('shuhlofo').length ? errorFieldClass : '']"
        >
        <p v-if="errorsFor('shuhlofo').length" id="chant-shuhlofo-error" role="alert" :class="errorTextClass">
          {{ errorsFor('shuhlofo').join(' ') }}
        </p>
        <p :class="hintClass">{{ t('admin.chants.form.shuhlofoHint') }}</p>
      </div>
    </div>

    <!-- Solqin: a chant that borrows another's melody. A relationship, not a
         fourth thing the player names. -->
    <div>
      <label for="chant-parent" :class="labelClass">{{ t('admin.chants.form.inheritsMelodyFrom') }}</label>
      <select
        id="chant-parent"
        v-model="inheritsMelodyFromId"
        :disabled="readonly"
        :aria-invalid="errorsFor('inheritsMelodyFromId').length > 0"
        :class="[fieldClass, 'mt-1', errorsFor('inheritsMelodyFromId').length ? errorFieldClass : '']"
      >
        <option value="">{{ t('admin.chants.form.inheritsMelodyFromNone') }}</option>
        <option v-for="candidate in melodyOptions" :key="candidate.id" :value="candidate.id">
          {{ candidate.transliteration }} · {{ candidate.modeName }}{{ candidate.shuhlofo ? ` · ${candidate.shuhlofo}` : '' }}
        </option>
      </select>
      <p v-if="errorsFor('inheritsMelodyFromId').length" role="alert" :class="errorTextClass">
        {{ errorsFor('inheritsMelodyFromId').join(' ') }}
      </p>
      <p :class="hintClass">{{ t('admin.chants.form.inheritsMelodyFromHint') }}</p>
    </div>

    <!-- Actions. A reviewer has nothing to submit here. -->
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
