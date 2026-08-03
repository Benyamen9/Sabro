import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import SegmentedControl from '~/components/SegmentedControl.vue'

const options = [
  { value: '', label: 'No access' },
  { value: 'Reviewer', label: 'Reviewer' },
  { value: 'Editor', label: 'Editor' },
]

describe('SegmentedControl', () => {
  it('shows every choice, including the ones not taken', async () => {
    // The point of replacing the select: you can see what you are not granting.
    const wrapper = await mountSuspended(SegmentedControl, {
      props: { modelValue: 'Reviewer', options, label: 'Access to Lexicon', name: 'a' },
    })

    expect(wrapper.text()).toContain('No access')
    expect(wrapper.text()).toContain('Reviewer')
    expect(wrapper.text()).toContain('Editor')
  })

  it('is a radio group, not a row of buttons', async () => {
    // One choice among several — so a screen reader announces it as such, and
    // arrow keys move between the options for free.
    const wrapper = await mountSuspended(SegmentedControl, {
      props: { modelValue: '', options, label: 'Access to Lexicon', name: 'b' },
    })

    expect(wrapper.find('[role="radiogroup"]').attributes('aria-label')).toBe('Access to Lexicon')
    expect(wrapper.findAll('input[type="radio"]')).toHaveLength(3)
    expect((wrapper.findAll('input[type="radio"]')[0]!.element as HTMLInputElement).checked).toBe(true)
  })

  it('emits the value that was chosen', async () => {
    const wrapper = await mountSuspended(SegmentedControl, {
      props: { modelValue: '', options, label: 'Access to Lexicon', name: 'c' },
    })

    await wrapper.findAll('input[type="radio"]')[2]!.trigger('change')

    expect(wrapper.emitted('update:modelValue')).toEqual([['Editor']])
  })

  it('says nothing when the current value is chosen again', async () => {
    // Re-picking what is already set would fire a pointless write to the API.
    const wrapper = await mountSuspended(SegmentedControl, {
      props: { modelValue: 'Editor', options, label: 'Access to Lexicon', name: 'd' },
    })

    await wrapper.findAll('input[type="radio"]')[2]!.trigger('change')

    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
  })

  it('emits nothing at all while disabled', async () => {
    const wrapper = await mountSuspended(SegmentedControl, {
      props: { modelValue: '', options, label: 'Access to Lexicon', name: 'e', disabled: true },
    })

    await wrapper.findAll('input[type="radio"]')[1]!.trigger('change')

    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
  })
})
