import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import ChantForm from '~/components/ChantForm.vue'
import type { BethGazoModeDto, BethGazoSectionDto, ChantDto } from '~/types/api'

const modes: BethGazoModeDto[] = [
  { id: 'mode-1', name: 'Qadmoyo', position: 1 },
  { id: 'mode-3', name: 'Tlithoyo', position: 3 },
]

/** Farde admit modes; madroshe admit none — the owner's two rules, as fixtures. */
const sections: BethGazoSectionDto[] = [
  { id: 'section-farde', name: 'Farde', position: 1, allowedModeIds: ['mode-1', 'mode-3'] },
  { id: 'section-madroshe', name: 'Madroshe', position: 3, allowedModeIds: [] },
]

function chant(overrides: Partial<ChantDto> = {}): ChantDto {
  return {
    id: 'c1',
    syriacIncipit: 'ܡܪܝܡ',
    syriacIncipitVocalized: null,
    transliteration: 'Maryam yoldath Aloho',
    sectionId: 'section-farde',
    sectionName: 'Farde',
    modeId: 'mode-3',
    modeName: 'Tlithoyo',
    shuhlofo: null,
    inheritsMelodyFromId: null,
    inheritsMelodyFromTransliteration: null,
    audioUrl: null,
    status: 'Draft',
    playableInNahlo: false,
    createdAt: '2026-08-05T00:00:00Z',
    updatedAt: '2026-08-05T00:00:00Z',
    ...overrides,
  }
}

describe('ChantForm', () => {
  it('highlights the field the server named', async () => {
    const wrapper = await mountSuspended(ChantForm, {
      props: {
        chant: chant(),
        modes, sections,
        submitLabel: 'Save',
        fieldErrors: { syriacIncipit: ['SyriacIncipit is required.'] },
      },
    })

    const field = wrapper.find('#chant-syriac')
    expect(field.attributes('aria-invalid')).toBe('true')
    expect(field.attributes('aria-describedby')).toBe('chant-syriac-error')
    expect(wrapper.find('#chant-syriac-error').text()).toBe('SyriacIncipit is required.')
  })

  it('leaves every other field unmarked', async () => {
    const wrapper = await mountSuspended(ChantForm, {
      props: {
        chant: chant(),
        modes, sections,
        submitLabel: 'Save',
        fieldErrors: { syriacIncipit: ['Required.'] },
      },
    })

    expect(wrapper.find('#chant-transliteration').attributes('aria-invalid')).toBe('false')
    expect(wrapper.find('#chant-transliteration-error').exists()).toBe(false)
  })

  it('shows an error it cannot place rather than dropping it', async () => {
    const wrapper = await mountSuspended(ChantForm, {
      props: {
        chant: chant(),
        modes, sections,
        submitLabel: 'Save',
        fieldErrors: { somethingElse: ['A rule this form does not edit.'] },
      },
    })

    expect(wrapper.text()).toContain('A rule this form does not edit.')
  })

  it('never offers the chant its own melody', async () => {
    // The domain refuses a chant that inherits from itself, so offering it would
    // be offering a save that cannot succeed.
    const self = chant()
    const other = chant({ id: 'c2', transliteration: 'Qolo d-Mor Afrem' })

    const wrapper = await mountSuspended(ChantForm, {
      props: { chant: self, modes, sections, melodySources: [self, other], submitLabel: 'Save' },
    })

    const values = wrapper.findAll('#chant-parent option').map(option => option.attributes('value'))
    expect(values).toContain('c2')
    expect(values).not.toContain('c1')
  })

  it('sends the optional fields as null when their boxes are empty', async () => {
    // "This chant has no shuḥlofo" and "its shuḥlofo is the empty string" are
    // different claims, and only the first one is ever true.
    const wrapper = await mountSuspended(ChantForm, {
      props: { chant: chant(), modes, sections, submitLabel: 'Save' },
    })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]?.[0]).toEqual({
      syriacIncipit: 'ܡܪܝܡ',
      syriacIncipitVocalized: null,
      transliteration: 'Maryam yoldath Aloho',
      sectionId: 'section-farde',
      modeId: 'mode-3',
      shuhlofo: null,
      inheritsMelodyFromId: null,
    })
  })

  it('will not submit until a mode is chosen, in a section that has modes', async () => {
    // The mode is half the answer: a chant identified by melody alone names a
    // family rather than a chant.
    const wrapper = await mountSuspended(ChantForm, {
      props: { modes, sections, submitLabel: 'Create' },
    })

    await wrapper.find('#chant-syriac').setValue('ܡܪܝܡ')
    await wrapper.find('#chant-transliteration').setValue('Maryam yoldath Aloho')
    await wrapper.find('#chant-section').setValue('section-farde')

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined()

    await wrapper.find('#chant-mode').setValue('mode-1')

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeUndefined()
  })

  it('asks for no mode in a section that has none, and submits null', async () => {
    // The owner's rule: "when you choose madroshe, there is no mode for them."
    // The field is absent rather than disabled — "no mode" is the answer here.
    const wrapper = await mountSuspended(ChantForm, {
      props: { modes, sections, submitLabel: 'Create' },
    })

    await wrapper.find('#chant-syriac').setValue('ܡܪܝܡ')
    await wrapper.find('#chant-transliteration').setValue('A madrosho')
    await wrapper.find('#chant-section').setValue('section-madroshe')

    expect(wrapper.find('#chant-mode').exists()).toBe(false)
    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeUndefined()

    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]?.[0]).toMatchObject({
      sectionId: 'section-madroshe',
      modeId: null,
    })
  })

  it('clears a mode the newly chosen section does not admit', async () => {
    // Moving a fardo into the madroshe would otherwise submit a mode its
    // section says cannot exist, and the domain would refuse the save.
    const wrapper = await mountSuspended(ChantForm, {
      props: { chant: chant(), modes, sections, submitLabel: 'Save' },
    })

    expect((wrapper.find('#chant-mode').element as HTMLSelectElement).value).toBe('mode-3')

    await wrapper.find('#chant-section').setValue('section-madroshe')
    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]?.[0]).toMatchObject({ modeId: null })
  })

  it('shows a reviewer the values but nothing to change or submit', async () => {
    // Chants carry no proposal workflow yet, so a Nahlo reviewer reads and no
    // more — the form must not look editable until the server refuses it.
    const wrapper = await mountSuspended(ChantForm, {
      props: { chant: chant(), modes, sections, submitLabel: 'Save', readonly: true },
    })

    expect(wrapper.find('#chant-syriac').attributes('readonly')).toBeDefined()
    expect(wrapper.find('#chant-transliteration').attributes('readonly')).toBeDefined()
    // A select has no read-only state, so it is disabled instead.
    expect(wrapper.find('#chant-mode').attributes('disabled')).toBeDefined()
    expect(wrapper.find('#chant-parent').attributes('disabled')).toBeDefined()
    expect(wrapper.find('button[type="submit"]').exists()).toBe(false)
    expect((wrapper.find('#chant-syriac').element as HTMLInputElement).value).toBe('ܡܪܝܡ')
  })
})
