import type {
  CreateHistoricalFigureRequest,
  HistoricalFigureCategory,
  HistoricalFigureDto,
  HistoricalFigureRegion,
  HistoricalFigureRole,
  HistoricalFigureStatus,
  HistoricalPeriod,
  PagedResult,
  UpdateHistoricalFigureRequest,
} from '~/types/api'

export interface HistoricalFigureListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: HistoricalFigureStatus
  category?: HistoricalFigureCategory
  period?: HistoricalPeriod
  role?: HistoricalFigureRole
  region?: HistoricalFigureRegion
  playableInShmo?: boolean
}

/**
 * Write-side bindings for the Shmo roster backoffice. Every endpoint requires
 * the api:v1:admin scope; callers should treat 401/403 as "admin access
 * required" rather than a hard error. This is the editorial write path — part
 * of Sabro itself, not a client app — so unlike the public reads it may mutate
 * content.
 *
 * Unlike the Lexicon admin list, this one is served by a direct relational
 * query rather than a search index: the roster is a few hundred rows at most,
 * so `search` is a plain case-insensitive name match, and there is no sort or
 * direction parameter — results come back newest first.
 */
export function useHistoricalFiguresAdmin() {
  const api = useSabroApi()

  function list(params: HistoricalFigureListParams = {}) {
    return api<PagedResult<HistoricalFigureDto>>('/admin/historical-figures', {
      query: {
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
        search: params.search || undefined,
        status: params.status,
        category: params.category,
        period: params.period,
        role: params.role,
        region: params.region,
        playableInShmo: params.playableInShmo,
      },
    })
  }

  function getById(id: string) {
    return api<HistoricalFigureDto>(`/admin/historical-figures/${id}`)
  }

  function create(body: CreateHistoricalFigureRequest) {
    return api<HistoricalFigureDto>('/admin/historical-figures', { method: 'POST', body })
  }

  function update(id: string, body: UpdateHistoricalFigureRequest) {
    return api<HistoricalFigureDto>(`/admin/historical-figures/${id}`, { method: 'PUT', body })
  }

  function remove(id: string) {
    return api(`/admin/historical-figures/${id}`, { method: 'DELETE' })
  }

  function publish(id: string) {
    return api<HistoricalFigureDto>(`/admin/historical-figures/${id}/publish`, { method: 'POST' })
  }

  function unpublish(id: string) {
    return api<HistoricalFigureDto>(`/admin/historical-figures/${id}/unpublish`, { method: 'POST' })
  }

  function setPlayable(id: string, playable: boolean) {
    return api<HistoricalFigureDto>(`/admin/historical-figures/${id}/playable`, {
      method: 'PUT',
      body: { playable },
    })
  }

  return { list, getById, create, update, remove, publish, unpublish, setPlayable }
}
