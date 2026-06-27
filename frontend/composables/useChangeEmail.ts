/**
 * Client-side primary-email-change helper. Two steps mirroring Logto's Account
 * API: request a code to the new address, then confirm with that code + the
 * current password. The server routes proxy Logto; this never sees a token.
 */

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function isValidEmail(email: string): boolean {
  return EMAIL_PATTERN.test(email.trim())
}

export interface ConfirmEmailInput {
  email: string
  code: string
  recordId: string
  verificationId: string
  currentPassword: string
}

export function useChangeEmail() {
  const { isConfigured, isSignedIn } = useAuth()

  const submitting = useState<boolean>('sabro-change-email-submitting', () => false)

  /** Step 1: send a verification code to the new email. */
  async function requestCode(email: string): Promise<{ ok: boolean, recordId?: string, verificationId?: string, reason?: string }> {
    if (!isConfigured.value || !isSignedIn.value) return { ok: false, reason: 'unauthenticated' }
    if (!isValidEmail(email)) return { ok: false, reason: 'invalid_email' }

    submitting.value = true
    try {
      const result = await $fetch<{ recordId: string, verificationId: string }>('/api/account/email-code', {
        method: 'POST',
        body: { email: email.trim() },
      })
      return { ok: true, recordId: result.recordId, verificationId: result.verificationId }
    }
    catch (error: unknown) {
      const reason = (error as { data?: { data?: { reason?: string } } })?.data?.data?.reason
      return { ok: false, reason: reason ?? 'unavailable' }
    }
    finally {
      submitting.value = false
    }
  }

  /** Step 2: confirm the code + current password and set the new email. */
  async function confirmEmail(input: ConfirmEmailInput): Promise<{ ok: boolean, reason?: string }> {
    if (!isConfigured.value || !isSignedIn.value) return { ok: false, reason: 'unauthenticated' }

    submitting.value = true
    try {
      await $fetch('/api/account/email', {
        method: 'POST',
        body: {
          email: input.email.trim(),
          code: input.code.trim(),
          recordId: input.recordId,
          verificationId: input.verificationId,
          currentPassword: input.currentPassword,
        },
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

  return { submitting, requestCode, confirmEmail }
}
