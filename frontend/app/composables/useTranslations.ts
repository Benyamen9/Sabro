import type {
  AuthorDto,
  PagedResult,
  SegmentDto,
  SegmentSearchHitDto,
  SourceDto,
} from '~/types/api'

export interface ListParams {
  page?: number
  pageSize?: number
}

export interface SegmentSearchParams {
  q?: string
  sourceId?: string
  chapter?: number
  verse?: number
  page?: number
  pageSize?: number
}

/**
 * Read-side bindings for the Translations module. All endpoints require the
 * api:v1:read scope; callers should treat 401/403 as "not signed in" rather
 * than a hard error once auth is wired.
 */
export function useTranslations() {
  const api = useSabroApi()

  function listSources(params: ListParams = {}) {
    return api<PagedResult<SourceDto>>('/sources', {
      query: { page: params.page ?? 1, pageSize: params.pageSize ?? 20 },
    })
  }

  function getSourceById(id: string) {
    return api<SourceDto>(`/sources/${id}`)
  }

  function getAuthorById(id: string) {
    return api<AuthorDto>(`/authors/${id}`)
  }

  function getSegmentById(id: string) {
    return api<SegmentDto>(`/segments/${id}`)
  }

  function searchSegments(params: SegmentSearchParams) {
    const query: Record<string, string | number> = {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }
    if (params.q) query.q = params.q
    if (params.sourceId) query.sourceId = params.sourceId
    if (params.chapter !== undefined) query.chapter = params.chapter
    if (params.verse !== undefined) query.verse = params.verse

    return api<PagedResult<SegmentSearchHitDto>>('/segments/search', { query })
  }

  return { listSources, getSourceById, getAuthorById, getSegmentById, searchSegments }
}
