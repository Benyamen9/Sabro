/**
 * Sabro API client. Reads the base URL from runtimeConfig.public.apiBaseUrl
 * and attaches a Logto bearer token via useAuth().getAccessToken() when
 * available. Pages should still treat 401/403 as "Sign in required" so
 * unconfigured / signed-out states render correctly.
 */
export function useSabroApi() {
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiBaseUrl
  const { getAccessToken } = useAuth()

  return $fetch.create({
    baseURL: baseUrl,
    async onRequest({ options }) {
      options.headers = new Headers(options.headers)
      if (!options.headers.has('Accept')) {
        options.headers.set('Accept', 'application/json')
      }
      if (!options.headers.has('Authorization')) {
        const token = await getAccessToken()
        if (token) {
          options.headers.set('Authorization', `Bearer ${token}`)
        }
      }
    },
  })
}
