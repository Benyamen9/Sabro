/**
 * Reports account capabilities the profile UI needs to adapt itself — currently
 * just whether the user has a password. Email and password changes both prove
 * identity with the current password, so a social-only account (no password)
 * can't use them; the UI hides those cards when `hasPassword` is false.
 *
 * Reads Logto's Account API server-side (the OP token never reaches the
 * browser). Returns `hasPassword: null` when unknown so the UI defaults to
 * "capable" and never wrongly hides the cards for a password user.
 */
import { defineEventHandler } from 'h3'

interface MyAccountResponse {
  hasPassword?: boolean
}

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  const endpoint = config.public.logtoEndpoint?.replace(/\/$/, '')
  const client = event.context.logtoClient
  if (!endpoint || !client) return { hasPassword: null }

  let token: string | undefined
  try {
    token = await client.getAccessToken()
  }
  catch {
    return { hasPassword: null }
  }
  if (!token) return { hasPassword: null }

  const response = await $fetch.raw<MyAccountResponse>(`${endpoint}/api/my-account`, {
    headers: { Authorization: `Bearer ${token}` },
    ignoreResponseError: true,
  })
  if (response.status >= 400) return { hasPassword: null }

  const hasPassword = response._data?.hasPassword
  return { hasPassword: typeof hasPassword === 'boolean' ? hasPassword : null }
})
