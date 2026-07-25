import type {
  CreateLexiconEntryRequest,
  GrammaticalCategory,
  LexiconAdminSort,
  LexiconEntryDto,
  LexiconEntryStatus,
  PagedResult,
  SortDirection,
  UpdateLexiconEntryRequest,
} from '~/types/api'

export interface LexiconListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: LexiconEntryStatus
  grammaticalCategory?: GrammaticalCategory
  playableInMeltho?: boolean
  sort?: LexiconAdminSort
  direction?: SortDirection
}

/**
 * Write-side bindings for the Lexicon backoffice. Every endpoint requires the
 * api:v1:admin scope; callers should treat 401/403 as "admin access required"
 * rather than a hard error. This is the editorial write path — part of Sabro
 * itself, not a client app — so unlike the public reads it may mutate content.
 */
export function useLexiconAdmin() {
  const api = useSabroApi()

  function list(params: LexiconListParams = {}) {
    return api<PagedResult<LexiconEntryDto>>('/admin/lexicon', {
      query: {
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
        search: params.search || undefined,
        status: params.status,
        grammaticalCategory: params.grammaticalCategory,
        playableInMeltho: params.playableInMeltho,
        sort: params.sort,
        direction: params.direction,
      },
    })
  }

  function getById(id: string) {
    return api<LexiconEntryDto>(`/admin/lexicon/${id}`)
  }

  function create(body: CreateLexiconEntryRequest) {
    return api<LexiconEntryDto>('/admin/lexicon', { method: 'POST', body })
  }

  function update(id: string, body: UpdateLexiconEntryRequest) {
    return api<LexiconEntryDto>(`/admin/lexicon/${id}`, { method: 'PUT', body })
  }

  function remove(id: string) {
    return api(`/admin/lexicon/${id}`, { method: 'DELETE' })
  }

  function publish(id: string) {
    return api<LexiconEntryDto>(`/admin/lexicon/${id}/publish`, { method: 'POST' })
  }

  function unpublish(id: string) {
    return api<LexiconEntryDto>(`/admin/lexicon/${id}/unpublish`, { method: 'POST' })
  }

  function setPlayable(id: string, playable: boolean) {
    return api<LexiconEntryDto>(`/admin/lexicon/${id}/playable`, {
      method: 'PUT',
      body: { playable },
    })
  }

  function uploadPronunciation(id: string, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return api<LexiconEntryDto>(`/admin/lexicon/${id}/pronunciation`, {
      method: 'POST',
      body: formData,
    })
  }

  function removePronunciation(id: string) {
    return api<LexiconEntryDto>(`/admin/lexicon/${id}/pronunciation`, { method: 'DELETE' })
  }

  return {
    list,
    getById,
    create,
    update,
    remove,
    publish,
    unpublish,
    setPlayable,
    uploadPronunciation,
    removePronunciation,
  }
}
