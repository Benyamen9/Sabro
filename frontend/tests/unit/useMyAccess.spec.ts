import { describe, expect, it } from 'vitest'
import type { AccessProfile } from '~/composables/useMyAccess'
import {
  canAssignRoles,
  canDecideProposals,
  canEditArea,
  canProposeIn,
  canViewArea,
} from '~/composables/useMyAccess'

const reader: AccessProfile = { role: 'Reader', areas: [] }
const lexiconReviewer: AccessProfile = {
  role: 'Reader',
  areas: [{ area: 'Lexicon', access: 'Reviewer' }],
}
const lexiconEditor: AccessProfile = {
  role: 'Reader',
  areas: [{ area: 'Lexicon', access: 'Editor' }],
}
const owner: AccessProfile = { role: 'Owner', areas: [] }

describe('area access', () => {
  it('gives a person with no grant nothing at all', () => {
    // The state every new staff account starts in: through Logto's door, granted
    // no area. Showing them an editable form here is what made an unlocked-looking
    // backoffice out of a correctly locked one.
    expect(canViewArea(reader, 'Lexicon')).toBe(false)
    expect(canEditArea(reader, 'Lexicon')).toBe(false)
    expect(canProposeIn(reader, 'Lexicon')).toBe(false)
  })

  it('lets a reviewer see the area and propose, but never save', () => {
    expect(canViewArea(lexiconReviewer, 'Lexicon')).toBe(true)
    expect(canProposeIn(lexiconReviewer, 'Lexicon')).toBe(true)
    expect(canEditArea(lexiconReviewer, 'Lexicon')).toBe(false)
  })

  it('lets an editor save, and does not offer them the propose panel', () => {
    // An editor changes the content directly, so a proposal from one would be a
    // decision waiting on its own author.
    expect(canEditArea(lexiconEditor, 'Lexicon')).toBe(true)
    expect(canViewArea(lexiconEditor, 'Lexicon')).toBe(true)
    expect(canProposeIn(lexiconEditor, 'Lexicon')).toBe(false)
  })

  it('keeps a grant to one area out of the other', () => {
    expect(canViewArea(lexiconEditor, 'Shmo')).toBe(false)
    expect(canEditArea(lexiconEditor, 'Shmo')).toBe(false)
    expect(canProposeIn(lexiconReviewer, 'Shmo')).toBe(false)
  })

  it('gives the Owner every area without needing a grant', () => {
    expect(canEditArea(owner, 'Lexicon')).toBe(true)
    expect(canEditArea(owner, 'Shmo')).toBe(true)
  })

  it('does not make the Owner a reviewer of their own work', () => {
    expect(canProposeIn(owner, 'Lexicon')).toBe(false)
  })

  it('keeps deciding proposals and granting access to the Owner alone', () => {
    // Deliberately not implied by an editor grant: being trusted with content is
    // not being trusted with whose correction stands, or with who else gets in.
    expect(canDecideProposals(lexiconEditor)).toBe(false)
    expect(canAssignRoles(lexiconEditor)).toBe(false)
    expect(canDecideProposals(owner)).toBe(true)
    expect(canAssignRoles(owner)).toBe(true)
  })

  it('offers nothing when there is no profile at all', () => {
    // Signed out, or `/profile/me` failed. Fails closed, like the server handler.
    expect(canViewArea(null, 'Lexicon')).toBe(false)
    expect(canEditArea(null, 'Lexicon')).toBe(false)
    expect(canProposeIn(null, 'Lexicon')).toBe(false)
    expect(canDecideProposals(null)).toBe(false)
  })

  it('ignores the legacy area roles, which grant nothing now', () => {
    // `Role` still carries `LexiconReviewer` / `ShmoEditor` for compatibility, and
    // comparing against them is exactly the bug this replaced: they describe a
    // permission the value no longer confers.
    const legacy: AccessProfile = { role: 'LexiconEditor', areas: [] }
    expect(canEditArea(legacy, 'Lexicon')).toBe(false)
    expect(canViewArea(legacy, 'Lexicon')).toBe(false)
  })
})
