/**
 * Keeps the Sabro access token in the browser until shortly before it expires
 * (owner, 2026-11-02: "how can we accelerate all these loadings?"). Without it,
 * every API call first asked the app's own server for a token, one round trip
 * each, often several in a row while a page loads. Calls made at the same time
 * share one fetch. A missing token is never kept, so signing in takes effect on
 * the next call.
 *
 * The same file is in all six frontends (hub, Meltho, Mno, Shmo, Nahlo, Sofro);
 * change them together.
 */

/** Fetch a new token this long before the old one expires. */
export const TOKEN_MARGIN_MS = 60_000

/** The token's expiry (`exp`, in ms), or null when it cannot be read. */
export function tokenExpiry(token: string): number | null {
  const payload = token.split('.')[1]
  if (!payload) return null
  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = JSON.parse(atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')))
    return typeof json.exp === 'number' ? json.exp * 1000 : null
  }
  catch {
    return null
  }
}

export function createTokenCache(
  fetchToken: () => Promise<string | null>,
  now: () => number = Date.now,
) {
  let kept: { token: string, expiresAt: number } | null = null
  let inFlight: Promise<string | null> | null = null

  function get(): Promise<string | null> {
    if (kept && now() < kept.expiresAt - TOKEN_MARGIN_MS) return Promise.resolve(kept.token)
    if (inFlight) return inFlight
    inFlight = fetchToken()
      .then((token) => {
        const expiresAt = token ? tokenExpiry(token) : null
        kept = token && expiresAt ? { token, expiresAt } : null
        return token
      })
      .finally(() => { inFlight = null })
    return inFlight
  }

  /** Forget the token: on sign-out, or when Sabro refuses it. */
  function clear() {
    kept = null
  }

  return { get, clear }
}
