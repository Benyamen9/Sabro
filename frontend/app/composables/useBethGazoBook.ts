import type { BethGazoBookDto } from '~/types/api'

/**
 * The whole Beth Gazo for the hub's book page, rendered on the server: every
 * section with its recorded chants and every mode with its text. Public and
 * anonymous, like the figure roster.
 */
export function useBethGazoBook() {
  const api = useSabroApi()
  const book = useAsyncData('beth-gazo-book', () => api<BethGazoBookDto>('/chants/book'))

  return {
    book: book.data,
    pending: book.pending,
    failed: computed(() => Boolean(book.error.value)),
  }
}
