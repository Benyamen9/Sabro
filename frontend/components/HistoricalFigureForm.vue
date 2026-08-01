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
  }>(),
  { figure: null, submitting: false, prefill: null },
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

const canSubmit = computed(() => name.value.trim().length > 0 && eraValid.value && !props.submitting)

function onSubmit() {
  if (!canSubmit.value) return

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
    descriptions: descriptionLanguages
      .map(language => ({ language, text: (descriptions[language] ?? '').trim() }))
      .filter(d => d.text.length > 0),
  })
}

const fieldClass
  = 'w-full rounded-md border border-[var(--color-border-strong)] bg-[var(--color-bg-elevated)] px-3 py-2 font-sans text-sm text-[var(--color-text)] focus:border-[var(--color-accent)] focus:outline-none'
const labelClass = 'block font-sans text-sm font-medium text-[var(--color-text)]'
const hintClass = 'mt-1 font-sans text-xs text-[var(--color-text-faint)]'
</script>

<template>
  <form class="flex flex-col gap-6" @submit.prevent="onSubmit">
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
        :class="[fieldClass, 'mt-1 text-lg']"
      >
      <p :class="hintClass">{{ t('admin.historicalFigures.form.nameHint') }}</p>
    </div>

    <!-- Category + era -->
    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="figure-category" :class="labelClass">{{ t('admin.historicalFigures.form.category') }}</label>
        <select id="figure-category" v-model="category" :class="[fieldClass, 'mt-1']">
          <option v-for="value in HISTORICAL_FIGURE_CATEGORIES" :key="value" :value="value">
            {{ t(`admin.historicalFigures.category.${value}`) }}
          </option>
        </select>
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
          :class="[fieldClass, 'mt-1']"
        >
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
        <select id="figure-period" v-model="period" :class="[fieldClass, 'mt-1']">
          <option v-for="value in HISTORICAL_PERIODS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.period.${value}`) }}
          </option>
        </select>
        <p :class="hintClass">{{ t('admin.historicalFigures.form.periodHint') }}</p>
      </div>
      <div>
        <label for="figure-role" :class="labelClass">{{ t('admin.historicalFigures.form.role') }}</label>
        <select id="figure-role" v-model="role" :class="[fieldClass, 'mt-1']">
          <option v-for="value in HISTORICAL_FIGURE_ROLES" :key="value" :value="value">
            {{ t(`admin.historicalFigures.role.${value}`) }}
          </option>
        </select>
        <p :class="hintClass">{{ t('admin.historicalFigures.form.roleHint') }}</p>
      </div>
      <div>
        <label for="figure-region" :class="labelClass">{{ t('admin.historicalFigures.form.region') }}</label>
        <select id="figure-region" v-model="region" :class="[fieldClass, 'mt-1']">
          <option v-for="value in HISTORICAL_FIGURE_REGIONS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.region.${value}`) }}
          </option>
        </select>
      </div>
    </div>

    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="figure-tradition" :class="labelClass">{{ t('admin.historicalFigures.form.tradition') }}</label>
        <select id="figure-tradition" v-model="tradition" :class="[fieldClass, 'mt-1']">
          <option value="">{{ t('admin.historicalFigures.form.traditionUnset') }}</option>
          <option v-for="value in HISTORICAL_FIGURE_TRADITIONS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.tradition.${value}`) }}
          </option>
        </select>
        <p :class="hintClass">{{ t('admin.historicalFigures.form.traditionHint') }}</p>
      </div>
      <div>
        <label for="figure-gender" :class="labelClass">{{ t('admin.historicalFigures.form.gender') }}</label>
        <select id="figure-gender" v-model="gender" :class="[fieldClass, 'mt-1']">
          <option v-for="value in HISTORICAL_FIGURE_GENDERS" :key="value" :value="value">
            {{ t(`admin.historicalFigures.gender.${value}`) }}
          </option>
        </select>
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
          :class="[fieldClass, 'resize-y']"
        />
        <p :class="hintClass">
          {{ (descriptions[language] ?? '').length }} / {{ descriptionMaxLength }}
        </p>
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
