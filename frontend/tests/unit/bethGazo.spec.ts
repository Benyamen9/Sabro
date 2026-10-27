import { describe, expect, it } from 'vitest'
import type { BethGazoBookChantDto, BethGazoBookDto, BethGazoBookSectionDto } from '~/types/api'
import { chantCount, filterBook, foldForSearch, lineFor, modeLabels } from '~/utils/bethGazo'

const MODES = ['Qadmoyo', 'Trayono', 'Tlithoyo', 'Mshaḥelfotho'].map((name, i) => ({ id: `m${i + 1}`, name, position: i + 1 }))

function chant(id: string, modeId: string | null, incipit: string | null = null): BethGazoBookChantDto {
  return { id, modeId, variantKind: 'None', variantNumber: null, syriacIncipit: incipit, syriacIncipitVocalized: null, audioUrl: `/media/chants/${id}.mp3` }
}

function section(id: string, position: number, allowedModeIds: string[], melodies: BethGazoBookSectionDto['melodies']): BethGazoBookSectionDto {
  return { id, name: id, position, syriacName: null, descriptions: [], allowedModeIds, melodies }
}

const book: BethGazoBookDto = {
  modes: MODES,
  sections: [
    // The farde admit every mode, the mshaḥelfotho included; the qole only the first three.
    section('Farde', 1, ['m1', 'm2', 'm3', 'm4'], []),
    section('Madroshe', 2, [], [{ name: 'Ḥad men ʿirey', chants: [chant('c9', null)] }]),
    section('Qole shahroye', 3, ['m1', 'm2', 'm3'], [
      { name: 'Ḥablo l’itokh', chants: [chant('c1', 'm1', 'ܚܒܠܐ'), chant('c2', 'm2')] },
      { name: 'Zodeq dnehwe', chants: [chant('c3', 'm1'), chant('c4', 'm3')] },
    ]),
  ],
}

describe('modeLabels', () => {
  it('numbers the modes every moded section admits and marks the rest ✶', () => {
    const labels = modeLabels(book)
    expect(['m1', 'm2', 'm3', 'm4'].map(id => labels.get(id))).toEqual(['1', '2', '3', '✶'])
  })

  it('numbers every mode when no section has modes', () => {
    const labels = modeLabels({ modes: MODES, sections: [section('Madroshe', 1, [], [])] })
    expect([...labels.values()]).toEqual(['1', '2', '3', '4'])
  })
})

describe('filterBook', () => {
  it('keeps every section, recorded or not, when nothing is chosen', () => {
    expect(filterBook(book, '', null).map(s => s.id)).toEqual(['Farde', 'Madroshe', 'Qole shahroye'])
  })

  it('keeps only the chosen mode and the sections that still have a chant', () => {
    const shown = filterBook(book, '', 'm1')
    expect(shown.map(s => s.id)).toEqual(['Qole shahroye'])
    expect(shown[0]!.melodies.flatMap(m => m.chants.map(c => c.id))).toEqual(['c1', 'c3'])
  })

  it('drops a melody with nothing in the chosen mode', () => {
    expect(filterBook(book, '', 'm3')[0]!.melodies.map(m => m.name)).toEqual(['Zodeq dnehwe'])
  })

  it('finds a melody without its dots, breves or ʿayn', () => {
    expect(filterBook(book, 'hablo litokh', null).flatMap(s => s.melodies.map(m => m.name))).toEqual(['Ḥablo l’itokh'])
    expect(filterBook(book, 'had men irey', null).map(s => s.id)).toEqual(['Madroshe'])
  })

  it('finds a melody by its incipit', () => {
    expect(filterBook(book, 'ܚܒܠܐ', null).flatMap(s => s.melodies.map(m => m.name))).toEqual(['Ḥablo l’itokh'])
  })

  it('shows nothing when the search matches nothing', () => {
    expect(filterBook(book, 'nothing here', null)).toEqual([])
  })
})

describe('chantCount', () => {
  it('counts every chant in the sections given', () => {
    expect(chantCount(book.sections)).toBe(5)
  })
})

describe('foldForSearch', () => {
  it('folds case and romanisation marks', () => {
    expect(foldForSearch('Mshaḥelfotho')).toBe('mshahelfotho')
    expect(foldForSearch('Rbiʿoyo')).toBe('rbioyo')
  })
})

describe('lineFor', () => {
  it('falls back to English, then to nothing', () => {
    const lines = [{ language: 'en', text: 'Morning' }, { language: 'fr', text: 'Matin' }]
    expect(lineFor(lines, 'fr')).toBe('Matin')
    expect(lineFor(lines, 'sv')).toBe('Morning')
    expect(lineFor([], 'en')).toBeNull()
  })
})
