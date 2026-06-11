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

  // The Logto event-handler middleware populates event.context.logtoClient on
  // every non-(sign-in/out/callback) request. We read it directly here because
  // useLogtoClient() relies on Nuxt's SSR context, which doesn't exist in a
  // standalone Nitro server route.
  const client = event.context.logtoClient
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
