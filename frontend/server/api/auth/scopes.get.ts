/**
 * Returns the scopes carried in the caller's Logto access token for the
 * configured Sabro API resource, or `scopes: []` when the user is not
 * signed in / Logto is not configured. The frontend uses this to decide
 * whether to surface admin-only UI (the `api:v1:admin` scope) — but the
 * API itself remains the real authorization boundary; this is UX gating
 * only. The browser never sees the OIDC cookie or app secret, only the
 * derived scope list.
 */
import { defineEventHandler } from 'h3'

/**
 * Decodes the `scope` claim from a JWT without verifying its signature.
 * The token comes straight from the server-side Logto client, so it is
 * trusted here; we only need to read its claims, not re-validate them.
 */
function readScopes(accessToken: string): string[] {
  const payloadSegment = accessToken.split('.')[1]
  if (!payloadSegment) {
    return []
  }

  try {
    const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/')
    const json = Buffer.from(normalized, 'base64').toString('utf8')
    const claims = JSON.parse(json) as { scope?: unknown }
    if (typeof claims.scope !== 'string') {
      return []
    }
    return claims.scope.split(' ').filter(Boolean)
  }
  catch {
    return []
  }
}

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  const resource = config.public.sabroApiResource

  if (!config.public.logtoEndpoint || !resource) {
    return { scopes: [] as string[] }
  }

  // The Logto event-handler middleware populates event.context.logtoClient on
  // every non-(sign-in/out/callback) request. We read it directly here because
  // useLogtoClient() relies on Nuxt's SSR context, which doesn't exist in a
  // standalone Nitro server route.
  const client = event.context.logtoClient
  if (!client) {
    return { scopes: [] as string[] }
  }

  try {
    const accessToken = await client.getAccessToken(resource)
    return { scopes: accessToken ? readScopes(accessToken) : [] }
  }
  catch {
    return { scopes: [] as string[] }
  }
})
