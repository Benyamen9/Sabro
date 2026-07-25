import { describe, expect, it } from 'vitest'
import { fromDto } from '~/composables/useUnifiedLibrary'
import type { UnifiedLibraryEntryDto } from '~/types/api'

describe('fromDto', () => {
  it('maps a never-played word with null stats', () => {
    const dto = {
      id: 'abc',
      syriacUnvocalized: 'ܪܒܐ',
      syriacVocalized: 'ܪܰܒ݁ܳܐ',
      sblTransliteration: 'rabo',
      grammaticalCategory: 'Adjective',
      letterCount: 3,
      meanings: [{ language: 'en', text: 'great' }],
      lastPlayedOn: null,
      timesPlayed: null,
    } as unknown as UnifiedLibraryEntryDto

    const word = fromDto(dto)

    expect(word.lastPlayedOn).toBeNull()
    expect(word.timesPlayed).toBeNull()
    expect(word.grammaticalCategory).toBe('Adjective')
  })

  it('coerces int32 fields that arrive as strings', () => {
    const dto = {
      id: 'abc',
      syriacUnvocalized: 'ܡܠܟܐ',
      syriacVocalized: null,
      sblTransliteration: null,
      grammaticalCategory: 'Noun',
      letterCount: '4',
      meanings: [],
      lastPlayedOn: '2026-07-20',
      timesPlayed: '2',
    } as unknown as UnifiedLibraryEntryDto

    const word = fromDto(dto)

    expect(word.letterCount).toBe(4)
    expect(word.timesPlayed).toBe(2)
    expect(word.lastPlayedOn).toBe('2026-07-20')
  })

  it('does not mistake timesPlayed: 0 for missing stats', () => {
    const dto = {
      id: 'abc',
      syriacUnvocalized: 'ܡܠܟܐ',
      syriacVocalized: null,
      sblTransliteration: null,
      grammaticalCategory: 'Noun',
      letterCount: 4,
      meanings: [],
      lastPlayedOn: '2026-07-20',
      timesPlayed: 0,
    } as unknown as UnifiedLibraryEntryDto

    const word = fromDto(dto)

    expect(word.timesPlayed).toBe(0)
  })
})
