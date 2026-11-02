import type { UserInfoResponse } from '@logto/node'
import { createTokenCache } from '~/utils/tokenCache'

// Browser only. On the server one module serves every visitor, so a token kept
// there would be handed to the next person's page.
let browserTokens: ReturnType<typeof createTokenCache> | null = null

async function fetchAccessToken(): Promise<string | null> {
  try {
    const result = await $fetch<{ accessToken: string | null }>('/api/auth/access-token')
    return result.accessToken
  }
  catch {
    return null
  }
}

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
    // Drop the ecosystem hint before leaving, so the other apps stop offering
    // to sign in an account the player is in the middle of leaving. Only the
    // sign-out path clears it — a merely signed-out app must never clear it,
    // or visiting one game anonymously would erase the hint for all of them.
    useEcosystemAccount().forget()
    forgetAccessToken()
    window.location.href = '/sign-out'
  }

  // In the browser the token is kept until shortly before it expires, so a page
  // that calls Sabro several times asks for it once (utils/tokenCache.ts).
  async function getAccessToken(): Promise<string | null> {
    if (!isConfigured.value || !isSignedIn.value) return null
    if (import.meta.server) return fetchAccessToken()
    browserTokens ??= createTokenCache(fetchAccessToken)
    return browserTokens.get()
  }

  /** Drop the kept token, so the next call fetches a new one. */
  function forgetAccessToken() {
    browserTokens?.clear()
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
    getAccessToken,
    forgetAccessToken,
  }
}
