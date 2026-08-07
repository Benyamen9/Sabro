import type { AreaAccess, AreaGrantDto, ContentArea, Role, UserProfileDto } from '~/types/api'

/**
 * The facts access is decided from: the non-area role, and the per-area grants.
 * Structural, not the whole profile, so the rules below can be tested without a
 * Nuxt runtime.
 */
export interface AccessProfile {
  role: Role | null
  areas: AreaGrantDto[]
}

/**
 * What each person may do, in one place.
 *
 * The frontend mirror of the backend's `RolePermissions`, and deliberately the
 * same shape: one table of who-may-do-what rather than role comparisons spread
 * across pages. Those drift — and they did. When per-area grants replaced the
 * `LexiconReviewer` / `ShmoEditor` roles, two pages kept comparing `role`
 * against names that no grant can produce any more, so the propose panel showed
 * to nobody while the editor's form showed to everybody.
 *
 * These decide what to *offer*. The API applies the same rules itself and
 * refuses independently, so a wrong answer here hides a control — it can never
 * grant one.
 */

/** The only role that still implies area access. */
export function isOwner(profile: AccessProfile | null): boolean {
  return profile?.role === 'Owner'
}

export function accessFor(profile: AccessProfile | null, area: ContentArea): AreaAccess | null {
  return profile?.areas.find(grant => grant.area === area)?.access ?? null
}

/** May create, edit, publish and delete the area's content. */
export function canEditArea(profile: AccessProfile | null, area: ContentArea): boolean {
  return isOwner(profile) || accessFor(profile, area) === 'Editor'
}

/**
 * May open the area's backoffice — editors plus reviewers, since a reviewer has
 * to see the content to have an opinion about it.
 */
export function canViewArea(profile: AccessProfile | null, area: ContentArea): boolean {
  return canEditArea(profile, area) || accessFor(profile, area) === 'Reviewer'
}

/**
 * May propose corrections to the area. Reviewers only: an editor changes the
 * content directly, so a proposal from one would be a decision waiting on its
 * own author — and the Owner is not a reviewer of their own work.
 */
export function canProposeIn(profile: AccessProfile | null, area: ContentArea): boolean {
  return accessFor(profile, area) === 'Reviewer'
}

/**
 * Deciding proposals and granting access are Owner-only and deliberately not
 * implied by any editor grant: being trusted with content is not the same as
 * being trusted with whose correction stands, or with who else gets in.
 */
export function canDecideProposals(profile: AccessProfile | null): boolean {
  return isOwner(profile)
}

export function canAssignRoles(profile: AccessProfile | null): boolean {
  return isOwner(profile)
}

/**
 * The signed-in person's access.
 *
 * Distinct from `useAdmin`, which only knows whether the token carries the
 * `api:v1:admin` scope. Two locks: Logto says whether someone is staff at all,
 * Sabro says which areas and how much. This is the second lock.
 */
export function useMyAccess() {
  const api = useSabroApi()
  const profile = useState<UserProfileDto | null>('sabro-my-access', () => null)

  async function refresh() {
    try {
      profile.value = await api<UserProfileDto>('/profile/me')
    }
    catch {
      // Signed out, or the profile call failed. Either way: offer nothing.
      profile.value = null
    }
  }

  const role = computed<Role | null>(() => profile.value?.role ?? null)

  return {
    profile,
    role,
    isOwner: computed(() => isOwner(profile.value)),
    canEdit: (area: ContentArea) => canEditArea(profile.value, area),
    canViewBackoffice: (area: ContentArea) => canViewArea(profile.value, area),
    canPropose: (area: ContentArea) => canProposeIn(profile.value, area),
    canDecideProposals: computed(() => canDecideProposals(profile.value)),
    canAssignRoles: computed(() => canAssignRoles(profile.value)),
    refresh,
  }
}
