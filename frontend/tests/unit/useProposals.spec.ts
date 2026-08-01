import { describe, expect, it } from 'vitest'
import { editLinkFor } from '~/composables/useProposals'

describe('editLinkFor', () => {
  it('points at the owning area\'s edit page', () => {
    expect(editLinkFor('LexiconEntry', 'abc')).toBe('/admin/lexicon/abc')
    expect(editLinkFor('HistoricalFigure', 'def')).toBe('/admin/historical-figures/def')
  })

  it('returns null for prose targets, which have no backoffice page yet', () => {
    // Segment and Annotation belong to the deferred Translations module. Linking
    // to a route that does not exist would send the Owner to a 404 from the one
    // screen whose whole job is telling them where to go next.
    expect(editLinkFor('Segment', 'abc')).toBeNull()
    expect(editLinkFor('Annotation', 'abc')).toBeNull()
  })
})
