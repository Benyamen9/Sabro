/**
 * Two jobs, both driven by the `sabro_account` hint shared across *.sabro.be
 * (see useEcosystemAccount for what the hint is and is not):
 *
 * 1. RECORD it. Only ever SETS. A signed-out app must not clear the hint: each
 *    app has its own Logto session, so "signed out here" says nothing about the
 *    account, and clearing on that basis would erase the hint the moment a
 *    player opened one game anonymously. Only an explicit sign-out clears it
 *    (see useAuth).
 *
 * 2. FOLLOW it (owner, 2026-10-12: "if I log in in any app, I'm logged in in
 *    every other"). When this browser has an account somewhere but this app is
 *    signed out, send it through the ordinary /sign-in once. Logto already holds
 *    the session from the other app, so the round trip comes straight back
 *    signed in — no password, no click.
 *
 *    This is NOT the silent `prompt=none` redirect the owner turned down on
 *    2026-08-08: that path makes a failed attempt throw in `@logto/nuxt`'s
 *    callback and serve a 500. This is the normal interactive sign-in, so the
 *    only failure mode is visible and harmless: if the Logto session has
 *    expired, the player sees the sign-in page.
 *
 *    That is also why it is rationed — at most one attempt per app per
 *    browser per day, stored here and checked BEFORE leaving. A player who
 *    backs out of the sign-in page is not sent there again on every page load,
 *    and a broken sign-in can never become a redirect loop.
 */
const attemptKey = 'sabro_auto_sign_in_at'
const attemptEveryMs = 24 * 60 * 60 * 1000

function recentlyAttempted(): boolean {
  try {
    const at = Number(localStorage.getItem(attemptKey))
    return Number.isFinite(at) && Date.now() - at < attemptEveryMs
  }
  catch {
    // Storage blocked (private mode, site data refused): without a way to
    // ration the attempt, do not make one at all.
    return true
  }
}

/** Once signed in, the ration is spent on nothing: clear it, so a later
 *  sign-out-then-sign-in elsewhere is followed straight away. */
function clearAttempt() {
  try {
    localStorage.removeItem(attemptKey)
  }
  catch {
    // Nothing to clear if storage is blocked.
  }
}

function markAttempted(): boolean {
  try {
    localStorage.setItem(attemptKey, String(Date.now()))
    return true
  }
  catch {
    return false
  }
}

export default defineNuxtPlugin(() => {
  const { isConfigured, isSignedIn, signIn } = useAuth()
  const { hasAccount, remember } = useEcosystemAccount()

  watch(isSignedIn, (signedIn) => {
    if (signedIn) {
      remember()
      clearAttempt()
      return
    }
    if (isConfigured.value && hasAccount.value && !recentlyAttempted() && markAttempted()) signIn()
  }, { immediate: true })
})
