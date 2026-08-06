import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mockNuxtImport, mountSuspended } from '@nuxt/test-utils/runtime'
import PeoplePage from '~/pages/admin/people.vue'
import type { PersonDto } from '~/types/api'

/**
 * The People page end to end, minus the network: that the list groups the way it
 * claims to, that an account with no name is still called something, and — the
 * one that matters — that Owner cannot be granted by a single click.
 */

const assignRole = vi.fn()
const setAreaAccess = vi.fn()
const listPeople = vi.fn()

mockNuxtImport('usePeopleAdmin', () => () => ({
  list: listPeople,
  assignRole,
  setAreaAccess,
}))

mockNuxtImport('useAdmin', () => () => ({
  isAdmin: ref(true),
  refresh: vi.fn(),
}))

// The full shape, not just what the page reads: the section nav above it asks
// the same composable which of the other sections to offer.
mockNuxtImport('useMyAccess', () => () => ({
  profile: ref({ role: 'Owner', areas: [] }),
  role: computed(() => 'Owner'),
  isOwner: computed(() => true),
  canEdit: () => true,
  canViewBackoffice: () => true,
  canPropose: () => true,
  canDecideProposals: computed(() => true),
  canAssignRoles: computed(() => true),
  refresh: vi.fn(),
}))

function person(overrides: Partial<PersonDto> = {}): PersonDto {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    role: 'Reader',
    areas: [],
    displayName: null,
    name: null,
    email: null,
    createdAt: '2026-01-01T00:00:00Z',
    isYou: false,
    ...overrides,
  }
}

const owner = person({ id: 'aaaaaaaa-0000-0000-0000-000000000000', role: 'Owner', name: 'Benyamen' })
const editor = person({
  id: 'bbbbbbbb-0000-0000-0000-000000000000',
  name: 'Sara',
  areas: [{ area: 'Lexicon', access: 'Editor' }],
})
// The account this page used to render as "Name unavailable": Logto knows its
// address and nothing else.
const emailOnly = person({ id: 'cccccccc-0000-0000-0000-000000000000', email: 'reader@example.com' })

async function mountPage(people: PersonDto[]) {
  listPeople.mockResolvedValue(people)
  const wrapper = await mountSuspended(PeoplePage)
  await nextTick()
  await nextTick()
  return wrapper
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('the People page', () => {
  it('groups people under a heading each, Owner first', async () => {
    const wrapper = await mountPage([emailOnly, editor, owner])
    const headings = wrapper.findAll('h2').map(node => node.text())

    expect(headings[0]).toContain('Owner')
    expect(headings[1]).toContain('Editors')
    expect(headings[2]).toContain('No access')
  })

  it('calls an account by its email rather than unnamed', async () => {
    const wrapper = await mountPage([emailOnly])
    expect(wrapper.text()).toContain('reader@example.com')
    expect(wrapper.text()).not.toContain('Name unavailable')
  })

  it('narrows the list from the search box', async () => {
    const wrapper = await mountPage([editor, emailOnly])
    await wrapper.find('input[type="search"]').setValue('sara')
    await nextTick()

    expect(wrapper.text()).toContain('Sara')
    expect(wrapper.text()).not.toContain('reader@example.com')
  })

  it('does not grant Owner on the first click', async () => {
    // The whole point of the change: the button opens a question, it does not
    // hand over the backoffice. A mis-aimed click costs a dismissed dialog.
    const wrapper = await mountPage([editor])

    const button = wrapper.findAll('button').find(node => node.text().startsWith('Make Owner'))
    expect(button).toBeDefined()
    await button!.trigger('click')
    await nextTick()

    expect(assignRole).not.toHaveBeenCalled()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
    expect(wrapper.find('[role="dialog"]').text()).toContain('Sara')
  })

  it('grants Owner once the confirmation is answered', async () => {
    assignRole.mockResolvedValue({ ...editor, role: 'Owner' })
    const wrapper = await mountPage([editor])

    await wrapper.findAll('button').find(node => node.text().startsWith('Make Owner'))!.trigger('click')
    await nextTick()

    const confirm = wrapper.find('[role="dialog"]').findAll('button')
      .find(node => node.text() === 'Yes, make Owner')
    await confirm!.trigger('click')
    await nextTick()

    expect(assignRole).toHaveBeenCalledWith(editor.id, 'Owner')
  })

  it('leaves the role alone when the confirmation is dismissed', async () => {
    const wrapper = await mountPage([editor])

    await wrapper.findAll('button').find(node => node.text().startsWith('Make Owner'))!.trigger('click')
    await nextTick()
    await wrapper.find('[role="dialog"]').findAll('button')
      .find(node => node.text() === 'Cancel')!.trigger('click')
    await nextTick()

    expect(assignRole).not.toHaveBeenCalled()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })
})
