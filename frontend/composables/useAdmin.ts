/**
 * Resolves whether the current user holds the `api:v1:admin` scope, which
 * gates the editorial backoffice. The decision is derived server-side from
 * the Logto access token (see server/api/auth/scopes.get.ts) and cached in
 * shared state for the lifetime of the page so repeated checks don't re-hit
 * the endpoint.
 *
 * This is UX gating only — Sabro's API enforces the admin scope on every
 * `/admin/*` endpoint regardless of what the frontend shows.
 */
const ADMIN_SCOPE = 'api:v1:admin'

export function useAdmin() {
  const { isConfigured, isSignedIn } = useAuth()

  // null = not yet resolved; the page can treat it as "loading".
  const isAdmin = useState<boolean | null>('sabro-is-admin', () => null)

  async function refresh() {
    if (!isConfigured.value || !isSignedIn.value) {
      isAdmin.value = false
      return
    }

    try {
      const { scopes } = await $fetch<{ scopes: string[] }>('/api/auth/scopes')
      isAdmin.value = scopes.includes(ADMIN_SCOPE)
    }
    catch {
      isAdmin.value = false
    }
  }

  return { isAdmin, refresh }
}
