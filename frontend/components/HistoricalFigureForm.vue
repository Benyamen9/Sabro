<script setup lang="ts">
import type {
  CreateHistoricalFigureRequest,
  HistoricalFigureCategory,
  HistoricalFigureDto,
  HistoricalFigureGender,
  HistoricalFigureRegion,
  HistoricalFigureRole,
  HistoricalFigureTradition,
  HistoricalPeriod,
} from '~/types/api'
import {
  HISTORICAL_FIGURE_CATEGORIES,
  HISTORICAL_FIGURE_GENDERS,
  HISTORICAL_FIGURE_REGIONS,
  HISTORICAL_FIGURE_ROLES,
  HISTORICAL_FIGURE_TRADITIONS,
  HISTORICAL_PERIODS,
  MAX_ERA,
  MIN_ERA,
  formatEra,
} from '~/utils/historicalFigures'

const props = withDefaults(
  defineProps<{
    figure?: HistoricalFigureDto | null
    submitting?: boolean
    submitLabel: string
    /**
     * An accepted proposal being applied: the named field opens holding the
     * proposed value instead of the stored one. Nothing is saved until the editor
     * submits — the proposal supplies the text, the human still commits it.
     */
    prefill?: { field: string, value: string } | null
    /**
     * Read-only: the fields are shown but cannot be changed and there is nothing
     * to submit. A reviewer has to see the figure to have an opinion about it, and
     * proposes their correction through the panel below rather than saving here.
     */
    readonly?: boolean
    /**
     * Per-field validation errors from the server, keyed by camelCase property
     * path exactly as `ValidationProblemDetails` returns them: `name`, `era`,
     * `descriptions[0].text`.
     */
    fieldErrors?: Record<string, string[]> | null
  }>(),
  { figure: null, submitting: false, prefill: null, readonly: false, fieldErrors: null },
)

