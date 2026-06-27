/**
 * Step 2 of changing the primary email via Logto's Account API. Takes the code
 * sent to the new address plus the current password, and runs Logto's required
 * three-call sequence:
 *   1. POST /api/verifications/verification-code/verify — proves the user owns
 *      the NEW email (the code), promoting its verification record.
 *   2. POST /api/verifications/password — proves identity with the CURRENT
 *      password, returning an identity verification record.
 *   3. POST /api/my-account/primary-email — sets the new email, authorised by
 *      the identity record (header) and the verified new-email record (body).
 *
 * Reason codes the UI maps to i18n messages:
 *   - `code_invalid`     — the emailed code didn't verify.
 *   - `password_invalid` — the current password didn't verify.
 *   - `email_taken`      — the new email is already in use.
 *   - `unauthenticated`  — no signed-in Logto session.
 *   - `unavailable`      — Logto not configured / unexpected failure.
 */
import { createError, defineEventHandler, readBody } from 'h3'

interface ConfirmBody {
  email?: string
  code?: string
  recordId?: string
  verificationId?: string
  currentPassword?: string
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

  const body = await readBody<ConfirmBody>(event)
  const email = (body?.email ?? '').trim().toLowerCase()
  const code = (body?.code ?? '').trim()
  const recordId = body?.recordId ?? ''
  const verificationId = body?.verificationId ?? ''
  const currentPassword = body?.currentPassword ?? ''
  if (!email || !code || !recordId || !verificationId || !currentPassword) fail('unavailable', 400)

  let token: string | undefined
  try {
    token = await client.getAccessToken()
  }
  catch {
    fail('unauthenticated', 401)
  }
  if (!token) fail('unauthenticated', 401)

  const authHeader = { Authorization: `Bearer ${token}` }

  // Step 1 — verify the code sent to the new email.
  const verify = await $fetch.raw(`${endpoint}/api/verifications/verification-code/verify`, {
    method: 'POST',
    headers: authHeader,
    body: { identifier: { type: 'email', value: email }, verificationId, code },
    ignoreResponseError: true,
  })
  if (verify.status >= 400) fail('code_invalid', 422)

  // Step 2 — prove identity with the current password.
  const password = await $fetch.raw<VerificationResponse>(`${endpoint}/api/verifications/password`, {
    method: 'POST',
    headers: authHeader,
    body: { password: currentPassword },
    ignoreResponseError: true,
  })
  if (password.status >= 400 || !password._data?.verificationRecordId) fail('password_invalid', 422)

  // Step 3 — set the new primary email.
  const update = await $fetch.raw(`${endpoint}/api/my-account/primary-email`, {
    method: 'POST',
    headers: {
      ...authHeader,
      'logto-verification-id': password._data.verificationRecordId!,
    },
    body: { email, newIdentifierVerificationRecordId: recordId },
    ignoreResponseError: true,
  })
  if (update.status >= 400) {
    const errorCode = (update._data as { code?: string })?.code ?? ''
    if (errorCode.includes('email_already_in_use') || errorCode.includes('email_in_use')) {
      fail('email_taken', 422)
    }
    fail('unavailable', 422)
  }

  return { ok: true }
})
