import { describe, expect, it } from 'vitest'
import { countSyriacLetters, searchHitToWord } from '~/composables/useDictionary'
import type { LexiconSearchHitDto } from '~/types/api'

describe('countSyriacLetters', () => {
  it('counts base letters only', () => {
    expect(countSyriacLetters('ܡܠܟܐ')).toBe(4)
    expect(countSyriacLetters('ܪܒܐ')).toBe(3)
  })

  it('ignores combining marks (seyame, vowel points)', () => {
    // ܡܝ̈ܐ — mayo carries seyame (U+0308) on the yudh.
    expect(countSyriacLetters('ܡܝ̈ܐ')).toBe(3)
    // Fully vocalized ܡܰܠܟ݁ܳܐ still counts 4 base letters.
    expect(countSyriacLetters('ܡܰܠܟ݁ܳܐ')).toBe(4)
  })
})

describe('searchHitToWord', () => {
  it('zips the parallel meaning arrays and computes the letter count', () => {
    const hit = {
      id: 'abc',
      syriacUnvocalized: 'ܡܠܟܐ',
      syriacVocalized: 'ܡܰܠܟ݁ܳܐ',
      sblTransliteration: 'malko',
      transliterationVariants: [],
      rootId: null,
      rootForm: 'ܡܠܟ',
      grammaticalCategory: 'Noun',
      morphology: null,
      meaningTexts: ['king', 'roi', 'koning'],
      meaningLanguages: ['en', 'fr', 'nl'],
    } as unknown as LexiconSearchHitDto

    const word = searchHitToWord(hit)

    expect(word.letterCount).toBe(4)
    expect(word.meanings).toEqual([
      { language: 'en', text: 'king' },
      { language: 'fr', text: 'roi' },
      { language: 'nl', text: 'koning' },
    ])
    expect(word.grammaticalCategory).toBe('Noun')
  })
})
