import { describe, expect, it } from 'vitest'
import { daysAgo, markHref, mergeMarks, parseStoredMarks, type LibraryMark } from '~/utils/libraryMarks'

const EARLY = '2026-11-08T10:00:00.000Z'
const LATE = '2026-11-09T10:00:00.000Z'

const mark = (over: Partial<LibraryMark>): LibraryMark => ({ book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: null, synced: false, ...over })

describe('parseStoredMarks', () => {
  it('keeps well-formed marks, newest first', () => {
    const raw = JSON.stringify([
      { book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: { title: 'king', syriac: 'ܡܠܟܐ' }, synced: true },
      { book: 'beth-gazo', itemKey: 'c1', markedAt: LATE, label: null },
    ])

    expect(parseStoredMarks(raw)).toEqual([
      { book: 'beth-gazo', itemKey: 'c1', markedAt: LATE, label: null, synced: false },
      { book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: { title: 'king', syriac: 'ܡܠܟܐ' }, synced: true },
    ])
  })

  it('drops what it cannot trust instead of failing', () => {
    expect(parseStoredMarks(null)).toEqual([])
    expect(parseStoredMarks('not json')).toEqual([])
    expect(parseStoredMarks('{"lexicon":{}}')).toEqual([])
    const raw = JSON.stringify([
      null,
      { book: 'hymnal', itemKey: 'x', markedAt: EARLY },
      { book: 'lexicon', itemKey: '', markedAt: EARLY },
      { book: 'beth-gazo', itemKey: 'c1', markedAt: 'yesterday' },
      { book: 'beth-salme', itemKey: 'f1', markedAt: EARLY, label: { nope: true } },
      { book: 'beth-salme', itemKey: 'f1', markedAt: LATE },
    ])

    expect(parseStoredMarks(raw)).toEqual([{ book: 'beth-salme', itemKey: 'f1', markedAt: EARLY, label: null, synced: false }])
  })
})

describe('mergeMarks', () => {
  it('takes Sabro\'s marks, with its label and date, and sends nothing', () => {
    const local = [mark({ label: { title: 'old name' }, synced: true, markedAt: LATE })]

    const { merged, toSend } = mergeMarks(local, [{ book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: { title: 'king' } }])

    expect(merged).toEqual([{ book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: { title: 'king' }, synced: true }])
    expect(toSend).toEqual([])
  })

  it('sends up a mark made while signed out, and keeps it', () => {
    const local = [mark({ book: 'beth-gazo', itemKey: 'c1', markedAt: LATE })]

    const { merged, toSend } = mergeMarks(local, [{ book: 'lexicon', itemKey: 'w1', markedAt: EARLY }])

    expect(toSend).toEqual(local)
    expect(merged.map(m => m.itemKey)).toEqual(['c1', 'w1'])
  })

  it('lets go of a mark Sabro once held and no longer lists (removed on another device)', () => {
    const local = [mark({ synced: true })]

    const { merged, toSend } = mergeMarks(local, [])

    expect(merged).toEqual([])
    expect(toSend).toEqual([])
  })

  it('keeps the device\'s label when Sabro sends none, and skips books it does not know', () => {
    const local = [mark({ label: { title: 'king' } })]

    const { merged } = mergeMarks(local, [
      { book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: null },
      { book: 'hymnal', itemKey: 'past-ktab', markedAt: LATE },
    ])

    expect(merged).toEqual([{ book: 'lexicon', itemKey: 'w1', markedAt: EARLY, label: { title: 'king' }, synced: true }])
  })
})

describe('markHref', () => {
  it('opens a word on its page and a chant or figure inside its book', () => {
    expect(markHref({ book: 'lexicon', itemKey: 'w1' })).toBe('/library/lexicon/w1')
    expect(markHref({ book: 'beth-gazo', itemKey: 'c1' })).toBe('/library/beth-gazo?chant=c1')
    expect(markHref({ book: 'beth-salme', itemKey: 'f1' })).toBe('/library/beth-salme?figure=f1')
  })
})

describe('daysAgo', () => {
  it('counts calendar days, not 24-hour spans', () => {
    const now = new Date(2026, 10, 10, 0, 30)

    expect(daysAgo(new Date(2026, 10, 10, 0, 5).toISOString(), now)).toBe(0)
    expect(daysAgo(new Date(2026, 10, 9, 23, 50).toISOString(), now)).toBe(1)
    expect(daysAgo(new Date(2026, 10, 3, 12, 0).toISOString(), now)).toBe(7)
  })
})
