/**
 * Returns the Logto access token for the configured Sabro API resource,
 * or `accessToken: null` when the user is not signed in / Logto is not
 * configured. Used by useAuth().getAccessToken() so the browser can
 * attach a bearer to /api/v1/* requests without ever seeing the OIDC
 * cookie or app secret.
 */
import { defineEventHandler } from 'h3'

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  const resource = config.public.sabroApiResource

  if (!config.public.logtoEndpoint || !resource) {
    return { accessToken: null }
  }

  const client = useLogtoClient()
  if (!client) {
    return { accessToken: null }
  }

  try {
    const accessToken = await client.getAccessToken(resource)
    return { accessToken: accessToken ?? null }
  }
  catch {
    return { accessToken: null }
  }
})
