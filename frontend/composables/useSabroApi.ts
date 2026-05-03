export function useSabroApi() {
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiBaseUrl

  return $fetch.create({
    baseURL: baseUrl,
    onRequest({ options }) {
      options.headers = new Headers(options.headers)
      if (!options.headers.has('Accept')) {
        options.headers.set('Accept', 'application/json')
      }
    },
  })
}
