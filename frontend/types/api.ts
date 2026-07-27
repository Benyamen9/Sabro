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
export type LexiconAdminSort = Schemas['LexiconAdminSort']

/*
 * Historical figures (the Shmo roster).
 *
 * TEMPORARILY HAND-WRITTEN. These belong in `api.generated.ts` like everything
 * else here, but the backend that defines them has not been built since it was
 * added, so the checked-in OpenAPI spec does not carry their schemas yet.
 *
 * To retire this block: build the API (which re-emits
 * `frontend/openapi/Sabro.API.json`), run `npm run generate:api-types`, then
 * replace each interface below with a `Schemas['...']` re-export. Consumers
 * import from `~/types/api`, so the swap is local to this file.
 *
 * Keep these in sync with src/Modules/Sabro.Historical/Domain and
 * Application/Figures until then.
 */
export type HistoricalFigureCategory = 'BiblicalOldTestament' | 'BiblicalNewTestament' | 'Patristic'
export type HistoricalFigureRole =
  | 'Prophet' | 'King' | 'Judge' | 'Apostle' | 'Evangelist' | 'Patriarch'
  | 'Bishop' | 'Translator' | 'Commentator' | 'Monk' | 'Martyr' | 'Other'
export type HistoricalFigureRegion =
  | 'IsraelJudah' | 'Mesopotamia' | 'Syria' | 'Persia' | 'Arabia' | 'Egypt' | 'Ethiopia'
  | 'AsiaMinor' | 'Greece' | 'Italy' | 'Armenia' | 'India' | 'Other'
export type HistoricalFigureTradition =
  | 'PreChalcedonian' | 'WestSyriac' | 'EastSyriac' | 'Coptic' | 'Armenian' | 'Ethiopian'
  | 'Malankara' | 'ByzantineChalcedonian' | 'Latin' | 'NotApplicable'
export type HistoricalFigureGender = 'Male' | 'Female'
export type HistoricalPeriod =
  | 'Primeval' | 'Patriarchal' | 'ExodusAndConquest' | 'Judges' | 'UnitedMonarchy'
  | 'DividedMonarchy' | 'ExileAndReturn' | 'SecondTemple' | 'Apostolic'
  | 'AnteNicene' | 'NiceneEra' | 'PostChalcedonian' | 'IslamicEra' | 'SyriacRenaissance'
  | 'LateMedieval' | 'ModernEra'
export type HistoricalFigureStatus = 'Draft' | 'Published'

export interface HistoricalFigureDto {
  id: string
  name: string
  category: HistoricalFigureCategory
  era: number
  period: HistoricalPeriod
  role: HistoricalFigureRole
  region: HistoricalFigureRegion
  tradition: HistoricalFigureTradition | null
  gender: HistoricalFigureGender
  status: HistoricalFigureStatus
  playableInShmo: boolean
  createdAt: string
  updatedAt: string
}

/** Public roster projection — no editorial state, no playable flag. */
export interface HistoricalFigureListItem {
  id: string
  name: string
  category: HistoricalFigureCategory
  era: number
  period: HistoricalPeriod
  role: HistoricalFigureRole
  region: HistoricalFigureRegion
  tradition: HistoricalFigureTradition | null
  gender: HistoricalFigureGender
}

export interface CreateHistoricalFigureRequest {
  name: string
  category: HistoricalFigureCategory
  era: number
  period: HistoricalPeriod
  role: HistoricalFigureRole
  region: HistoricalFigureRegion
  gender: HistoricalFigureGender
  tradition?: HistoricalFigureTradition | null
}

export type UpdateHistoricalFigureRequest = CreateHistoricalFigureRequest

export interface SetPlayableHistoricalFigureRequest {
  playable: boolean
}

// Play results.
export type GameResultDto = Schemas['GameResultDto']

// Meltho leaderboard.
export type MelthoLeaderboardDto = Schemas['MelthoLeaderboardDto']
export type MelthoLeaderboardEntryDto = Schemas['MelthoLeaderboardEntryDto']
export type MelthoLeaderboardMeDto = Schemas['MelthoLeaderboardMeDto']

// Public dictionary (every published word; anonymous).
export type DictionaryEntryListItem = Schemas['DictionaryEntryListItem']
export type DictionaryEntryDetail = Schemas['DictionaryEntryDetailResponse']
export type LexiconSearchHitDto = Schemas['LexiconSearchHitDto']

// Meltho play + library.
export type MelthoPuzzleMeaningDto = Schemas['MelthoPuzzleMeaningDto']
export type MelthoLibraryEntryDto = Schemas['MelthoLibraryEntryDto']
export type MelthoLibraryDetailDto = Schemas['MelthoLibraryDetailDto']
export type LibrarySort = Schemas['LibrarySort']
export type SortDirection = Schemas['SortDirection']

// Unified library (/library page: dictionary words, optionally filtered to ones played in Meltho).
export type UnifiedLibraryEntryDto = Schemas['UnifiedLibraryEntryDto']
export type SyriacLetter = Schemas['SyriacLetter']
export type SyriacLetterCode = Schemas['SyriacLetterCode']
export type SyriacVowel = Schemas['SyriacVowel']
export type LetterHardening = Schemas['LetterHardening']
export type HardeningSource = Schemas['HardeningSource']
