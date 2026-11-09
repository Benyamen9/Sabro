import { libraryRedirect } from '../../app/utils/libraryRedirect'

/** Permanent redirects from the addresses the Library had before its shelf (see libraryRedirect). */
export default defineEventHandler((event) => {
  if (event.method !== 'GET' && event.method !== 'HEAD') return
  const url = getRequestURL(event)
  const target = libraryRedirect(url.pathname, url.search)
  if (target) return sendRedirect(event, target, 301)
})
