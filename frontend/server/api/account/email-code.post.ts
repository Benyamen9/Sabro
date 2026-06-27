/**
 * Step 1 of changing the primary email: send a verification code to the NEW
 * address via Logto's Account API. Returns the verification record references
 * the confirm step needs. Thin server-side proxy — the OP token never reaches
 * the browser.
 *
 * Reason codes:
 *   - `invalid_email`   — the address is malformed.
 *   - `send_failed`     — Logto could not issue/send the code.
 *   - `unauthenticated` — no signed-in Logto session.
 *   - `unavailable`     — Logto not configured / unexpected failure.
 */
import { createError, defineEventHandler, readBody } from 'h3'

interface RequestCodeBody {
  email?: string
}

interface VerificationCodeResponse {
  verificationRecordId?: string
  verificationId?: string
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function fail(reason: string, statusCode = 422): never {
  throw createError({ statusCode, data: { reason }, statusMessage: reason })
}

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  const endpoint = config.public.logtoEndpoint?.replace(/\/$/, '')
  if (!endpoint) fail('unavailable', 503)

  const client = event.context.logtoClient
  if (!client) fail('unauthenticated', 401)

  const body = await readBody<RequestCodeBody>(event)
  const email = (body?.email ?? '').trim().toLowerCase()
  if (!EMAIL_PATTERN.test(email)) fail('invalid_email', 400)

  let token: string | undefined
  try {
    token = await client.getAccessToken()
  }
  catch {
    fail('unauthenticated', 401)
  }
  if (!token) fail('unauthenticated', 401)

  const response = await $fetch.raw<VerificationCodeResponse>(`${endpoint}/api/verifications/verification-code`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: { identifier: { type: 'email', value: email } },
    ignoreResponseError: true,
  })
  if (response.status >= 400 || !response._data?.verificationRecordId || !response._data?.verificationId) {
    fail('send_failed', 422)
  }

  return {
    recordId: response._data.verificationRecordId,
    verificationId: response._data.verificationId,
  }
})
