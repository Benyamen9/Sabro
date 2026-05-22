import type {
  GrammaticalCategory,
  LexiconEntryDto,
  LexiconSearchHitDto,
  PagedResult,
} from '~/types/api'

export interface LexiconSearchParams {
  q?: string
  category?: GrammaticalCategory
  rootId?: string
  page?: number
  pageSize?: number
}

export interface LexiconListParams {
  page?: number
  pageSize?: number
}

/**
 * Read-side bindings for the Lexicon module. All endpoints require the
 * api:v1:read scope; callers should treat 401/403 as "not signed in" rather
 * than a hard error once auth is wired.
 */
export function useLexicon() {
  const api = useSabroApi()

  function list(params: LexiconListParams = {}) {
    return api<PagedResult<LexiconEntryDto>>('/lexicon-entries', {
      query: { page: params.page ?? 1, pageSize: params.pageSize ?? 20 },
    })
  }

  function getById(id: string) {
    return api<LexiconEntryDto>(`/lexicon-entries/${id}`)
  }

  function search(params: LexiconSearchParams) {
    const query: Record<string, string | number> = {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }
    if (params.q) query.q = params.q
    if (params.category) query.category = params.category
    if (params.rootId) query.rootId = params.rootId

    return api<PagedResult<LexiconSearchHitDto>>('/lexicon-entries/search', { query })
  }

  return { list, getById, search }
}
