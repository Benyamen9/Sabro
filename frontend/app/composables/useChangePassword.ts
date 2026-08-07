/**
 * Client-side password-change helper. Does cheap local validation first (so we
 * don't round-trip obvious mistakes), then posts to the Sabro server route that
 * proxies Logto's Account API. The actual credential change happens in Logto;
 * this never sees a token.
 */

export type PasswordChangeError
  = | 'required'
    | 'tooShort'
    | 'mismatch'
    | 'sameAsCurrent'

/** Logto's default minimum password length. */
export const MIN_PASSWORD_LENGTH = 8

export interface PasswordChangeInput {
  current: string
  next: string
  confirm: string
}

/**
 * Pure local validation, returns the first failing rule or `null` when the
 * input is well-formed. Server/Logto still has the final say on policy.
 */
export function validatePasswordChange(input: PasswordChangeInput): PasswordChangeError | null {
  const { current, next, confirm } = input
  if (!current || !next || !confirm) return 'required'
  if (next.length < MIN_PASSWORD_LENGTH) return 'tooShort'
  if (next !== confirm) return 'mismatch'
  if (next === current) return 'sameAsCurrent'
  return null
}

export function useChangePassword() {
  const { isConfigured, isSignedIn } = useAuth()

  const submitting = useState<boolean>('sabro-change-password-submitting', () => false)

  /**
   * Attempts the change. Returns `{ ok: true }` or `{ ok: false, reason }`
   * where `reason` is either a local validation code or a server `reason`.
   */
  async function changePassword(input: PasswordChangeInput): Promise<{ ok: boolean, reason?: string }> {
    if (!isConfigured.value || !isSignedIn.value) return { ok: false, reason: 'unauthenticated' }

    const localError = validatePasswordChange(input)
    if (localError) return { ok: false, reason: localError }

    submitting.value = true
    try {
      await $fetch('/api/account/change-password', {
        method: 'POST',
        body: { currentPassword: input.current, newPassword: input.next },
      })
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

  return { submitting, changePassword }
}
