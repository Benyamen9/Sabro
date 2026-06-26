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

  // Display-friendly identity claims, resolved once here so the header menu and
  // the profile page render the same values. Logto is the single source of
  // truth for name/email/avatar (we never mirror them locally), so anything the
  // token doesn't carry is simply absent and the UI omits it.
  const displayName = computed(() => {
    const u = user.value
    if (!u) return ''
    return (u.name as string) || (u.username as string) || (u.email as string) || (u.sub as string) || ''
  })
  const email = computed(() => (user.value?.email as string) || '')
  const username = computed(() => (user.value?.username as string) || '')
  const avatarUrl = computed(() => (user.value?.picture as string) || '')

  // First character of the best available label, for the fallback avatar tile.
  const initial = computed(() => {
    const source = displayName.value.trim()
    return source ? source.charAt(0).toUpperCase() : '?'
  })

  function signIn() {
    if (!isConfigured.value) return
    window.location.href = '/sign-in'
  }

  function signOut() {
    if (!isConfigured.value) return
    window.location.href = '/sign-out'
  }

  // Hands the user off to Logto's hosted Account Center to change their email,
  // password, etc. Logto owns credentials — Sabro never writes them — so this is
  // a redirect, not an in-app form. `redirect` brings the user back to /profile.
  function manageAccount() {
    if (!isConfigured.value) return
    const endpoint = config.public.logtoEndpoint.replace(/\/$/, '')
    const redirect = encodeURIComponent(window.location.href)
    window.location.href = `${endpoint}/account?redirect=${redirect}`
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

  return {
    isConfigured,
    isSignedIn,
    user,
    displayName,
    email,
    username,
    avatarUrl,
    initial,
    signIn,
    signOut,
    manageAccount,
    getAccessToken,
  }
}
