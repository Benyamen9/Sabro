import type { ReaderPlaceDto, ReaderShelfDto } from '~/types/api'
import { mergePlaces, parseStoredPlaces, type LibraryBook, type LibraryPlace, type LibraryPlaceLabel, type LibraryPlaces } from '~/utils/libraryPlaces'

const STORAGE_KEY = 'sabro:library:places'
/** Beth Gazo's own ribbon from before the shelf: the last chant played, by id. Moved in once. */
const LEGACY_BETH_GAZO_KEY = 'sabro:beth-gazo:ribbon'

function readDevice(): LibraryPlaces {
  try {
    const places = parseStoredPlaces(localStorage.getItem(STORAGE_KEY))
    const legacy = localStorage.getItem(LEGACY_BETH_GAZO_KEY)
    if (legacy && !places['beth-gazo']) {
      // Its time is unknown: the epoch, so any place kept anywhere else wins over it.
      places['beth-gazo'] = { itemKey: legacy, openedAt: new Date(0).toISOString(), label: null }
    }
    if (legacy) {
      localStorage.removeItem(LEGACY_BETH_GAZO_KEY)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(places))
    }
    return places
  }
  catch {
    return {}
  }
}

function writeDevice(places: LibraryPlaces) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(places))
  }
  catch {
    // No storage (a private window, blocked site data): the ribbon lasts this visit only.
  }
}

/**
 * The red ribbon in each book (see utils/libraryPlaces). Every reader keeps it on the device; a
 * signed-in reader's goes up to Sabro too, so it follows them, and what they kept while signed out
 * goes up on their next signed-in visit. Browser only: nothing here runs during SSR.
 */
export function useLibraryPlaces() {
  const api = useSabroApi()
  const { isConfigured, isSignedIn } = useAuth()
  const { locale } = useI18n()
  const { fetchShelf } = useReaderShelf()
  const { label: grammarLabel } = useGrammar()

  /** Sabro sends no label for a grammar table (its pages live in the hub): name it from its slug. */
  function named(all: LibraryPlaces): LibraryPlaces {
    const kept = all.grammar
    return kept ? { ...all, grammar: { ...kept, label: grammarLabel(kept.itemKey) ?? kept.label } } : all
  }

  const places = useState<LibraryPlaces>('library-places', () => ({}))
  const loaded = useState('library-places-loaded', () => false)

  const signedIn = computed(() => isConfigured.value && isSignedIn.value)

  function send(book: LibraryBook, place: LibraryPlace) {
    return api<ReaderPlaceDto>(`/library/me/places/${book}`, {
      method: 'PUT',
      query: { language: locale.value },
      body: { itemKey: place.itemKey, openedAt: place.openedAt },
    })
  }

  /** Reads the device, then Sabro for a signed-in reader, sending up whatever only the device had. */
  async function load() {
    if (import.meta.server) return
    const device = readDevice()
    places.value = named(device)
    loaded.value = true
    if (!signedIn.value) return

    let shelf: ReaderShelfDto
    try {
      shelf = await fetchShelf()
    }
    catch {
      return // Sabro unreachable: the device's places stand.
    }

    const { merged, toSend } = mergePlaces(device, shelf.places)
    places.value = named(merged)
    writeDevice(places.value)
    for (const { book, place } of toSend) {
      try {
        const kept = await send(book, place)
        if (kept.label) places.value = { ...places.value, [book]: { ...place, label: kept.label } }
      }
      catch {
        // A place for an item since unpublished (404), or Sabro briefly away: the device keeps it.
      }
    }
    writeDevice(places.value)
  }

  /** Moves the ribbon in `book` to the item just opened. */
  function record(book: LibraryBook, itemKey: string, label: LibraryPlaceLabel | null) {
    if (import.meta.server) return
    const place: LibraryPlace = { itemKey, openedAt: new Date().toISOString(), label }
    places.value = { ...places.value, [book]: place }
    writeDevice(places.value)
    if (signedIn.value) send(book, place).catch(() => {})
  }

  /** Gives a place a name it lacked (Beth Gazo's old ribbon kept only an id), without moving it. */
  function name(book: LibraryBook, itemKey: string, label: LibraryPlaceLabel) {
    const place = places.value[book]
    if (!place || place.itemKey !== itemKey || place.label) return
    places.value = { ...places.value, [book]: { ...place, label } }
    writeDevice(places.value)
  }

  return { places, loaded, load, record, name }
}
