import type {
  BethGazoModeDto, BethGazoSectionDto,
  ChantDto,
  ChantStatus,
  CreateChantRequest,
  PagedResult,
  UpdateChantRequest,
} from '~/types/api'

export interface ChantListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: ChantStatus
  sectionId?: string
  modeId?: string
  playableInNahlo?: boolean
}

/** The API's own ceiling on `pageSize`; asking for more is a 400, not a bigger page. */
const MAX_PAGE_SIZE = 100

/**
 * A backstop on {@link useChantsAdmin.listAll}, not an expected limit. Ten pages
 * is far past the treasury's plausible size — it exists so a paging bug loops a
 * finite number of times rather than forever.
 */
const MAX_PAGES = 10

/**
 * Write-side bindings for the Beth Gazo backoffice — the chants Nahlo draws its
 * daily puzzle from. Every endpoint requires the api:v1:admin scope plus a Nahlo
 * grant; callers should treat 401/403 as "admin access required" rather than a
 * hard error. This is the editorial write path — part of Sabro itself, not a
 * client app — so unlike the public reads it may mutate content.
 *
 * Like the Shmo roster and unlike the Lexicon, the list is served by a direct
 * relational query rather than a search index: `search` is a plain
 * case-insensitive match on the transliteration or the Syriac, and results come
 * back newest first.
 */
export function useChantsAdmin() {
  const api = useSabroApi()

  function list(params: ChantListParams = {}) {
    return api<PagedResult<ChantDto>>('/admin/chants', {
      query: {
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 25,
        search: params.search || undefined,
        status: params.status,
        sectionId: params.sectionId,
        modeId: params.modeId,
        playableInNahlo: params.playableInNahlo,
      },
    })
  }

  /**
   * Every chant, paged through to the end.
   *
   * For the solqin picker, which has to offer the whole treasury: a chant that
   * inherits its melody points at one specific other chant, and one missing from
   * the list cannot be chosen at all. A single capped request would silently
   * offer the newest hundred and look complete — the same shape of trap that had
   * the gloss scripts reporting words absent because the list stopped at its cap.
   */
  async function listAll(): Promise<ChantDto[]> {
    const all: ChantDto[] = []
    for (let page = 1; page <= MAX_PAGES; page++) {
      const result = await list({ page, pageSize: MAX_PAGE_SIZE })
      all.push(...result.items)
      if (all.length >= result.total || result.items.length === 0) break
    }
    return all
  }

  function listModes() {
    return api<BethGazoModeDto[]>('/admin/chants/modes')
  }

  /** The sections, each with the modes it admits — see BethGazoSectionDto. */
  function listSections() {
    return api<BethGazoSectionDto[]>('/admin/chants/sections')
  }

  function getById(id: string) {
    return api<ChantDto>(`/admin/chants/${id}`)
  }

  function create(body: CreateChantRequest) {
    return api<ChantDto>('/admin/chants', { method: 'POST', body })
  }

  function update(id: string, body: UpdateChantRequest) {
    return api<ChantDto>(`/admin/chants/${id}`, { method: 'PUT', body })
  }

  function remove(id: string) {
    return api(`/admin/chants/${id}`, { method: 'DELETE' })
  }

  function publish(id: string) {
    return api<ChantDto>(`/admin/chants/${id}/publish`, { method: 'POST' })
  }

  function unpublish(id: string) {
    return api<ChantDto>(`/admin/chants/${id}/unpublish`, { method: 'POST' })
  }

  function setPlayable(id: string, playable: boolean) {
    return api<ChantDto>(`/admin/chants/${id}/playable`, {
      method: 'PUT',
      body: { playable },
    })
  }

  function uploadAudio(id: string, file: File) {
    const formData = new FormData()
    formData.append('file', file)
    return api<ChantDto>(`/admin/chants/${id}/audio`, { method: 'POST', body: formData })
  }

  function removeAudio(id: string) {
    return api<ChantDto>(`/admin/chants/${id}/audio`, { method: 'DELETE' })
  }

  return {
    list,
    listAll,
    listModes,
    listSections,
    getById,
    create,
    update,
    remove,
    publish,
    unpublish,
    setPlayable,
    uploadAudio,
    removeAudio,
  }
}
