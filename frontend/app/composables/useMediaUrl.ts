/**
 * Resolves a relative media path (e.g. "/media/pronunciations/{id}.mp3", as
 * returned by the API) against the API's origin — media is served by
 * Sabro.API's static file middleware, not the frontend's own origin, and
 * apiBaseUrl carries the "/api/v1" suffix that must be stripped first.
 */
export function useMediaUrl() {
  const config = useRuntimeConfig()
  const origin = config.public.apiBaseUrl.replace(/\/api\/v\d+\/?$/, '')

  return (path: string) => `${origin}${path}`
}
