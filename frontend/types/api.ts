/**
 * Hand-written mirror of the backend DTOs in src/Modules/Sabro.Lexicon/Application
 * and src/Sabro.Shared/Pagination. Replace with openapi-typescript-generated
 * types once the OpenAPI spec is exposed at a stable URL.
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
