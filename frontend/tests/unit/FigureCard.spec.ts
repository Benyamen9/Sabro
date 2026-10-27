import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import FigureCard from '~/components/FigureCard.vue'
import FigureDetails from '~/components/FigureDetails.vue'
import type { GalleryFigure } from '~/utils/gallery'

function figure(overrides: Partial<GalleryFigure> = {}): GalleryFigure {
  return {
    id: 'ephrem',
    name: 'Ephrem the Syrian',
    era: 4,
    category: 'Patristic',
    period: 'NiceneEra',
    role: 'Commentator',
    region: 'Mesopotamia',
    tradition: 'PreChalcedonian',
    gender: 'Male',
    emblem: 'Harp',
    syriacName: 'ܡܪܝ ܐܦܪܝܡ',
    readMoreUrl: 'https://syriaca.org/person/13',
    no: '171',
    tier: 'found',
    timesMet: 2,
    firstFoundOn: '2026-10-08',
    fewestAttempts: 8,
    illuminated: false,
    descriptions: [{ language: 'en', text: 'Deacon of Nisibis and Edessa.' }],
    ...overrides,
  }
}

describe('FigureCard', () => {
  it('names the figure and its status for screen readers', async () => {
    const wrapper = await mountSuspended(FigureCard, { props: { figure: figure() } })

    expect(wrapper.find('button').attributes('aria-label')).toBe('Ephrem the Syrian, Found')
    expect(wrapper.text()).toContain('✓')
  })

  it('marks an illuminated find with the star and the foil', async () => {
    const wrapper = await mountSuspended(FigureCard, { props: { figure: figure({ illuminated: true }) } })

    expect(wrapper.text()).toContain('✦')
    expect(wrapper.find('.foil').exists()).toBe(true)
  })

  it('shows a figure not met yet by name, without a century', async () => {
    const wrapper = await mountSuspended(FigureCard, {
      props: { figure: figure({ tier: 'locked', timesMet: 0, firstFoundOn: null, fewestAttempts: null }) },
    })

    expect(wrapper.text()).toContain('Ephrem the Syrian')
    expect(wrapper.text()).toContain('not met yet')
    expect(wrapper.text()).not.toContain('✓')
  })
})

describe('FigureDetails', () => {
  it('shows the story and the link whether the figure is found, met or not met yet', async () => {
    for (const tier of ['found', 'met', 'locked'] as const) {
      const wrapper = await mountSuspended(FigureDetails, { props: { figure: figure({ tier }) } })

      expect(wrapper.text(), tier).toContain('Deacon of Nisibis and Edessa.')
      expect(wrapper.find('a[href="https://syriaca.org/person/13"]').exists(), tier).toBe(true)
    }
  })

  it('says so for a figure not met yet', async () => {
    const wrapper = await mountSuspended(FigureDetails, { props: { figure: figure({ tier: 'locked', timesMet: 0 }) } })

    expect(wrapper.text()).toContain('Not met yet in Shmo')
  })

  it('says so when a figure has no story yet', async () => {
    const wrapper = await mountSuspended(FigureDetails, { props: { figure: figure({ descriptions: [] }) } })

    expect(wrapper.text()).toContain('No story written yet.')
  })
})
