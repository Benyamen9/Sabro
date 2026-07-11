import type { ScriptVariant as ApiScriptVariant, ProfileExportDto, UpdateUserProfileRequest, UserProfileDto } from '~/types/api'
import type { ScriptVariant } from '~/composables/useScriptVariant'

// The API models the script variant in PascalCase; the frontend preference uses
// lowercase slugs (see useScriptVariant). Map between the two at the boundary.
const apiToLocalVariant: Record<ApiScriptVariant, ScriptVariant> = {
  Estrangela: 'estrangela',
  Serto: 'serto',
  Madnhaya: 'madnhaya',
}
const localToApiVariant: Record<ScriptVariant, ApiScriptVariant> = {
  estrangela: 'Estrangela',
  serto: 'Serto',
  madnhaya: 'Madnhaya',
}

/**
 * Ties the language + script-variant preferences to the signed-in user's
 * profile, so they follow the account across devices rather than living only
 * in this browser's cookies.
 *
 *  - `load()` (called once after sign-in) pulls /profile/me and applies the
 *    saved preferences, making the profile the source of truth for a signed-in
 *    user. The local cookies still drive the experience while signed out.
 *  - `persist()` write-throughs the current local choice to /profile/me. The
 *    switchers always update the cookie first (instant, offline-safe) and then
 *    call this; a failed PUT leaves the local cookie change intact.
 */
export function useProfile() {
  const { isConfigured, isSignedIn } = useAuth()
  const { locale, setLocale, locales } = useI18n()
  const { variant, set: setVariant } = useScriptVariant()
  const api = useSabroApi()

  const profile = useState<UserProfileDto | null>('sabro-profile', () => null)
  const loaded = useState<boolean>('sabro-profile-loaded', () => false)

  function availableLocales() {
    return (locales.value as Array<{ code: string }>).map(l => l.code)
  }

  async function applyFromProfile(dto: UserProfileDto) {
    const localVariant = apiToLocalVariant[dto.preferredScriptVariant]
    if (localVariant && localVariant !== variant.value) {
      setVariant(localVariant)
    }
    if (
      dto.preferredLanguage
      && dto.preferredLanguage !== locale.value
      && availableLocales().includes(dto.preferredLanguage)
    ) {
      await setLocale(dto.preferredLanguage as typeof locale.value)
    }
  }

  // Resolve the profile once per session and adopt its saved preferences.
  async function load() {
    if (!isConfigured.value || !isSignedIn.value || loaded.value) return
    try {
      const dto = await api<UserProfileDto>('/profile/me')
      profile.value = dto
      loaded.value = true
      await applyFromProfile(dto)
    }
    catch {
      // No profile / network error: keep the local cookie preferences as-is.
    }
  }

  // The full PUT body. The API treats an omitted displayName/showOnLeaderboard as
  // "clear it", so every write must carry the current account fields too — otherwise
  // switching language would silently wipe the user's display name and opt-out them.
  function buildPayload(overrides?: Partial<UpdateUserProfileRequest>): UpdateUserProfileRequest {
    return {
      preferredLanguage: locale.value as string,
      preferredScriptVariant: localToApiVariant[variant.value],
      displayName: profile.value?.displayName ?? null,
      showOnLeaderboard: profile.value?.showOnLeaderboard ?? false,
      ...overrides,
    }
  }

  // Save the current language + script variant to the profile, preserving the
  // account fields. No-op when signed out — the cookie alone covers the anonymous
  // case.
  async function persist() {
    if (!isConfigured.value || !isSignedIn.value) return
    try {
      profile.value = await api<UserProfileDto>('/profile/me', { method: 'PUT', body: buildPayload() })
    }
    catch {
      // The local cookie is already updated; tolerate a failed server sync.
    }
  }

  // Save the editable account fields (display name + leaderboard opt-in). Returns
  // true on success; false on a validation/network error so the caller can surface it.
  async function saveAccount(account: { displayName: string | null, showOnLeaderboard: boolean }): Promise<boolean> {
    if (!isConfigured.value || !isSignedIn.value) return false
    try {
      profile.value = await api<UserProfileDto>('/profile/me', {
        method: 'PUT',
        body: buildPayload(account),
      })
      return true
    }
    catch {
      return false
    }
  }

  // Download everything Sabro stores about the user as one JSON file (GDPR data
  // portability, GET /profile/me/export). Returns true on success; false on a
  // network/auth error so the caller can surface it.
  async function exportData(): Promise<boolean> {
    if (!isConfigured.value || !isSignedIn.value) return false
    try {
      const dto = await api<ProfileExportDto>('/profile/me/export')
      const blob = new Blob([JSON.stringify(dto, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `sabro-data-export-${new Date().toISOString().slice(0, 10)}.json`
      link.click()
      URL.revokeObjectURL(url)
      return true
    }
    catch {
      return false
    }
  }

  // Permanently delete the account: Sabro data + the Logto identity, server-side
  // (DELETE /profile/me). Returns true on success so the caller can sign out.
  async function deleteAccount(): Promise<boolean> {
    if (!isConfigured.value || !isSignedIn.value) return false
    try {
      await api('/profile/me', { method: 'DELETE' })
      return true
    }
    catch {
      return false
    }
  }

  return { profile, load, persist, saveAccount, exportData, deleteAccount }
}
