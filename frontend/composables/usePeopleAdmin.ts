import type { AssignRoleRequest, PersonDto, Role } from '~/types/api'

/**
 * The Owner-only people surface: who has signed in, and what each may edit.
 *
 * `name` and `email` on a person are read from Logto per request and are not
 * stored by Sabro, so they may be null — when the Management API is unreachable
 * or unconfigured the list still renders and roles are still grantable. Treat
 * them as decoration; `role` is the part that means anything.
 */
export function usePeopleAdmin() {
  const api = useSabroApi()

  function list() {
    return api<PersonDto[]>('/admin/people')
  }

  function assignRole(profileId: string, role: Role) {
    const body: AssignRoleRequest = { role }
    return api<PersonDto>(`/admin/people/${profileId}/role`, {
      method: 'PUT',
      body,
    })
  }

  return { list, assignRole }
}
