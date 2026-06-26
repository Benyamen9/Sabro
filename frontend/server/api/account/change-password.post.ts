/**
 * Changes the signed-in user's password via Logto's Account API.
 *
 * Logto owns credentials, so this route is a thin server-side proxy: it never
 * exposes the OP token to the browser. The flow is Logto's required two step
 * "sensitive operation" dance:
 *   1. POST /api/verifications/password  — proves the user knows their CURRENT
 *      password and returns a short-lived verification record.
 *   2. POST /api/my-account/password     — sets the NEW password, authorised by
 *      that verification record (passed in the `logto-verification-id` header).
 *
 * Errors are mapped to stable `reason` codes the UI turns into i18n messages:
 *   - `current_invalid`  — the current password didn't verify.
 *   - `new_rejected`     — Logto's password policy rejected the new password.
 *   - `unauthenticated`  — no signed-in Logto session.
 *   - `unavailable`      — Logto not configured / unexpected failure.
 */
import { createError, defineEventHandler, readBody } from 'h3'

interface ChangePasswordBody {
  currentPassword?: string
  newPassword?: string
}

interface VerificationResponse {
  verificationRecordId?: string
}

function fail(reason: string, statusCode = 422): never {
  throw createError({ statusCode, data: { reason }, statusMessage: reason })
}

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  const endpoint = config.public.logtoEndpoint?.replace(/\/$/, '')
  if (!endpoint) fail('unavailable', 503)

  const client = event.context.logtoClient
  if (!client) fail('unauthenticated', 401)

  const body = await readBody<ChangePasswordBody>(event)
  const currentPassword = body?.currentPassword ?? ''
  const newPassword = body?.newPassword ?? ''
  if (!currentPassword || !newPassword) fail('new_rejected', 400)

  // Opaque access token for Logto's own API (the OP). No resource argument —
  // that is what distinguishes it from the Sabro-API-bound token used elsewhere.
  let token: string | undefined
  try {
    token = await client.getAccessToken()
  }
  catch {
    fail('unauthenticated', 401)
  }
  if (!token) fail('unauthenticated', 401)

  const authHeader = { Authorization: `Bearer ${token}` }

  // Step 1 — verify the current password.
  const verify = await $fetch.raw<VerificationResponse>(`${endpoint}/api/verifications/password`, {
    method: 'POST',
    headers: authHeader,
    body: { password: currentPassword },
    ignoreResponseError: true,
  })
  if (verify.status >= 400 || !verify._data?.verificationRecordId) {
    fail('current_invalid', 422)
  }

  // Step 2 — set the new password, authorised by the verification record.
  const update = await $fetch.raw(`${endpoint}/api/my-account/password`, {
    method: 'POST',
    headers: {
      ...authHeader,
      'logto-verification-id': verify._data.verificationRecordId!,
    },
    body: { password: newPassword },
    ignoreResponseError: true,
  })
  if (update.status >= 400) {
    // Most commonly Logto's password policy rejecting a weak/breached password.
    fail('new_rejected', 422)
  }

  return { ok: true }
})
