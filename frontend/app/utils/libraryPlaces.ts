/**
 * The red ribbon (Owner, 2026-11-01): one place per book, the last item opened there, placed
 * automatically. Kept on the device for everyone and on Sabro for a signed-in reader
 * (`/api/v1/library/me`), where the most recently opened place wins. Pure logic only; the
 * composable `useLibraryPlaces` does the storage and the calls.
 */

/** The books on the shelf, by their address under /library/. Grammar joins when it has pages. */
export const LIBRARY_BOOKS = ['lexicon', 'beth-gazo', 'beth-salme'] as const
export type LibraryBook = (typeof LIBRARY_BOOKS)[number]

/** How a place is named on its cover: what Sabro sends, or what the page knew when it was opened. */
export interface LibraryPlaceLabel {
  title: string
  syriac?: string | null
  subtitle?: string | null
}

export interface LibraryPlace {
  itemKey: string
  /** ISO time the item was opened. */
  openedAt: string
  label: LibraryPlaceLabel | null
}

export type LibraryPlaces = Partial<Record<LibraryBook, LibraryPlace>>

export function isLibraryBook(value: string): value is LibraryBook {
  return (LIBRARY_BOOKS as readonly string[]).includes(value)
}

/** Reads what the device stored, dropping anything malformed (storage is the reader's, not ours). */
export function parseStoredPlaces(raw: string | null): LibraryPlaces {
  if (!raw) return {}
  let parsed: unknown
  try {
    parsed = JSON.parse(raw)
  }
  catch {
    return {}
  }
  if (!parsed || typeof parsed !== 'object') return {}
  const places: LibraryPlaces = {}
  for (const [book, value] of Object.entries(parsed as Record<string, unknown>)) {
    if (!isLibraryBook(book) || !value || typeof value !== 'object') continue
    const place = value as Partial<LibraryPlace>
    if (typeof place.itemKey !== 'string' || !place.itemKey || typeof place.openedAt !== 'string' || Number.isNaN(Date.parse(place.openedAt))) continue
    const label = place.label && typeof place.label.title === 'string' ? place.label : null
    places[book] = { itemKey: place.itemKey, openedAt: place.openedAt, label }
  }
  return places
}

export interface ServerPlace {
  book: string
  itemKey: string
  openedAt: string
  label?: { title: string, syriac?: string | null, subtitle?: string | null } | null
}

/**
 * Brings the device's places and Sabro's together: per book the most recently opened wins, as on the
 * server. Returns the merged places and the device places Sabro should be sent (newer than what it
 * holds, or missing there: kept while signed out, or never sent).
 */
export function mergePlaces(local: LibraryPlaces, server: ServerPlace[]): { merged: LibraryPlaces, toSend: { book: LibraryBook, place: LibraryPlace }[] } {
  const merged: LibraryPlaces = {}
  const toSend: { book: LibraryBook, place: LibraryPlace }[] = []
  const fromServer = new Map<LibraryBook, LibraryPlace>()
  for (const place of server) {
    if (!isLibraryBook(place.book)) continue
    fromServer.set(place.book, { itemKey: place.itemKey, openedAt: place.openedAt, label: place.label ?? null })
  }

  for (const book of LIBRARY_BOOKS) {
    const mine = local[book]
    const theirs = fromServer.get(book)
    if (mine && (!theirs || Date.parse(mine.openedAt) > Date.parse(theirs.openedAt))) {
      merged[book] = mine.itemKey === theirs?.itemKey ? { ...mine, label: theirs.label ?? mine.label } : mine
      toSend.push({ book, place: mine })
    }
    else if (theirs) {
      // Sabro's label is the fresher name; keep the device's only if Sabro has none.
      merged[book] = { ...theirs, label: theirs.label ?? (mine?.itemKey === theirs.itemKey ? mine.label : null) }
    }
  }
  return { merged, toSend }
}
