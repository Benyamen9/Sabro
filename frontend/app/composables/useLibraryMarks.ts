import type { ReaderMarkDto, ReaderShelfDto } from '~/types/api'
import { markId, mergeMarks, parseStoredMarks, sortMarks, type LibraryMark } from '~/utils/libraryMarks'
import type { LibraryBook, LibraryPlaceLabel } from '~/utils/libraryPlaces'

const STORAGE_KEY = 'sabro:library:marks'

function readDevice(): LibraryMark[] {
  try {
    return parseStoredMarks(localStorage.getItem(STORAGE_KEY))
  }
  catch {
    return []
  }
}

function writeDevice(marks: LibraryMark[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(marks))
  }
  catch {
    // No storage (a private window, blocked site data): the marks last this visit only.
  }
}

/**
 * The reader's ܀ marks (see utils/libraryMarks). Every reader keeps them on the device; a signed-in
 * reader's go up to Sabro too, so they follow them, and what they marked while signed out goes up
 * on their next signed-in visit with its date. Browser only: nothing here runs during SSR.
 */
export function useLibraryMarks() {
  const api = useSabroApi()
  const { isConfigured, isSignedIn } = useAuth()
  const { locale } = useI18n()
  const { fetchShelf } = useReaderShelf()
  const { label: grammarLabel } = useGrammar()

  const marks = useState<LibraryMark[]>('library-marks', () => [])
  const loaded = useState('library-marks-loaded', () => false)

  const signedIn = computed(() => isConfigured.value && isSignedIn.value)
  const ids = computed(() => new Set(marks.value.map(mark => markId(mark.book, mark.itemKey))))

  function set(next: LibraryMark[]) {
    // Sabro sends no label for a grammar table (its pages live in the hub): name it from its slug.
    marks.value = sortMarks(next).map(mark => (mark.book === 'grammar' ? { ...mark, label: grammarLabel(mark.itemKey) ?? mark.label } : mark))
    writeDevice(marks.value)
  }

  function patch(book: LibraryBook, itemKey: string, change: Partial<LibraryMark>) {
    set(marks.value.map(mark => (mark.book === book && mark.itemKey === itemKey ? { ...mark, ...change } : mark)))
  }

  async function send(mark: LibraryMark) {
    try {
      const kept = await api<ReaderMarkDto>(`/library/me/marks/${mark.book}/${encodeURIComponent(mark.itemKey)}`, {
        method: 'PUT',
        query: { language: locale.value },
        body: { markedAt: mark.markedAt },
      })
      patch(mark.book, mark.itemKey, { synced: true, markedAt: kept.markedAt, label: kept.label ?? mark.label })
    }
    catch {
      // An item since unpublished (404), the ceiling (409), or Sabro briefly away: the device keeps it.
    }
  }

  /** Reads the device, then Sabro for a signed-in reader, sending up whatever only the device had. */
  async function load() {
    if (import.meta.server) return
    const device = readDevice()
    set(device)
    loaded.value = true
    if (!signedIn.value) return

    let shelf: ReaderShelfDto
    try {
      shelf = await fetchShelf()
    }
    catch {
      return // Sabro unreachable: the device's marks stand.
    }

    const { merged, toSend } = mergeMarks(device, shelf.marks)
    set(merged)
    for (const mark of toSend) await send(mark)
  }

  const isMarked = (book: LibraryBook, itemKey: string) => ids.value.has(markId(book, itemKey))

  /** Marks an item, or puts back a mark just removed (keeping its date). */
  function mark(book: LibraryBook, itemKey: string, label: LibraryPlaceLabel | null, markedAt: string = new Date().toISOString()) {
    if (import.meta.server || isMarked(book, itemKey)) return
    const made: LibraryMark = { book, itemKey, markedAt, label, synced: false }
    set([...marks.value, made])
    if (signedIn.value) send(made)
  }

  function unmark(book: LibraryBook, itemKey: string) {
    if (import.meta.server || !isMarked(book, itemKey)) return
    set(marks.value.filter(m => !(m.book === book && m.itemKey === itemKey)))
    if (signedIn.value) {
      api(`/library/me/marks/${book}/${encodeURIComponent(itemKey)}`, { method: 'DELETE' }).catch(() => {})
    }
  }

  function toggle(book: LibraryBook, itemKey: string, label: LibraryPlaceLabel | null) {
    if (isMarked(book, itemKey)) unmark(book, itemKey)
    else mark(book, itemKey, label)
  }

  return { marks, loaded, signedIn, load, isMarked, mark, unmark, toggle }
}
