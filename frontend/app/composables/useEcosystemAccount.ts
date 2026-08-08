/**
 * "Do you have a Sabro account?", shared across the ecosystem.
 *
 * Every app in the ecosystem is its OWN Logto OIDC application, with its own
 * app id, its own secret and its own encrypted session cookie. Signing in on
 * the hub therefore writes the hub's cookie and nothing else: `shmo.sabro.be`
 * genuinely does not know who you are, and `isSignedIn` there is honestly
 * false. That is the architecture working, not a bug — but it reads as one,
 * because the player did sign in, once, and has no idea the boundary exists.
 *
 * The cost is real rather than cosmetic. A signed-out game cannot record a
 * result and cannot see one recorded elsewhere, so the player silently loses
 * cross-device continuity: they finish a round on a phone, open the desktop,
 * and are invited to play the same day again. (Owner hit exactly this on
 * 2026-08-08.)
 *
 * This cookie is a HINT, never an authority:
 *
 *  - It says only "this browser has signed in to something in the ecosystem".
 *    It carries no identity, no token and no claim, so it is worthless to
 *    forge — the most it can do is make an app offer a sign-in button to
 *    someone who then fails to sign in.
 *  - Nothing may gate access on it. Authorization stays with the app's own
 *    Logto session and the API's own token validation, exactly as before.
 *
 * Deliberately NOT a silent `prompt=none` redirect, which is the other way to
 * solve this. `@logto/nuxt` calls `handleSignInCallback` with no try/catch, so
 * a silent attempt that fails — the normal case for a genuinely anonymous
 * visitor — throws and serves a 500 instead of the game. Making that safe means
 * taking over `/callback` in every repo, which puts live sign-in through an
 * untestable path. Owner chose this version on 2026-08-08.
 */

const cookieKey = 'sabro_account'

export function useEcosystemAccount() {
  // Empty in dev → host-only; ".sabro.be" in prod, which is what makes the
  // cookie visible to the games. Secure only when a domain is set, so http
  // localhost still works. Same treatment as the theme and locale cookies.
  const cookieDomain = useRuntimeConfig().public.cookieDomain

  const cookie = useCookie<string | null>(cookieKey, {
    default: () => null,
    maxAge: 60 * 60 * 24 * 365,
    sameSite: 'lax',
    domain: cookieDomain || undefined,
    secure: Boolean(cookieDomain),
    // Read by client-side code in the other apps, so it cannot be httpOnly.
    // Safe precisely because it carries nothing worth stealing.
    httpOnly: false,
  })

  /** Whether this browser has an account somewhere in the ecosystem. */
  const hasAccount = computed(() => cookie.value === '1')

  /** Called by the app the player actually signed in to. */
  function remember() {
    cookie.value = '1'
  }

  /**
   * Called on sign-out. Clears the hint everywhere, so the other apps stop
   * offering to sign in an account the player has just left.
   */
  function forget() {
    cookie.value = null
  }

  return { hasAccount, remember, forget }
}
