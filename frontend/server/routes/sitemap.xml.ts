// Sitemap for the public surface: the home page, the word library, and one
// URL per published word (the long-tail entry points). The dictionary list
// supplies every word; the Meltho library list supplies lastmod for the words
// the game has shown (and any served word that later left the dictionary).
// Both come from the same public API the pages render from; if the API is
// unreachable the static URLs still ship rather than failing the whole
// sitemap. Cached for an hour — the pools grow slowly.
interface LibraryItem { lexiconEntryId: string, lastPlayedOn?: string }
interface LibraryPage { items: LibraryItem[], total: number, page: number, pageSize: number }
interface DictionaryItem { id: string }
interface DictionaryPage { items: DictionaryItem[], total: number, page: number, pageSize: number }

export default defineCachedEventHandler(async (event) => {
  // Method-agnostic route (no .get suffix) so HEAD works too — search-engine
  // validators probe sitemaps with HEAD before fetching the body, and a
  // method-suffixed route 404s those probes.
  const config = useRuntimeConfig()
  const siteUrl = config.public.siteUrl.replace(/\/$/, '')
  const apiBaseUrl = config.public.apiBaseUrl.replace(/\/$/, '')

  const urls: { loc: string, lastmod?: string }[] = [
    { loc: `${siteUrl}/` },
    { loc: `${siteUrl}/library` },
    { loc: `${siteUrl}/privacy` },
  ]

  // Word id → lastmod (only known for words Meltho has shown).
  const words = new Map<string, string | undefined>()

  try {
    let page = 1
    for (;;) {
      const res = await $fetch<DictionaryPage>(`${apiBaseUrl}/dictionary`, {
        query: { page, pageSize: 100 },
      })
      for (const item of res.items) {
        words.set(item.id, undefined)
      }
      if (res.page * res.pageSize >= res.total || res.items.length === 0) break
      page += 1
    }
  }
  catch {
    // API unavailable — fall through; the played list below may still respond.
  }

  try {
    let page = 1
    for (;;) {
      const res = await $fetch<LibraryPage>(`${apiBaseUrl}/play/meltho/library`, {
        query: { page, pageSize: 100 },
      })
      for (const item of res.items) {
        words.set(item.lexiconEntryId, item.lastPlayedOn)
      }
      if (res.page * res.pageSize >= res.total || res.items.length === 0) break
      page += 1
    }
  }
  catch {
    // API unavailable — serve what we have; the next cache refresh retries.
  }

  for (const [id, lastmod] of words) {
    urls.push({ loc: `${siteUrl}/library/${id}`, lastmod })
  }

  setHeader(event, 'content-type', 'application/xml; charset=utf-8')
  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${urls
  .map(u => `  <url><loc>${u.loc}</loc>${u.lastmod ? `<lastmod>${u.lastmod}</lastmod>` : ''}</url>`)
  .join('\n')}
</urlset>
`
}, { maxAge: 3600, name: 'sitemap' })
