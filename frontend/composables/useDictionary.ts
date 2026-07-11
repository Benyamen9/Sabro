import type { FetchError } from 'ofetch'
import type {
  DictionaryEntryDetail,
  DictionaryEntryListItem,
  LexiconSearchHitDto,
  MelthoLibraryDetailDto,
  PagedResult,
} from '~/types/api'

/**
 * One word as the library list renders it, regardless of whether it came from
 * the alphabetical browse (relational, `DictionaryEntryListItem`) or a search
 * (Meilisearch, `LexiconSearchHitDto` — a different wire shape with parallel
 * meaning arrays and no letter count).
 */
export interface DictionaryWord {
  id: string
  syriacUnvocalized: string
  syriacVocalized: string | null
  sblTransliteration: string | null
  grammaticalCategory: string
  letterCount: number
  meanings: { language: string, text: string }[]
}

/** The unified word detail behind /library/{id}, whichever endpoint resolved it. */
export interface LibraryWordDetail {
  id: string
  syriacUnvocalized: string
  syriacVocalized: string | null
  sblTransliteration: string | null
  grammaticalCategory: string
  letterCount: number
  root: string | null
  meanings: { language: string, text: string }[]
  composition: DictionaryEntryDetail['composition']
  playedInMeltho: boolean
}

export interface DictionaryListParams {
  page?: number
  pageSize?: number
  search?: string
}

/**
 * The backend counts Unicode letters only — combining marks (seyame, vowel
 * points) are excluded. Search hits don't carry the count, so mirror that rule.
 */
export function countSyriacLetters(syriac: string): number {
  return Array.from(syriac).filter(ch => /\p{L}/u.test(ch)).length
}

/** Maps a Meilisearch hit (parallel meaning arrays) onto the list view model. */
export function searchHitToWord(hit: LexiconSearchHitDto): DictionaryWord {
  return {
    id: hit.id,
    syriacUnvocalized: hit.syriacUnvocalized,
    syriacVocalized: hit.syriacVocalized ?? null,
    sblTransliteration: hit.sblTransliteration ?? null,
    grammaticalCategory: hit.grammaticalCategory,
    letterCount: countSyriacLetters(hit.syriacUnvocalized),
    meanings: hit.meaningTexts.map((text, i) => ({ language: hit.meaningLanguages[i] ?? '', text })),
  }
}

/**
 * Read-side bindings for the dictionary side of the word library — every
 * published word, served anonymously. Browsing is alphabetical from Postgres;
 * a search query switches to the Meilisearch endpoint (typo-tolerant,
 * transliteration synonyms), and both shapes are mapped to `DictionaryWord`.
 */
export function useDictionary() {
  const api = useSabroApi()

  function fromListItem(item: DictionaryEntryListItem): DictionaryWord {
    return {
      id: item.id,
      syriacUnvocalized: item.syriacUnvocalized,
      syriacVocalized: item.syriacVocalized ?? null,
      sblTransliteration: item.sblTransliteration ?? null,
      grammaticalCategory: item.grammaticalCategory,
      letterCount: item.letterCount,
      meanings: item.meanings,
    }
  }

  async function listWords(params: DictionaryListParams = {}): Promise<PagedResult<DictionaryWord>> {
    const page = params.page ?? 1
    const pageSize = params.pageSize ?? 24

    if (params.search) {
      const result = await api<PagedResult<LexiconSearchHitDto>>('/dictionary/search', {
        query: { q: params.search, page, pageSize },
      })
      return { ...result, items: result.items.map(searchHitToWord) }
    }

    const result = await api<PagedResult<DictionaryEntryListItem>>('/dictionary', {
      query: { page, pageSize },
    })
    return { ...result, items: result.items.map(fromListItem) }
  }

  /**
   * Resolves one word for the unified /library/{id} detail. The dictionary
   * endpoint covers every published word; a served word that was later
   * unpublished stays reachable through the Meltho library endpoint (a word
   * that has been a daily puzzle never disappears), so a dictionary 404 falls
   * back there before giving up.
   */
  async function getWord(id: string): Promise<LibraryWordDetail> {
    try {
      const detail = await api<DictionaryEntryDetail>(`/dictionary/${id}`)
      return {
        id: detail.id,
        syriacUnvocalized: detail.syriacUnvocalized,
        syriacVocalized: detail.syriacVocalized ?? null,
        sblTransliteration: detail.sblTransliteration ?? null,
        grammaticalCategory: detail.grammaticalCategory,
        letterCount: detail.letterCount,
        root: detail.root ?? null,
        meanings: detail.meanings,
        composition: detail.composition,
        playedInMeltho: detail.playedInMeltho,
      }
    }
    catch (error) {
      if ((error as FetchError).statusCode !== 404) throw error

      const played = await api<MelthoLibraryDetailDto>(`/play/meltho/library/${id}`)
      const today = new Date().toISOString().slice(0, 10)
      return {
        id: played.lexiconEntryId,
        syriacUnvocalized: played.syriacUnvocalized,
        syriacVocalized: played.syriacVocalized ?? null,
        sblTransliteration: played.sblTransliteration ?? null,
        grammaticalCategory: played.grammaticalCategory,
        letterCount: played.playableLength,
        root: played.root ?? null,
        meanings: played.meanings,
        composition: played.composition,
        playedInMeltho: played.playedOn.some(date => date < today),
      }
    }
  }

  return { listWords, getWord }
}
