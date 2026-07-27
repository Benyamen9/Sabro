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
  }>(),
  { figure: null, submitting: false },
)

const emit = defineEmits<{
  (e: 'submit', payload: CreateHistoricalFigureRequest): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const name = ref(props.figure?.name ?? '')
const category = ref<HistoricalFigureCategory>(props.figure?.category ?? 'Patristic')
const period = ref<HistoricalPeriod>(props.figure?.period ?? 'PostChalcedonian')
const role = ref<HistoricalFigureRole>(props.figure?.role ?? 'Other')
const region = ref<HistoricalFigureRegion>(props.figure?.region ?? 'Mesopotamia')
const gender = ref<HistoricalFigureGender>(props.figure?.gender ?? 'Male')
// Empty string means "not yet decided" — a draft may omit it, publishing may not.
const tradition = ref<HistoricalFigureTradition | ''>(props.figure?.tradition ?? '')

// Era is entered as a signed century, so the field is a number input rather
// than a year. Kept as a string ref because an empty input is not 0.
const era = ref(props.figure?.era != null ? String(props.figure.era) : '')

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
