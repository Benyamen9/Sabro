import type { HistoricalFigureListItem, PagedResult, ShmoCollectionDto } from '~/types/api'
import { buildGallery } from '~/utils/gallery'

/** The roster endpoint's page-size ceiling (PageRequest.MaxPageSize). */
const ROSTER_PAGE_SIZE = 200

/**
 * Beth Ṣalme's data: the public roster (every card, rendered on the server) and,
 * for a signed-in player, their collection (who they met and found), fetched in
 * the browser because it needs their token. Signed out, every card is locked.
 */
export function useFigureGallery() {
  const api = useSabroApi()
  const { isConfigured, isSignedIn } = useAuth()

  const roster = useAsyncData('beth-salme-roster', async () => {
    const items: HistoricalFigureListItem[] = []
    for (let page = 1; ; page++) {
      const result = await api<PagedResult<HistoricalFigureListItem>>('/historical-figures', {
        query: { page, pageSize: ROSTER_PAGE_SIZE },
      })
      items.push(...result.items)
      if (items.length >= result.total || result.items.length === 0) return items
    }
  })

  const collection = ref<ShmoCollectionDto | null>(null)
  const collectionFailed = ref(false)

  async function loadCollection() {
    if (!isConfigured.value || !isSignedIn.value) return
    collectionFailed.value = false
    try {
      collection.value = await api<ShmoCollectionDto>('/play/shmo/collection')
    }
    catch {
      // The cards still render, all locked; the page says the collection did not load.
      collectionFailed.value = true
    }
  }

  onMounted(loadCollection)

  const figures = computed(() => buildGallery(roster.data.value ?? [], collection.value?.cards ?? []))

  return {
    figures,
    pending: roster.pending,
    rosterFailed: computed(() => Boolean(roster.error.value)),
    collectionFailed,
    signedIn: computed(() => isConfigured.value && isSignedIn.value),
    canSignIn: isConfigured,
  }
}
