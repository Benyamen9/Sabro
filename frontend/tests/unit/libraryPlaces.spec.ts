import { describe, expect, it } from 'vitest'
import { mergePlaces, parseStoredPlaces, type LibraryPlaces } from '~/utils/libraryPlaces'

const EARLY = '2026-11-08T10:00:00.000Z'
const LATE = '2026-11-09T10:00:00.000Z'

describe('parseStoredPlaces', () => {
  it('keeps well-formed places for books on the shelf', () => {
    const raw = JSON.stringify({ lexicon: { itemKey: 'w1', openedAt: EARLY, label: { title: 'king', syriac: 'ܡܠܟܐ' } } })

    expect(parseStoredPlaces(raw)).toEqual({ lexicon: { itemKey: 'w1', openedAt: EARLY, label: { title: 'king', syriac: 'ܡܠܟܐ' } } })
  })

  it('drops what it cannot trust instead of failing', () => {
    expect(parseStoredPlaces(null)).toEqual({})
    expect(parseStoredPlaces('not json')).toEqual({})
    const raw = JSON.stringify({
      hymnal: { itemKey: 'x', openedAt: EARLY },
      lexicon: { itemKey: '', openedAt: EARLY },
      'beth-gazo': { itemKey: 'c1', openedAt: 'yesterday' },
      'beth-salme': { itemKey: 'f1', openedAt: EARLY, label: { nope: true } },
    })

    expect(parseStoredPlaces(raw)).toEqual({ 'beth-salme': { itemKey: 'f1', openedAt: EARLY, label: null } })
  })
})

describe('mergePlaces', () => {
  it('takes Sabro\'s place when the device has none, and sends nothing', () => {
    const { merged, toSend } = mergePlaces({}, [{ book: 'lexicon', itemKey: 'w1', openedAt: EARLY, label: { title: 'king' } }])

    expect(merged.lexicon).toEqual({ itemKey: 'w1', openedAt: EARLY, label: { title: 'king' } })
    expect(toSend).toEqual([])
  })

  it('sends up a place only the device has (kept while signed out)', () => {
    const local: LibraryPlaces = { 'beth-gazo': { itemKey: 'c1', openedAt: EARLY, label: null } }

    const { merged, toSend } = mergePlaces(local, [])

    expect(merged['beth-gazo']?.itemKey).toBe('c1')
    expect(toSend).toEqual([{ book: 'beth-gazo', place: local['beth-gazo'] }])
  })

  it('lets the most recently opened place win, either way', () => {
    const local: LibraryPlaces = {
      lexicon: { itemKey: 'newer-here', openedAt: LATE, label: null },
      'beth-salme': { itemKey: 'older-here', openedAt: EARLY, label: null },
    }
    const server = [
      { book: 'lexicon', itemKey: 'older-there', openedAt: EARLY, label: null },
      { book: 'beth-salme', itemKey: 'newer-there', openedAt: LATE, label: { title: 'Ephrem' } },
    ]

    const { merged, toSend } = mergePlaces(local, server)

    expect(merged.lexicon?.itemKey).toBe('newer-here')
    expect(merged['beth-salme']?.itemKey).toBe('newer-there')
    expect(toSend.map(s => s.book)).toEqual(['lexicon'])
  })

  it('prefers Sabro\'s label for the same item, as it is in the reader\'s language', () => {
    const local: LibraryPlaces = { lexicon: { itemKey: 'w1', openedAt: EARLY, label: { title: 'king' } } }

    const { merged } = mergePlaces(local, [{ book: 'lexicon', itemKey: 'w1', openedAt: EARLY, label: { title: 'roi' } }])

    expect(merged.lexicon?.label?.title).toBe('roi')
  })

  it('ignores books it does not know', () => {
    const { merged } = mergePlaces({}, [{ book: 'grammar', itemKey: 'past', openedAt: EARLY }])

    expect(merged).toEqual({})
  })
})
