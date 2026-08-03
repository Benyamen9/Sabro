import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import LexiconEntryForm from '~/components/LexiconEntryForm.vue'
import type { LexiconEntryDto } from '~/types/api'

function entry(): LexiconEntryDto {
  return {
    id: 'e1',
    syriacUnvocalized: 'ܡܠܬܐ',
    syriacVocalized: null,
    sblTransliteration: 'meltho',
    grammaticalCategory: 'Noun',
    rootId: null,
    transliterationVariants: [],
    morphology: null,
    meanings: [{ language: 'en', text: 'word' }],
    status: 'Draft',
    playableInMeltho: false,
    playableLength: 4,
    pronunciationAudioUrl: null,
    createdAt: '2026-08-01T00:00:00Z',
    updatedAt: '2026-08-01T00:00:00Z',
  }
}

describe('LexiconEntryForm', () => {
  it('highlights the field the server named', async () => {
    // The bug this covers: the save banner said "check the highlighted fields"
    // while nothing on the form could ever highlight.
    const wrapper = await mountSuspended(LexiconEntryForm, {
      props: {
        entry: entry(),
        submitLabel: 'Save',
        fieldErrors: { syriacUnvocalized: ['The Syriac form is required.'] },
      },
    })

    const field = wrapper.find('#syriac-unvocalized')
    expect(field.attributes('aria-invalid')).toBe('true')
    expect(field.attributes('aria-describedby')).toBe('syriac-unvocalized-error')
    expect(wrapper.find('#syriac-unvocalized-error').text()).toBe('The Syriac form is required.')
  })

  it('leaves every other field unmarked', async () => {
    const wrapper = await mountSuspended(LexiconEntryForm, {
      props: {
        entry: entry(),
        submitLabel: 'Save',
        fieldErrors: { syriacUnvocalized: ['Required.'] },
      },
    })

    expect(wrapper.find('#sbl').attributes('aria-invalid')).toBe('false')
    expect(wrapper.find('#sbl-error').exists()).toBe(false)
  })

  it('shows an error it cannot place rather than dropping it', async () => {
    const wrapper = await mountSuspended(LexiconEntryForm, {
      props: {
        entry: entry(),
        submitLabel: 'Save',
        fieldErrors: { somethingElse: ['A rule this form does not edit.'] },
      },
    })

    expect(wrapper.text()).toContain('A rule this form does not edit.')
  })

  it('is fully editable and submittable by default', async () => {
    const wrapper = await mountSuspended(LexiconEntryForm, {
      props: { entry: entry(), submitLabel: 'Save' },
    })

    expect(wrapper.find('#syriac-unvocalized').attributes('readonly')).toBeUndefined()
    expect(wrapper.find('button[type="submit"]').exists()).toBe(true)
  })

  it('shows a reviewer the values but nothing to change or submit', async () => {
    // A reviewer needs to read the entry to have an opinion about it; their
    // surface is the propose panel, not this form.
    const wrapper = await mountSuspended(LexiconEntryForm, {
      props: { entry: entry(), submitLabel: 'Save', readonly: true },
    })

    expect(wrapper.find('#syriac-unvocalized').attributes('readonly')).toBeDefined()
    expect(wrapper.find('#meaning-en').attributes('readonly')).toBeDefined()
    // A select has no read-only state, so it is disabled instead.
    expect(wrapper.find('#category').attributes('disabled')).toBeDefined()
    expect(wrapper.find('button[type="submit"]').exists()).toBe(false)
    expect((wrapper.find('#syriac-unvocalized').element as HTMLInputElement).value).toBe('ܡܠܬܐ')
  })
})
