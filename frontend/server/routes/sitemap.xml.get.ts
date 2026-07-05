// Sitemap for the public surface: the home page, the Meltho word library, and
// one URL per library word (the long-tail entry points). Word URLs come from
// the same public API the pages render from; if the API is unreachable the
// static URLs still ship rather than failing the whole sitemap. Cached for an
// hour — the library gains at most one word per day.
interface LibraryItem { lexiconEntryId: string, lastPlayedOn?: string }
interface LibraryPage { items: LibraryItem[], total: number, page: number, pageSize: number }

export default defineCachedEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const siteUrl = config.public.siteUrl.replace(/\/$/, '')
  const apiBaseUrl = config.public.apiBaseUrl.replace(/\/$/, '')

  const urls: { loc: string, lastmod?: string }[] = [
    { loc: `${siteUrl}/` },
    { loc: `${siteUrl}/library` },
  ]

  try {
    let page = 1
    for (;;) {
      const res = await $fetch<LibraryPage>(`${apiBaseUrl}/play/meltho/library`, {
        query: { page, pageSize: 100 },
      })
      for (const item of res.items) {
        urls.push({
          loc: `${siteUrl}/library/${item.lexiconEntryId}`,
          lastmod: item.lastPlayedOn,
        })
      }
      if (res.page * res.pageSize >= res.total || res.items.length === 0) break
      page += 1
    }
  }
  catch {
    // API unavailable — serve the static URLs; the next cache refresh retries.
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
