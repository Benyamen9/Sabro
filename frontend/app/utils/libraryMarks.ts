import { isLibraryBook, type LibraryBook, type LibraryPlaceLabel } from '~/utils/libraryPlaces'

/**
 * The reader's ܀ marks (Owner, 2026-11-01): items marked by hand to find again, unlike the red
 * ribbon, which moves by itself. Kept on the device for everyone and on Sabro for a signed-in reader
 * (`/api/v1/library/me`). Pure logic only; the composable `useLibraryMarks` does the storage and
 * the calls.
 */
export interface LibraryMark {
  book: LibraryBook
  itemKey: string
  /** ISO time the mark was made. */
  markedAt: string
  label: LibraryPlaceLabel | null
  /**
   * Sabro holds this mark. A mark Sabro has held and no longer lists was removed on another device
   * (or its item unpublished), so the device lets it go; one Sabro never held was made signed out
   * and is sent up.
   */
  synced?: boolean
}

export interface ServerMark {
  book: string
  itemKey: string
  markedAt: string
  label?: { title: string, syriac?: string | null, subtitle?: string | null } | null
}

export const markId = (book: string, itemKey: string) => `${book}/${itemKey}`

/** Newest first, as the panel lists them. */
export function sortMarks(marks: LibraryMark[]): LibraryMark[] {
  return [...marks].sort((a, b) => Date.parse(b.markedAt) - Date.parse(a.markedAt))
}

/** Reads what the device stored, dropping anything malformed (storage is the reader's, not ours). */
export function parseStoredMarks(raw: string | null): LibraryMark[] {
  if (!raw) return []
  let parsed: unknown
  try {
    parsed = JSON.parse(raw)
  }
  catch {
    return []
  }
  if (!Array.isArray(parsed)) return []
  const seen = new Set<string>()
  const marks: LibraryMark[] = []
  for (const value of parsed) {
    if (!value || typeof value !== 'object') continue
    const mark = value as Partial<LibraryMark>
    if (typeof mark.book !== 'string' || !isLibraryBook(mark.book)) continue
    if (typeof mark.itemKey !== 'string' || !mark.itemKey) continue
    if (typeof mark.markedAt !== 'string' || Number.isNaN(Date.parse(mark.markedAt))) continue
    const id = markId(mark.book, mark.itemKey)
    if (seen.has(id)) continue
    seen.add(id)
    const label = mark.label && typeof mark.label.title === 'string' ? mark.label : null
    marks.push({ book: mark.book, itemKey: mark.itemKey, markedAt: mark.markedAt, label, synced: mark.synced === true })
  }
  return sortMarks(marks)
}

/**
 * Brings the device's marks and Sabro's together. Everything Sabro lists is kept, with its label
 * (the fresher name). A device mark Sabro does not list is sent up if Sabro never held it (made
 * while signed out) and dropped if it did (removed elsewhere since).
 */
export function mergeMarks(local: LibraryMark[], server: ServerMark[]): { merged: LibraryMark[], toSend: LibraryMark[] } {
  const mine = new Map(local.map(mark => [markId(mark.book, mark.itemKey), mark]))
  const merged: LibraryMark[] = []
  const listed = new Set<string>()

  for (const mark of server) {
    if (!isLibraryBook(mark.book)) continue
    const id = markId(mark.book, mark.itemKey)
    listed.add(id)
    merged.push({
      book: mark.book,
      itemKey: mark.itemKey,
      markedAt: mark.markedAt,
      label: mark.label ?? mine.get(id)?.label ?? null,
      synced: true,
    })
  }

  const toSend = local.filter(mark => !mark.synced && !listed.has(markId(mark.book, mark.itemKey)))
  merged.push(...toSend)
  return { merged: sortMarks(merged), toSend }
}

/** Where a marked item opens: a word and a grammar table have their own page, a chant and a figure open inside their book. */
export function markHref(mark: Pick<LibraryMark, 'book' | 'itemKey'>): string {
  const key = encodeURIComponent(mark.itemKey)
  if (mark.book === 'lexicon') return `/library/lexicon/${key}`
  if (mark.book === 'grammar') return `/library/grammar/${key}`
  if (mark.book === 'beth-gazo') return `/library/beth-gazo?chant=${key}`
  return `/library/beth-salme?figure=${key}`
}

/** Whole days between the mark and now, by the reader's own calendar: 0 is today, 1 yesterday. */
export function daysAgo(markedAt: string, now: Date = new Date()): number {
  const then = new Date(markedAt)
  const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
  return Math.round((startOfDay(now) - startOfDay(then)) / 86_400_000)
}
