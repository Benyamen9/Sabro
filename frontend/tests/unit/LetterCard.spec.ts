import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import LetterCard from '~/components/LetterCard.vue'
import type { SyriacLetter } from '~/types/api'

function letter(overrides: Partial<SyriacLetter>): SyriacLetter {
  return {
    letter: 'ܒ',
    code: 'Beth',
    vowel: null,
    isBegadkephat: true,
    hardening: null,
    hardeningSource: 'None',
    ...overrides,
  }
}

describe('LetterCard', () => {
  it('renders the base letter glyph and its name', async () => {
    const wrapper = await mountSuspended(LetterCard, { props: { letter: letter({}) } })

    expect(wrapper.find('span[lang="syc"]').text()).toBe('ܒ')
    expect(wrapper.text()).toContain('Beth')
  })

  it('shows a hardening label for a marked begadkephat letter', async () => {
    const wrapper = await mountSuspended(LetterCard, {
      props: { letter: letter({ hardening: 'Rukkokho', hardeningSource: 'Marked' }) },
    })

    expect(wrapper.text()).toContain('Rukkokho')
    expect(wrapper.text()).toContain('marked in the vocalization')
  })

  it('omits the hardening section for a non-begadkephat letter', async () => {
    const wrapper = await mountSuspended(LetterCard, {
      props: { letter: letter({ letter: 'ܫ', code: 'Shin', isBegadkephat: false }) },
    })

    expect(wrapper.text()).not.toContain('Qushoyo')
    expect(wrapper.text()).not.toContain('Rukkokho')
  })
})
