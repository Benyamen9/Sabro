/**
 * Public API types for the frontend. Most DTOs are re-exported from
 * `api.generated.ts`, which is produced by `npm run generate:api-types`
 * from the OpenAPI spec emitted by the backend build
 * (`frontend/openapi/Sabro.API.json`).
 *
 * Don't edit `api.generated.ts` directly — change the backend DTO, rebuild
 * the API, and regenerate. One override lives here:
 *
 *  `PagedResult<T>` — the OpenAPI spec emits one schema per generic
 *  instantiation (`PagedResultOfSourceDto`, etc.). We keep a single
 *  generic shape so consumers can write `PagedResult<SourceDto>`
 *  instead of importing instantiation-specific names. Also narrows
 *  `total/page/pageSize` from `number | string` (defensive int32) to
 *  `number` since the API only sends numbers.
 */

import type { components } from './api.generated'

type Schemas = components['schemas']

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

// Enums — all come through as string literal unions from the generated schemas.
export type Testament = Schemas['Testament']
export type ApprovalStatus = Schemas['ApprovalStatus']
export type ApprovalTargetType = Schemas['ApprovalTargetType']
export type Role = Schemas['Role']
export type ScriptVariant = Schemas['ScriptVariant']
export type SuggestedEditStatus = Schemas['SuggestedEditStatus']
export type SuggestedEditTargetType = Schemas['SuggestedEditTargetType']

// Translations.
export type AuthorDto = Schemas['AuthorDto']
export type SourceDto = Schemas['SourceDto']
export type SegmentDto = Schemas['SegmentDto']
export type SegmentSearchHitDto = Schemas['SegmentSearchHitDto']
export type AnnotationDto = Schemas['AnnotationDto']
export type AnnotationSearchHitDto = Schemas['AnnotationSearchHitDto']

// Biblical.
export type BiblicalBookDto = Schemas['BiblicalBookDto']
export type BiblicalPassageDto = Schemas['BiblicalPassageDto']
export type BiblicalPassageSearchHitDto = Schemas['BiblicalPassageSearchHitDto']

// Reviews.
export type ApprovalDto = Schemas['ApprovalDto']
export type EffectiveChapterApprovalsDto = Schemas['EffectiveChapterApprovalsDto']
export type SuggestedEditDto = Schemas['SuggestedEditDto']

// Identity.
export type UserProfileDto = Schemas['UserProfileDto']
export type UpdateUserProfileRequest = Schemas['UpdateUserProfileRequest']
export type ProfileExportDto = Schemas['ProfileExportDto']

// Lexicon.
export type GrammaticalCategory = Schemas['GrammaticalCategory']
export type LexiconEntryStatus = Schemas['LexiconEntryStatus']
export type LexiconMeaningDto = Schemas['LexiconMeaningDto']
export type LexiconEntryDto = Schemas['LexiconEntryDto']
export type CreateLexiconMeaningRequest = Schemas['CreateLexiconMeaningRequest']
export type CreateLexiconEntryRequest = Schemas['CreateLexiconEntryRequest']
export type UpdateLexiconEntryRequest = Schemas['UpdateLexiconEntryRequest']
export type SetPlayableLexiconEntryRequest = Schemas['SetPlayableLexiconEntryRequest']

// Play results.
export type GameResultDto = Schemas['GameResultDto']

// Meltho leaderboard.
export type MelthoLeaderboardDto = Schemas['MelthoLeaderboardDto']
export type MelthoLeaderboardEntryDto = Schemas['MelthoLeaderboardEntryDto']
export type MelthoLeaderboardMeDto = Schemas['MelthoLeaderboardMeDto']

// Meltho play + library.
export type MelthoPuzzleMeaningDto = Schemas['MelthoPuzzleMeaningDto']
export type MelthoLibraryEntryDto = Schemas['MelthoLibraryEntryDto']
export type MelthoLibraryDetailDto = Schemas['MelthoLibraryDetailDto']
export type LibrarySort = Schemas['LibrarySort']
export type SortDirection = Schemas['SortDirection']
export type SyriacLetter = Schemas['SyriacLetter']
export type SyriacLetterCode = Schemas['SyriacLetterCode']
export type SyriacVowel = Schemas['SyriacVowel']
export type LetterHardening = Schemas['LetterHardening']
export type HardeningSource = Schemas['HardeningSource']
