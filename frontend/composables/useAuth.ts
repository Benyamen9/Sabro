import type { UserInfoResponse } from '@logto/node'

/**
 * Thin wrapper over the @logto/nuxt module that:
 *  - exposes `isConfigured` so the UI can detect a missing Logto endpoint
 *    and degrade gracefully (no sign-in button instead of a broken one).
 *  - exposes `isSignedIn` + `user` for header rendering.
 *  - provides `signIn()` / `signOut()` that drive the module-registered
 *    /sign-in and /sign-out endpoints.
 *  - provides `getAccessToken()` which calls the Sabro-owned server route
 *    that asks the Logto client (server-only) for a fresh token bound to
 *    the configured API resource. The browser never sees the cookie or
 *    secret; only the bearer.
 */
export function useAuth() {
  const config = useRuntimeConfig()
  const logtoUser = useLogtoUser() as UserInfoResponse | undefined

  const isConfigured = computed(() => config.public.logtoEndpoint.length > 0)
  const isSignedIn = computed(() => Boolean(logtoUser))
  const user = computed(() => logtoUser)

  function signIn() {
    if (!isConfigured.value) return
    window.location.href = '/sign-in'
  }

  function signOut() {
    if (!isConfigured.value) return
    window.location.href = '/sign-out'
  }

  async function getAccessToken(): Promise<string | null> {
    if (!isConfigured.value || !isSignedIn.value) return null
    try {
      const result = await $fetch<{ accessToken: string | null }>(
        '/api/auth/access-token',
      )
      return result.accessToken
    }
    catch {
      return null
    }
  }

  return { isConfigured, isSignedIn, user, signIn, signOut, getAccessToken }
}
