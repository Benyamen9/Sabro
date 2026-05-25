/**
 * Hand-written mirror of the backend DTOs in src/Modules/Sabro.Lexicon/Application,
 * src/Modules/Sabro.Translations/Application, and src/Sabro.Shared/Pagination.
 * Replace with openapi-typescript-generated types once the OpenAPI spec is
 * exposed at a stable URL.
 */

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

export interface LexiconMeaningDto {
  language: string
  text: string
}

export interface LexiconEntryDto {
  id: string
  syriacUnvocalized: string
  syriacVocalized: string | null
  rootId: string | null
  sblTransliteration: string
  transliterationVariants: string[]
  grammaticalCategory: GrammaticalCategory
  morphology: string | null
  meanings: LexiconMeaningDto[]
  createdAt: string
  updatedAt: string
}

/**
 * Lexicon search hit — denormalized projection used by GET /lexicon-entries/search.
 * Differences from LexiconEntryDto: rootForm is denormalized in, meanings are
 * split into flat texts + languages arrays, grammaticalCategory is the raw
 * enum string (server serializes via JsonStringEnumConverter).
 */
export interface LexiconSearchHitDto {
  id: string
  syriacUnvocalized: string
  syriacVocalized: string | null
  sblTransliteration: string
  transliterationVariants: string[]
  rootId: string | null
  rootForm: string | null
  grammaticalCategory: string
  morphology: string | null
  meaningTexts: string[]
  meaningLanguages: string[]
}

export interface AuthorDto {
  id: string
  name: string
  syriacName: string | null
  title: string | null
  createdAt: string
  updatedAt: string
}

export interface SourceDto {
  id: string
  authorId: string
  title: string
  originalLanguageCode: string | null
  description: string | null
  createdAt: string
  updatedAt: string
}

export interface SegmentDto {
  id: string
  sourceId: string
  chapterNumber: number
  verseNumber: number
  textVersionId: string
  content: string
  version: number
  previousVersionId: string | null
  createdAt: string
  updatedAt: string
}

/**
 * Segment search hit — flat projection used by GET /segments/search.
 * Reflects only the latest indexed version of each segment.
 */
export interface SegmentSearchHitDto {
  id: string
  sourceId: string
  chapterNumber: number
  verseNumber: number
  textVersionId: string
  content: string
  version: number
}
