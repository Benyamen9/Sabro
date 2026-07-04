/**
 * Locale-aware sign-in. Replaces the module-registered /sign-in route (parked
 * at /logto/sign-in in nuxt.config) so the redirect to Logto can carry the
 * user's chosen interface language: the OIDC ui_locales parameter, read from
 * the shared sabro_locale cookie, makes the Logto sign-in pages render in the
 * same language as the app — for signed-out users the cookie is the only
 * source of that choice. Omitted when the cookie is absent, so Logto falls
 * back to its own browser-language detection, exactly as before.
 */
import { defineEventHandler, getCookie, getRequestURL, sendRedirect } from 'h3'

// The locales the app ships (see the i18n block in nuxt.config). A cookie
// value outside this set (stale or hand-edited) is simply not forwarded.
const appLocales = ['en', 'fr', 'nl']

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)

  // The Logto event-handler middleware populates event.context.logtoClient on
  // every non-(sign-in/out/callback) request; see server/api/auth for the same
  // pattern. When Logto isn't configured there is nothing to sign in to.
  const client = event.context.logtoClient
  if (!config.public.logtoEndpoint || !client) {
    return sendRedirect(event, '/', 302)
  }

  const locale = getCookie(event, 'sabro_locale')
  await client.signIn({
    // Mirrors the module's own handler: the callback URL is derived from the
    // request URL, and the /callback path is still handled by the module.
    redirectUri: new URL('/callback', getRequestURL(event)).href,
    ...(locale && appLocales.includes(locale)
      ? { extraParams: { ui_locales: locale } }
      : {}),
  })
})
