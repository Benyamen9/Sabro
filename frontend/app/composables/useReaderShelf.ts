import type { ReaderShelfDto } from '~/types/api'

/** One answer serves the ribbon and the marks when a page loads both. */
const FRESH_FOR_MS = 5000
let inFlight: { at: number, language: string, promise: Promise<ReaderShelfDto> } | null = null

/**
 * The signed-in reader's shelf from Sabro (`GET /library/me`): places and marks in one answer. The
 * ribbon (`useLibraryPlaces`) and the marks (`useLibraryMarks`) both load on the same pages, so a
 * call made moments ago is shared instead of repeated. Browser only.
 */
export function useReaderShelf() {
  const api = useSabroApi()
  const { locale } = useI18n()

  function fetchShelf(): Promise<ReaderShelfDto> {
    const language = locale.value
    if (inFlight && inFlight.language === language && Date.now() - inFlight.at < FRESH_FOR_MS) return inFlight.promise
    const promise = api<ReaderShelfDto>('/library/me', { query: { language } })
    inFlight = { at: Date.now(), language, promise }
    promise.catch(() => {
      if (inFlight?.promise === promise) inFlight = null
    })
    return promise
  }

  return { fetchShelf }
}
