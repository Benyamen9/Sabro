/**
 * Public API types for the frontend. Most DTOs are re-exported from
 * `api.generated.ts`, which is produced by `npm run generate:api-types`
 * from the OpenAPI spec emitted by the backend build
 * (`frontend/openapi/Sabro.API.json`).
 *
 * Don't edit `api.generated.ts` directly — change the backend DTO, rebuild
 * the API, and regenerate. Two small overrides live here:
 *
 *  1. `PagedResult<T>` — the OpenAPI spec emits one schema per generic
 *     instantiation (`PagedResultOfSourceDto`, etc.). We keep a single
 *     generic shape so consumers can write `PagedResult<SourceDto>`
 *     instead of importing instantiation-specific names. Also narrows
 *     `total/page/pageSize` from `number | string` (defensive int32) to
 *     `number` since the API only sends numbers.
 *
 *  2. `GrammaticalCategory` and `Testament` — the backend's
 *     StringEnumSchemaTransformer correctly converts most enum schemas to
 *     string literal unions, but these two are emitted as `integer` for
 *     reasons that aren't worth fighting. Both serialize as their member
 *     name at runtime via the global JsonStringEnumConverter, so we
 *     hand-override them here.
 */

import type { components } from './api.generated'

type Schemas = components['schemas']

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export type GrammaticalCategory =
  | 'Noun'
  | 'Verb'
  | 'Adjective'
  | 'Adverb'
  | 'Pronoun'
  | 'Preposition'
  | 'Conjunction'
  | 'Particle'
  | 'Numeral'
  | 'Interjection'
  | 'Other'

export type Testament = 'Old' | 'New'

// Enums that come through correctly from the generated schemas.
export type ApprovalStatus = Schemas['ApprovalStatus']
export type ApprovalTargetType = Schemas['ApprovalTargetType']
export type Role = Schemas['Role']
export type ScriptVariant = Schemas['ScriptVariant']
export type SuggestedEditStatus = Schemas['SuggestedEditStatus']
export type SuggestedEditTargetType = Schemas['SuggestedEditTargetType']

// Lexicon — generated `grammaticalCategory: number` overridden to the
// string union above so callers get useful autocomplete.
export type LexiconMeaningDto = Schemas['LexiconMeaningDto']
export type LexiconEntryDto = Omit<Schemas['LexiconEntryDto'], 'grammaticalCategory'> & {
  grammaticalCategory: GrammaticalCategory
}
export type LexiconSearchHitDto = Schemas['LexiconSearchHitDto']
export type LexiconRootDto = Schemas['LexiconRootDto']

// Translations.
export type AuthorDto = Schemas['AuthorDto']
export type SourceDto = Schemas['SourceDto']
export type SegmentDto = Schemas['SegmentDto']
export type SegmentSearchHitDto = Schemas['SegmentSearchHitDto']
export type AnnotationDto = Schemas['AnnotationDto']
export type AnnotationSearchHitDto = Schemas['AnnotationSearchHitDto']

// Biblical — generated `testament: number` overridden to the string union.
export type BiblicalBookDto = Omit<Schemas['BiblicalBookDto'], 'testament'> & {
  testament: Testament
}
export type BiblicalPassageDto = Schemas['BiblicalPassageDto']
export type BiblicalPassageSearchHitDto = Omit<Schemas['BiblicalPassageSearchHitDto'], 'testament'> & {
  testament: Testament
}

// Reviews.
export type ApprovalDto = Schemas['ApprovalDto']
export type EffectiveChapterApprovalsDto = Schemas['EffectiveChapterApprovalsDto']
export type SuggestedEditDto = Schemas['SuggestedEditDto']

// Identity.
export type UserProfileDto = Schemas['UserProfileDto']
