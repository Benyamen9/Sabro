import { describe, expect, it } from 'vitest'
import type { HistoricalFigureListItem, ShmoCollectionCardDto } from '~/types/api'
import { EMBLEM_PATHS, ROLE_EMBLEM_PATHS } from '~/utils/emblems'
import { buildGallery, matchesGallery, pickDescription } from '~/utils/gallery'
import { HISTORICAL_FIGURE_EMBLEMS, HISTORICAL_FIGURE_ROLES } from '~/utils/historicalFigures'

function figure(id: string, name: string, era: number): HistoricalFigureListItem {
  return {
    id,
    name,
    era,
    category: 'Patristic',
    period: 'NiceneEra',
    role: 'Bishop',
    region: 'Mesopotamia',
    tradition: 'PreChalcedonian',
    gender: 'Male',
    emblem: null,
    syriacName: null,
    readMoreUrl: null,
  }
}

function card(id: string, status: 'Met' | 'Found', extra: Partial<ShmoCollectionCardDto> = {}): ShmoCollectionCardDto {
  return {
    historicalFigureId: id,
    status,
    timesMet: 1,
    firstFoundOn: status === 'Found' ? '2026-10-20' : null,
    fewestAttempts: status === 'Found' ? 4 : null,
    illuminated: false,
    descriptions: [],
    ...extra,
  }
}

const roster = [figure('c', 'Ephrem', 4), figure('a', 'Abel', -41), figure('b', 'Aphrahat', 4)]

describe('buildGallery', () => {
  it('lists every figure A–Z, numbered through time', () => {
    const gallery = buildGallery(roster, [])

    expect(gallery.map(f => f.name)).toEqual(['Abel', 'Aphrahat', 'Ephrem'])
    // Abel is the oldest; Aphrahat and Ephrem share a century and break the tie by name.
    expect(gallery.map(f => f.no)).toEqual(['001', '002', '003'])
  })

  it('marks figures without a card as not met yet', () => {
    expect(buildGallery(roster, []).every(f => f.tier === 'locked')).toBe(true)
  })

  it('takes tier, progress and story from the collection', () => {
    const gallery = buildGallery(roster, [
      card('a', 'Met', { timesMet: 3 }),
      card('c', 'Found', { illuminated: true, descriptions: [{ language: 'en', text: 'Deacon of Nisibis.' }] }),
    ])
    const abel = gallery.find(f => f.id === 'a')!
    const ephrem = gallery.find(f => f.id === 'c')!

    expect(abel.tier).toBe('met')
    expect(abel.timesMet).toBe(3)
    expect(ephrem.tier).toBe('found')
    expect(ephrem.illuminated).toBe(true)
    expect(ephrem.fewestAttempts).toBe(4)
    expect(ephrem.descriptions).toHaveLength(1)
  })
})

describe('matchesGallery', () => {
  const gallery = buildGallery(roster, [card('a', 'Met'), card('c', 'Found', { illuminated: true })])

  it('filters by tier and by illumination', () => {
    expect(gallery.filter(f => matchesGallery(f, 'found', '')).map(f => f.id)).toEqual(['c'])
    expect(gallery.filter(f => matchesGallery(f, 'illuminated', '')).map(f => f.id)).toEqual(['c'])
    expect(gallery.filter(f => matchesGallery(f, 'locked', '')).map(f => f.id)).toEqual(['b'])
  })

  it('searches names without regard to case', () => {
    expect(gallery.filter(f => matchesGallery(f, 'all', 'APH')).map(f => f.name)).toEqual(['Aphrahat'])
  })
})

describe('pickDescription', () => {
  const stories = [
    { language: 'en', text: 'English' },
    { language: 'fr', text: 'Français' },
  ]

  it('prefers the reader’s language, then English', () => {
    expect(pickDescription(stories, 'fr')).toBe('Français')
    expect(pickDescription(stories, 'sv')).toBe('English')
  })

  it('returns null when nothing is written', () => {
    expect(pickDescription([], 'en')).toBeNull()
  })
})

describe('emblems', () => {
  it('draws every emblem the API can send', () => {
    for (const emblem of HISTORICAL_FIGURE_EMBLEMS) {
      expect(EMBLEM_PATHS[emblem], emblem).toMatch(/^M/)
    }
  })

  it('draws every role, for figures without an emblem of their own', () => {
    for (const role of HISTORICAL_FIGURE_ROLES) {
      expect(ROLE_EMBLEM_PATHS[role], role).toMatch(/^M/)
    }
  })
})
