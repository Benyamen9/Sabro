/**
 * Changes the signed-in user's username via Logto's Account API.
 *
 * Username is a non-sensitive profile field, so unlike password/email it needs
 * no verification record — a single PATCH on the account does it. Like the
 * other account routes this is a thin server-side proxy that never exposes the
 * OP token to the browser.
 *
 * Reason codes the UI maps to i18n messages:
 *   - `taken`           — the username is already in use.
 *   - `invalid`         — Logto rejected the value (format/length).
 *   - `unauthenticated` — no signed-in Logto session.
 *   - `unavailable`     — Logto not configured / unexpected failure.
 */
import { createError, defineEventHandler, readBody } from 'h3'

interface ChangeUsernameBody {
  username?: string
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

  const body = await readBody<ChangeUsernameBody>(event)
  const username = (body?.username ?? '').trim()
  if (!username) fail('invalid', 400)

  // Opaque access token for Logto's own API (the OP). No resource argument.
  let token: string | undefined
  try {
    token = await client.getAccessToken()
  }
  catch {
    fail('unauthenticated', 401)
  }
  if (!token) fail('unauthenticated', 401)

  const update = await $fetch.raw(`${endpoint}/api/my-account`, {
    method: 'PATCH',
    headers: { Authorization: `Bearer ${token}` },
    body: { username },
    ignoreResponseError: true,
  })
  if (update.status >= 400) {
    const code = (update._data as { code?: string })?.code ?? ''
    if (code.includes('username_already_in_use') || code.includes('username_in_use')) {
      fail('taken', 422)
    }
    fail('invalid', 422)
  }

  return { ok: true }
})
