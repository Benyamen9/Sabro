import type { Role, UserProfileDto } from '~/types/api'

/**
 * The signed-in user's Sabro role.
 *
 * Distinct from `useAdmin`, which only knows whether the token carries the
 * `api:v1:admin` scope. The scope says who is staff; the role says which room
 * they may enter — and telling a reviewer from an editor needs the second.
 *
 * Used only to decide what to *offer*. The API refuses on its own regardless, so
 * a stale or missing role here hides a control, never grants one.
 */
export function useMyRole() {
  const api = useSabroApi()
  const role = useState<Role | null>('sabro-my-role', () => null)

  async function refresh() {
    try {
      role.value = (await api<UserProfileDto>('/profile/me')).role
    }
    catch {
      // Signed out, or the profile call failed. Either way: offer nothing.
      role.value = null
    }
  }

  return { role, refresh }
}
