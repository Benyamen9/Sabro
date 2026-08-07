import type { LibrarySort, MelthoLibraryDetailDto, MelthoLibraryEntryDto, PagedResult, SortDirection } from '~/types/api'

export interface LibraryListParams {
  page?: number
  pageSize?: number
  sort?: LibrarySort
  direction?: SortDirection
  search?: string
}

/**
 * Read-side bindings for the public Meltho word library. The endpoints are
 * anonymous (the daily word's past archive is shared content), so no auth is
 * required — a 404 from the detail endpoint means the word has never been a
 * past puzzle.
 */
export function useMelthoLibrary() {
  const api = useSabroApi()

  function listWords(params: LibraryListParams = {}) {
    return api<PagedResult<MelthoLibraryEntryDto>>('/play/meltho/library', {
      query: {
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
        sort: params.sort ?? 'Recent',
        direction: params.direction ?? 'Descending',
        // Omit the param entirely when there's no query so the URL stays clean.
        ...(params.search ? { search: params.search } : {}),
      },
    })
  }

  function getWord(lexiconEntryId: string) {
    return api<MelthoLibraryDetailDto>(`/play/meltho/library/${lexiconEntryId}`)
  }

  return { listWords, getWord }
}
