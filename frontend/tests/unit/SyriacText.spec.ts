import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import SyriacText from '~/components/SyriacText.vue'

describe('SyriacText', () => {
  it('renders the text inside an rtl span tagged as Syriac', async () => {
    const wrapper = await mountSuspended(SyriacText, {
      props: { text: 'ܫܠܡܐ' },
    })

    const span = wrapper.find('span')
    expect(span.attributes('dir')).toBe('rtl')
    expect(span.attributes('lang')).toBe('syc')
    expect(span.text()).toBe('ܫܠܡܐ')
  })

  it('uses the Estrangelo Edessa font family by default', async () => {
    const wrapper = await mountSuspended(SyriacText, {
      props: { text: 'ܫܠܡܐ' },
    })

    expect(wrapper.find('span').attributes('style')).toContain('Estrangelo Edessa')
  })

  it('uses Serto Jerusalem when variant=serto', async () => {
    const wrapper = await mountSuspended(SyriacText, {
      props: { text: 'ܫܠܡܐ', variant: 'serto' },
    })

    expect(wrapper.find('span').attributes('style')).toContain('Serto Jerusalem')
  })

  it('uses East Syriac Adiabene when variant=madnhaya', async () => {
    const wrapper = await mountSuspended(SyriacText, {
      props: { text: 'ܫܠܡܐ', variant: 'madnhaya' },
    })

    expect(wrapper.find('span').attributes('style')).toContain('East Syriac Adiabene')
  })
})