const emit = defineEmits<{
  (e: 'submit', payload: CreateHistoricalFigureRequest): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

/** The stored value, unless a proposal is being applied to this exact field. */
function initial(field: string, stored: string) {
  return props.prefill?.field === field ? props.prefill.value : stored
}

const name = ref(initial('name', props.figure?.name ?? ''))
const category = ref<HistoricalFigureCategory>(
  initial('category', props.figure?.category ?? 'Patristic') as HistoricalFigureCategory,
)
const period = ref<HistoricalPeriod>(
  initial('period', props.figure?.period ?? 'PostChalcedonian') as HistoricalPeriod,
)
const role = ref<HistoricalFigureRole>(
  initial('role', props.figure?.role ?? 'Other') as HistoricalFigureRole,
)
const region = ref<HistoricalFigureRegion>(
  initial('region', props.figure?.region ?? 'Mesopotamia') as HistoricalFigureRegion,
)
const gender = ref<HistoricalFigureGender>(
  initial('gender', props.figure?.gender ?? 'Male') as HistoricalFigureGender,
)
// Empty string means "not yet decided" — a draft may omit it, publishing may not.
const tradition = ref<HistoricalFigureTradition | ''>(
  initial('tradition', props.figure?.tradition ?? '') as HistoricalFigureTradition | '',
)

// Era is entered as a signed century, so the field is a number input rather
// than a year. Kept as a string ref because an empty input is not 0.
const era = ref(initial('era', props.figure?.era != null ? String(props.figure.era) : ''))

// The five languages the ecosystem serves. Descriptions are optional enrichment
// and never gate publication — the roster was published long before this field
// existed, so requiring them would invalidate all 289 figures at once.
const descriptionLanguages = ['en', 'fr', 'nl', 'de', 'sv'] as const

function descriptionFor(language: string) {
  return props.figure?.descriptions?.find(d => d.language === language)?.text ?? ''
}

const descriptions = reactive<Record<string, string>>(
  Object.fromEntries(
    descriptionLanguages.map(language => [
      language,
      initial(`description.${language}`, descriptionFor(language)),
    ]),
  ) as Record<string, string>,
)

// Longest description still in the box, so the counter warns before the server
// does. Matches HistoricalFigureDescription.MaxTextLength on the backend.
const descriptionMaxLength = 500

const eraNumber = computed(() => {
  const parsed = Number.parseInt(era.value, 10)
  return Number.isFinite(parsed) ? parsed : null
})

const eraValid = computed(() => {
  const value = eraNumber.value
  return value !== null && value !== 0 && value >= MIN_ERA && value <= MAX_ERA
})

const eraPreview = computed(() => (eraValid.value ? formatEra(eraNumber.value!, t) : null))

const canSubmit = computed(() =>
  name.value.trim().length > 0 && eraValid.value && !props.submitting && !props.readonly)

/**
 * The languages sent in the last submission, in order. The server names a bad
 * description by its position in the payload (`descriptions[0].text`), and the
 * payload carries only the languages that had text — so without this, position 0
 * would be blamed on English whatever was actually sent.
 */
const submittedDescriptionLanguages = ref<string[]>([])

/** Server keys are property paths, so an indexed or nested key belongs to its root field. */
function matchesField(key: string, field: string) {
  return key === field || key.startsWith(`${field}[`) || key.startsWith(`${field}.`)
}

function errorsFor(field: string): string[] {
  return Object.entries(props.fieldErrors ?? {})
    .filter(([key]) => matchesField(key, field))
    .flatMap(([, messages]) => messages)
}

function descriptionErrorsFor(language: string): string[] {
  const index = submittedDescriptionLanguages.value.indexOf(language)
  if (index === -1) return []
  return Object.entries(props.fieldErrors ?? {})
    .filter(([key]) => key.startsWith(`descriptions[${index}]`))
    .flatMap(([, messages]) => messages)
}

const scalarFields = [
  'name',
  'category',
  'era',
  'period',
  'role',
  'region',
  'tradition',
  'gender',
] as const

/**
 * Anything the server rejected that no field on this form owns. Shown rather
 * than dropped: an invisible error is an error the editor cannot act on.
 */
const unmappedErrors = computed(() =>
  Object.entries(props.fieldErrors ?? {})
    .filter(([key]) =>
      !scalarFields.some(field => matchesField(key, field))
      && !(key.startsWith('descriptions[') && submittedDescriptionLanguages.value.length > 0))
    .flatMap(([, messages]) => messages))

const errorFieldClass
  = 'border-[var(--color-accent)] bg-[var(--color-accent-faint)] focus:border-[var(--color-accent)]'
const errorTextClass = 'mt-1 font-sans text-xs font-medium text-[var(--color-accent)]'

function onSubmit() {
  if (!canSubmit.value) return

  const descriptionPayload = descriptionLanguages
    .map(language => ({ language, text: (descriptions[language] ?? '').trim() }))
    .filter(d => d.text.length > 0)

  submittedDescriptionLanguages.value = descriptionPayload.map(d => d.language)

  emit('submit', {
    name: name.value.trim(),
    category: category.value,
    era: eraNumber.value!,
    period: period.value,
    role: role.value,
    region: region.value,
    gender: gender.value,
    tradition: tradition.value === '' ? null : tradition.value,
    // Empty boxes are omitted rather than sent as blank strings: the domain
    // rejects an empty description, and a language left alone simply has none.
    descriptions: descriptionPayload,
  })
}

const fieldClass
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none read-only:bg-[var(--color-bg-subtle)] read-only:text-[var(--color-text-muted)] disabled:bg-[var(--color-bg-subtle)] disabled:text-[var(--color-text-muted)]'
const labelClass = 'block font-sans text-sm font-medium text-[var(--color-text)]'
const hintClass = 'mt-1 font-sans text-xs text-[var(--color-text-faint)]'
</script>

<template>
  <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
    <!-- Rejected for a reason no field on this form owns. Never silently dropped. -->
    <p
      v-if="unmappedErrors.length"
      role="alert"
      class="rounded-md border border-[color-mix(in_oklab,var(--color-accent)_30%,transparent)] bg-[var(--color-accent-faint)] px-4 py-3 font-sans text-sm text-[var(--color-accent)]"
    >{{ unmappedErrors.join(' ') }}</p>

    <!-- Name — the answer of a Shmo round. -->
    <div>
      <label for="figure-name" :class="labelClass">
        {{ t('admin.historicalFigures.form.name') }} <span class="text-[var(--color-accent)]">*</span>
      </label>
      <input
        id="figure-name"
        v-model="name"
        required
        maxlength="256"
        :readonly="readonly"
        :aria-invalid="errorsFor('name').length > 0"
        :aria-describedby="errorsFor('name').length ? 'figure-name-error' : undefined"
        :class="[fieldClass, 'mt-1 text-lg', errorsFor('name').length ? errorFieldClass : '']"
      >
      <p v-if="errorsFor('name').length" id="figure-name-error" role="alert" :class="errorTextClass">
        {{ errorsFor('name').join(' ') }}
      </p>
      <p :class="hintClass">{{ t('admin.historicalFigures.form.nameHint') }}</p>
    </div>

    <!-- Category + era -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="figure-category" :class="labelClass">{{ t('admin.historicalFigures.form.category') }}</label>
        <!-- A select has no read-only state, so a reviewer's is disabled. -->
        <select
          id="figure-category"
          v-model="category"
          :disabled="readonly"
          :aria-invalid="errorsFor('category').length > 0"
          :class="[fieldClass, 'mt-1', errorsFor('category').length ? errorFieldClass : '']"
        >
          <option v-for="value in HISTORICAL_FIGURE_CATEGORIES" :key="value" :value="value">
            {{ t(`admin.historicalFigures.category.${value}`) }}
          </option>
        </select>
        <p v-if="errorsFor('category').length" role="alert" :class="errorTextClass">
          {{ errorsFor('category').join(' ') }}
        </p>
      </div>
      <div>
        <label for="figure-era" :class="labelClass">
          {{ t('admin.historicalFigures.form.era') }} <span class="text-[var(--color-accent)]">*</span>
        </label>
        <input
          id="figure-era"
          v-model="era"
          type="number"
          inputmode="numeric"
          required
          :min="MIN_ERA"
          :max="MAX_ERA"
          :readonly="readonly"
          :aria-invalid="errorsFor('era').length > 0"
          :aria-describedby="errorsFor('era').length ? 'figure-era-error' : undefined"
          :class="[fieldClass, 'mt-1', errorsFor('era').length ? errorFieldClass : '']"
        >
        <p v-if="errorsFor('era').length" id="figure-era-error" role="alert" :class="errorTextClass">
          {{ errorsFor('era').join(' ') }}
        </p>
        <p :class="hintClass">
          <span v-if="eraPreview" class="text-[var(--color-accent)]">{{ eraPreview }}</span>
          <span v-else>{{ t('admin.historicalFigures.form.eraHint') }}</span>
        </p>
      </div>
    </div>

    <!-- Classification -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="figure-period" :class="labelClass">{{ t('admin.historicalFigures.form.period') }}</label>
        <select
          id="figure-period"
          v-model="period"
          :disabled="readonly"
          :aria-invalid="errorsFor('period').length > 0"
          :class="[fieldClass, 'mt-1', errorsFor('period').length ? errorFieldClass : '']"
        >
          <option v-for="value in HISTORICAL_PERIODS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.period.${value}`) }}
          </option>
        </select>
        <p v-if="errorsFor('period').length" role="alert" :class="errorTextClass">
          {{ errorsFor('period').join(' ') }}
        </p>
        <p :class="hintClass">{{ t('admin.historicalFigures.form.periodHint') }}</p>
      </div>
      <div>
        <label for="figure-role" :class="labelClass">{{ t('admin.historicalFigures.form.role') }}</label>
        <select
          id="figure-role"
          v-model="role"
          :disabled="readonly"
          :aria-invalid="errorsFor('role').length > 0"
          :class="[fieldClass, 'mt-1', errorsFor('role').length ? errorFieldClass : '']"
        >
          <option v-for="value in HISTORICAL_FIGURE_ROLES" :key="value" :value="value">
            {{ t(`admin.historicalFigures.role.${value}`) }}
          </option>
        </select>
        <p v-if="errorsFor('role').length" role="alert" :class="errorTextClass">
          {{ errorsFor('role').join(' ') }}
        </p>
        <p :class="hintClass">{{ t('admin.historicalFigures.form.roleHint') }}</p>
      </div>
      <div>
        <label for="figure-region" :class="labelClass">{{ t('admin.historicalFigures.form.region') }}</label>
        <select
          id="figure-region"
          v-model="region"
          :disabled="readonly"
          :aria-invalid="errorsFor('region').length > 0"
          :class="[fieldClass, 'mt-1', errorsFor('region').length ? errorFieldClass : '']"
        >
          <option v-for="value in HISTORICAL_FIGURE_REGIONS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.region.${value}`) }}
          </option>
        </select>
        <p v-if="errorsFor('region').length" role="alert" :class="errorTextClass">
          {{ errorsFor('region').join(' ') }}
        </p>
      </div>
    </div>

    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="figure-tradition" :class="labelClass">{{ t('admin.historicalFigures.form.tradition') }}</label>
        <select
          id="figure-tradition"
          v-model="tradition"
          :disabled="readonly"
          :aria-invalid="errorsFor('tradition').length > 0"
          :class="[fieldClass, 'mt-1', errorsFor('tradition').length ? errorFieldClass : '']"
        >
          <option value="">{{ t('admin.historicalFigures.form.traditionUnset') }}</option>
          <option v-for="value in HISTORICAL_FIGURE_TRADITIONS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.tradition.${value}`) }}
          </option>
        </select>
        <p v-if="errorsFor('tradition').length" role="alert" :class="errorTextClass">
          {{ errorsFor('tradition').join(' ') }}
        </p>
        <p :class="hintClass">{{ t('admin.historicalFigures.form.traditionHint') }}</p>
      </div>
      <div>
        <label for="figure-gender" :class="labelClass">{{ t('admin.historicalFigures.form.gender') }}</label>
        <select
          id="figure-gender"
          v-model="gender"
          :disabled="readonly"
          :aria-invalid="errorsFor('gender').length > 0"
          :class="[fieldClass, 'mt-1', errorsFor('gender').length ? errorFieldClass : '']"
        >
          <option v-for="value in HISTORICAL_FIGURE_GENDERS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.gender.${value}`) }}
          </option>
        </select>
        <p v-if="errorsFor('gender').length" role="alert" :class="errorTextClass">
          {{ errorsFor('gender').join(' ') }}
        </p>
      </div>
    </div>

    <!-- Descriptions: shown when a Shmo round is revealed, never during it. -->
    <fieldset class="flex flex-col gap-4">
      <legend :class="labelClass">{{ t('admin.historicalFigures.form.descriptions') }}</legend>
      <p :class="[hintClass, 'mt-0']">{{ t('admin.historicalFigures.form.descriptionsHint') }}</p>
      <div v-for="language in descriptionLanguages" :key="language" class="flex flex-col gap-1">
        <label
          :for="`description-${language}`"
          class="font-sans text-xs font-medium uppercase tracking-wider text-[var(--color-text-muted)]"
        >
          {{ t(`admin.historicalFigures.description.${language}`) }}
        </label>
        <textarea
          :id="`description-${language}`"
          v-model="descriptions[language]"
          rows="2"
          :maxlength="descriptionMaxLength"
          :readonly="readonly"
          :aria-invalid="descriptionErrorsFor(language).length > 0"
          :aria-describedby="descriptionErrorsFor(language).length ? `description-${language}-error` : undefined"
          :class="[fieldClass, 'resize-y', descriptionErrorsFor(language).length ? errorFieldClass : '']"
        />
        <p
          v-if="descriptionErrorsFor(language).length"
          :id="`description-${language}-error`"
          role="alert"
          :class="errorTextClass"
        >{{ descriptionErrorsFor(language).join(' ') }}</p>
        <p :class="hintClass">
          {{ (descriptions[language] ?? '').length }} / {{ descriptionMaxLength }}
        </p>
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
