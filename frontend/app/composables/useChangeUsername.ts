/**
 * Client-side username-change helper. Validates the format locally (so obvious
 * mistakes don't round-trip), then posts to the Sabro server route that proxies
 * Logto's Account API. The change happens in Logto; this never sees a token.
 */

export type UsernameChangeError
  = | 'required'
    | 'invalid'
    | 'unchanged'

// Logto usernames start with a letter or underscore, then letters/digits/_.
const USERNAME_PATTERN = /^[A-Za-z_]\w*$/

export function validateUsername(next: string, current: string): UsernameChangeError | null {
  const value = next.trim()
  if (!value) return 'required'
  if (!USERNAME_PATTERN.test(value)) return 'invalid'
  if (current && value === current) return 'unchanged'
  return null
}

export function useChangeUsername() {
  const { isConfigured, isSignedIn } = useAuth()

  const submitting = useState<boolean>('sabro-change-username-submitting', () => false)

  /**
   * Attempts the change. Returns `{ ok: true }` or `{ ok: false, reason }`
   * where `reason` is a local validation code or a server `reason`.
   */
  async function changeUsername(next: string): Promise<{ ok: boolean, reason?: string }> {
    if (!isConfigured.value || !isSignedIn.value) return { ok: false, reason: 'unauthenticated' }

    submitting.value = true
    try {
      await $fetch('/api/account/username', { method: 'POST', body: { username: next.trim() } })
      return { ok: true }
    }
    catch (error: unknown) {
      const reason = (error as { data?: { data?: { reason?: string } } })?.data?.data?.reason
      return { ok: false, reason: reason ?? 'unavailable' }
    }
    finally {
      submitting.value = false
    }
  }

  return { submitting, changeUsername }
}
