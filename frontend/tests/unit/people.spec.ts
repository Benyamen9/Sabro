import { describe, expect, it } from 'vitest'
import type { PersonDto } from '~/types/api'
import {
  comparePeople,
  filterPeople,
  groupPeople,
  isUnidentified,
  matchesPersonSearch,
  personIdentity,
  personInitial,
  personTier,
} from '~/utils/people'

function person(overrides: Partial<PersonDto> = {}): PersonDto {
  return {
    id: '11111111-2222-3333-4444-555555555555',
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

describe('personTier', () => {
  it('puts the Owner above every grant', () => {
    // Owner does not go through the area table at all, so a row with no grants
    // is still the most powerful row on the page.
    expect(personTier(person({ role: 'Owner' }))).toBe('Owner')
  })

  it('takes the strongest grant somebody holds', () => {
    const mixed = person({
      areas: [
        { area: 'Shmo', access: 'Reviewer' },
        { area: 'Lexicon', access: 'Editor' },
      ],
    })
    expect(personTier(mixed)).toBe('Editor')
  })

  it('calls a reviewer a reviewer', () => {
    expect(personTier(person({ areas: [{ area: 'Nahlo', access: 'Reviewer' }] }))).toBe('Reviewer')
  })

  it('gives everyone else nothing', () => {
    expect(personTier(person())).toBe('None')
    // The legacy role predates area grants and opens no area on its own.
    expect(personTier(person({ role: 'ExpertReviewer' }))).toBe('None')
  })
})

describe('personIdentity', () => {
  it('prefers the name the sign-in service knows', () => {
    const identity = personIdentity(person({ name: 'Sara', displayName: 'sara-k', email: 'sara@example.com' }))
    expect(identity).toMatchObject({ label: 'Sara', source: 'name', secondary: 'sara@example.com' })
  })

  it('falls back to the self-set display name', () => {
    expect(personIdentity(person({ displayName: 'sara-k' })).label).toBe('sara-k')
  })

  it('names an account by its email rather than calling it unnamed', () => {
    // The regression this exists for: Logto's `name` is optional and stays null
    // for anyone who signed up with an address and never filled a profile in. The
    // address was already on the wire — the page just never showed it, so two
    // perfectly identifiable accounts rendered as "Name unavailable".
    const identity = personIdentity(person({ email: 'reader@example.com' }))
    expect(identity).toMatchObject({ label: 'reader@example.com', source: 'email', secondary: null })
    expect(isUnidentified(person({ email: 'reader@example.com' }))).toBe(false)
  })

  it('treats blank strings as absent', () => {
    expect(personIdentity(person({ name: '   ', email: '  ' })).source).toBe('none')
  })

  it('offers the head of the profile id when nothing at all came back', () => {
    const identity = personIdentity(person())
    expect(identity.label).toBeNull()
    expect(identity.source).toBe('none')
    expect(identity.shortId).toBe('11111111')
  })
})

describe('personInitial', () => {
  it('takes a letter from an address when there is no name', () => {
    expect(personInitial(person({ email: 'reader@example.com' }))).toBe('R')
  })

  it('marks an account it knows nothing about', () => {
    expect(personInitial(person())).toBe('?')
  })
})

describe('matchesPersonSearch', () => {
  const sara = person({ name: 'Sara', email: 'sara@example.com', displayName: 'skl' })

  it('matches an empty query', () => {
    expect(matchesPersonSearch(sara, '   ')).toBe(true)
  })

  it('matches name, display name, email, and id alike', () => {
    expect(matchesPersonSearch(sara, 'SAR')).toBe(true)
    expect(matchesPersonSearch(sara, 'skl')).toBe(true)
    expect(matchesPersonSearch(sara, 'example.com')).toBe(true)
    // An id copied out of a log should find its person.
    expect(matchesPersonSearch(sara, '11111111-2222')).toBe(true)
    expect(matchesPersonSearch(sara, 'nobody')).toBe(false)
  })
})

describe('filterPeople', () => {
  const owner = person({ id: 'a0000000-0000-0000-0000-000000000000', role: 'Owner', name: 'Owner' })
  const editor = person({
    id: 'b0000000-0000-0000-0000-000000000000',
    name: 'Editor',
    areas: [{ area: 'Lexicon', access: 'Editor' }],
  })
  const reviewer = person({
    id: 'c0000000-0000-0000-0000-000000000000',
    name: 'Reviewer',
    areas: [{ area: 'Shmo', access: 'Reviewer' }],
  })
  const nobody = person({ id: 'd0000000-0000-0000-0000-000000000000' })
  const all = [owner, editor, reviewer, nobody]

  const base = { search: '', tier: '', area: '', unidentifiedOnly: false } as const

  it('keeps everyone when nothing is set', () => {
    expect(filterPeople(all, { ...base })).toHaveLength(4)
  })

  it('filters by what somebody holds', () => {
    expect(filterPeople(all, { ...base, tier: 'Editor' })).toEqual([editor])
  })

  it('keeps the Owner under an area filter', () => {
    // The Owner reaches every area without a grant to show for it. Dropping them
    // here would have the Lexicon filter claim nobody can edit the Lexicon.
    expect(filterPeople(all, { ...base, area: 'Lexicon' })).toEqual([owner, editor])
  })

  it('finds the accounts the sign-in service could not name', () => {
    expect(filterPeople(all, { ...base, unidentifiedOnly: true })).toEqual([nobody])
  })

  it('applies filters together', () => {
    expect(filterPeople(all, { ...base, search: 'Reviewer', area: 'Lexicon' })).toEqual([])
  })
})

describe('groupPeople', () => {
  it('orders Owners first, then descending power, and drops empty groups', () => {
    const owner = person({ id: 'a0000000-0000-0000-0000-000000000000', role: 'Owner', name: 'Zoe' })
    const editor = person({
      id: 'b0000000-0000-0000-0000-000000000000',
      name: 'Adam',
      areas: [{ area: 'Lexicon', access: 'Editor' }],
    })
    const nobody = person({ id: 'c0000000-0000-0000-0000-000000000000', name: 'Beth' })

    const groups = groupPeople([nobody, editor, owner])

    // No 'Reviewer' heading: a heading over no rows reads as a loading failure.
    expect(groups.map(group => group.tier)).toEqual(['Owner', 'Editor', 'None'])
    expect(groups[0]!.people).toEqual([owner])
  })

  it('sorts within a group by name, you first, unnamed last', () => {
    const you = person({ id: 'a0000000-0000-0000-0000-000000000000', name: 'Zoe', isYou: true })
    const adam = person({ id: 'b0000000-0000-0000-0000-000000000000', name: 'Adam' })
    const unknown = person({ id: 'c0000000-0000-0000-0000-000000000000' })

    const sorted = [unknown, adam, you].sort(comparePeople)
    expect(sorted.map(p => personIdentity(p).label)).toEqual(['Zoe', 'Adam', null])
  })
})
