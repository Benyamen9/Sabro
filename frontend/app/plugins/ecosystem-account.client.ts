/**
 * Records, in a cookie shared across *.sabro.be, that this browser has an
 * account somewhere in the ecosystem. See useEcosystemAccount for why.
 *
 * Only ever SETS. A signed-out app must not clear the hint: each app has its
 * own Logto session, so "signed out here" says nothing about the account, and
 * clearing on that basis would erase the hint the moment a player opened one
 * game anonymously. Only an explicit sign-out clears it (see useAuth).
 */
export default defineNuxtPlugin(() => {
  const { isSignedIn } = useAuth()
  const { remember } = useEcosystemAccount()

  watch(isSignedIn, (signedIn) => {
    if (signedIn) remember()
  }, { immediate: true })
})
