/**
 * Where an address from before the Library shelf (2026-11-09) now lives. The three books moved under
 * /library/ (Owner, 2026-11-01): Meltho links /library/<word id>, Shmo links /beth-salme, and search
 * engines know /library?q=… and the old pages, so each keeps working through a permanent redirect.
 *
 * Returns the new path (with the query string kept), or null when the address is current.
 */
export function libraryRedirect(path: string, search: string): string | null {
  const trimmed = path.length > 1 ? path.replace(/\/+$/, '') : path
  if (trimmed === '/beth-gazo') return `/library/beth-gazo${search}`
  if (trimmed === '/beth-salme') return `/library/beth-salme${search}`

  // A word page: /library/<uuid> is now /library/lexicon/<uuid>.
  const word = /^\/library\/([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$/i.exec(trimmed)
  if (word) return `/library/lexicon/${word[1]!.toLowerCase()}${search}`

  // The word list's own filters (?view=meltho) and the site search (?q=) belonged to the old /library.
  if (trimmed === '/library' && /[?&](q|view)=/.test(search)) return `/library/lexicon${search}`

  return null
}
