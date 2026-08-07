import type { LibrarySort, PagedResult, SortDirection, UnifiedLibraryEntryDto } from '~/types/api'

/**
 * One word as the unified library list renders it. `lastPlayedOn`/`timesPlayed` are null unless
 * the word has been served as a Meltho daily puzzle before today — always non-null when
 * `playedInMeltho: true` was requested, since that's the whole point of the filter.
 */
export interface UnifiedLibraryWord {
  id: string
  syriacUnvocalized: string
  syriacVocalized: string | null
  sblTransliteration: string | null
  grammaticalCategory: string
  letterCount: number
  meanings: { language: string, text: string }[]
  lastPlayedOn: string | null
  timesPlayed: number | null
}

export interface UnifiedLibraryListParams {
  page?: number
  pageSize?: number
  search?: string
  sort?: LibrarySort
  direction?: SortDirection
  playedInMeltho?: boolean
}

export function fromDto(item: UnifiedLibraryEntryDto): UnifiedLibraryWord {
  return {
    id: item.id,
    syriacUnvocalized: item.syriacUnvocalized,
    syriacVocalized: item.syriacVocalized ?? null,
    sblTransliteration: item.sblTransliteration ?? null,
    grammaticalCategory: item.grammaticalCategory,
    // int32 fields come through the generated types as number | string.
    letterCount: Number(item.letterCount),
    meanings: item.meanings,
    lastPlayedOn: item.lastPlayedOn ?? null,
    timesPlayed: item.timesPlayed === null || item.timesPlayed === undefined ? null : Number(item.timesPlayed),
  }
}

/**
 * Read-side bindings for the unified `/api/v1/library` endpoint — every published word,
 * optionally filtered to just the ones that have appeared in Meltho. Anonymous, like the
 * standalone dictionary and Meltho archive it composes server-side.
 */
export function useUnifiedLibrary() {
  const api = useSabroApi()

  async function listWords(params: UnifiedLibraryListParams = {}): Promise<PagedResult<UnifiedLibraryWord>> {
    const result = await api<PagedResult<UnifiedLibraryEntryDto>>('/library', {
      query: {
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 24,
        // Omit params entirely when unset so the URL/query stays clean and defaults apply server-side.
        ...(params.search ? { search: params.search } : {}),
        ...(params.sort ? { sort: params.sort } : {}),
        ...(params.direction ? { direction: params.direction } : {}),
        ...(params.playedInMeltho ? { playedInMeltho: true } : {}),
      },
    })
    return { ...result, items: result.items.map(fromDto) }
  }

  return { listWords }
}
