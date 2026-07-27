import type {
  HistoricalFigureCategory,
  HistoricalFigureGender,
  HistoricalFigureRegion,
  HistoricalFigureRole,
  HistoricalFigureStatus,
  HistoricalFigureTradition,
} from '~/types/api'

/**
 * Option lists for the Shmo roster backoffice, in the order the enums are
 * declared in src/Modules/Sabro.Historical/Domain — not alphabetically, so the
 * dropdowns read in the order an editor thinks in (biblical roles before
 * ecclesiastical ones, and so on). Keep in sync with the domain enums; the API
 * rejects unknown values, so drift shows up as a failed save rather than bad data.
 */
export const HISTORICAL_FIGURE_CATEGORIES: readonly HistoricalFigureCategory[] = [
  'BiblicalOldTestament',
  'BiblicalNewTestament',
  'Patristic',
] as const

export const HISTORICAL_FIGURE_ROLES: readonly HistoricalFigureRole[] = [
  'Prophet',
  'King',
  'Judge',
  'Apostle',
  'Evangelist',
  'Patriarch',
  'Bishop',
  'Translator',
  'Commentator',
  'Monk',
  'Martyr',
  'Other',
] as const

export const HISTORICAL_FIGURE_REGIONS: readonly HistoricalFigureRegion[] = [
  'IsraelJudah',
  'Mesopotamia',
  'Syria',
  'Persia',
  'Egypt',
  'AsiaMinor',
  'Other',
] as const

export const HISTORICAL_FIGURE_TRADITIONS: readonly HistoricalFigureTradition[] = [
  'WestSyriac',
  'EastSyriac',
  'ByzantineChalcedonian',
  'NotApplicable',
] as const

export const HISTORICAL_FIGURE_GENDERS: readonly HistoricalFigureGender[] = ['Male', 'Female'] as const

export const HISTORICAL_FIGURE_STATUSES: readonly HistoricalFigureStatus[] = ['Draft', 'Published'] as const

/**
 * Widest centuries the API accepts — mirrors HistoricalFigure.MinEra/MaxEra.
 * Signed: negative is BC, positive is AD, and there is no century zero. The
 * lower bound reaches back to the primeval genealogies of Genesis 1–11, whose
 * dating depends on which traditional chronology you follow.
 */
export const MIN_ERA = -60
export const MAX_ERA = 21

/**
 * Renders a signed century as a human-readable era, e.g. -10 -> "10th c. BC".
 * The ordinal suffix comes from i18n so it can be localised; the era itself is
 * a bare number in the data because Shmo compares it numerically for its
 * higher/lower hint.
 */
export function formatEra(era: number, t: (key: string, named?: Record<string, unknown>) => string): string {
  const century = Math.abs(era)
  return t(era < 0 ? 'admin.historicalFigures.eraBc' : 'admin.historicalFigures.eraAd', { century })
}
