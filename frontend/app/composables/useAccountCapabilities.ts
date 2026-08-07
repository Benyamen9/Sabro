/**
 * Whether the signed-in account can use password-gated operations (changing the
 * password and — since it needs the current password as identity proof —
 * changing the email). Defaults to `true` so a password user never sees those
 * cards flicker out; flips to `false` only once Logto confirms no password
 * (e.g. a Google/GitHub-only account).
 */
export function useAccountCapabilities() {
  const { isConfigured, isSignedIn } = useAuth()

  const hasPassword = useState<boolean>('sabro-account-has-password', () => true)
  const loaded = useState<boolean>('sabro-account-capabilities-loaded', () => false)

  async function load() {
    if (!isConfigured.value || !isSignedIn.value || loaded.value) return
    try {
      const result = await $fetch<{ hasPassword: boolean | null }>('/api/account/capabilities')
      if (result.hasPassword === false) hasPassword.value = false
      loaded.value = true
    }
    catch {
      // Keep the optimistic default; better to show the cards than hide them wrongly.
    }
  }

  return { hasPassword, load }
}
