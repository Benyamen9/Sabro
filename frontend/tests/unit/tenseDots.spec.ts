import { describe, expect, it } from 'vitest'
import { GRAMMAR_TABLES, GRAMMAR_TENSES, GRAMMAR_TEXT, grammarTable, grammarTablesOf } from '~/data/grammar'
import { clusters, prepareWord } from '~/utils/tenseDots'

const BELOW = '̣'
const ABOVE = '̇'
const FEMININE = '݀'
const QUSHOYO = '݁'
const RUKKOKHO = '݂'

describe('clusters', () => {
  it('keeps each letter with its marks', () => {
    expect(clusters(`ܟ${QUSHOYO}${BELOW}ܬ${RUKKOKHO}ܰܒ${RUKKOKHO}`)).toEqual([`ܟ${QUSHOYO}${BELOW}`, `ܬ${RUKKOKHO}ܰ`, `ܒ${RUKKOKHO}`])
  })
})

describe('prepareWord, past', () => {
  it('takes the dot below out and marks it on the first letter', () => {
    const he = `ܟ${BELOW}${QUSHOYO}ܬ${RUKKOKHO}ܰܒ${RUKKOKHO}`

    const prepared = prepareWord(he, 'past')

    expect(prepared.word).toBe(`ܟ${QUSHOYO}ܬ${RUKKOKHO}ܰܒ${RUKKOKHO}`)
    expect(prepared.marks).toEqual([{ kind: 'below', marked: `ܟ${QUSHOYO}${BELOW}ܬ${RUKKOKHO}ܰܒ${RUKKOKHO}`, firstLength: 2 }])
    expect(prepared.raise).toBeNull()
  })

  it('draws "I" as a qushoyo on the final letter, with no dot below', () => {
    const i = grammarTable('past-ktab')!.rows[0]!.cells[0]![0]!

    const prepared = prepareWord(i, 'past')

    expect(prepared.word).not.toContain(ABOVE)
    expect(prepared.marks.map(mark => mark.kind)).toEqual(['first'])
    expect(prepared.marks[0]!.marked.endsWith(QUSHOYO)).toBe(true)
  })

  it('gives "she" both the dot below and the feminine dot', () => {
    const she = grammarTable('past-ktab')!.rows[4]!.cells[0]![0]!

    const prepared = prepareWord(she, 'past')

    expect(prepared.word).not.toContain(FEMININE)
    expect(prepared.marks.map(mark => mark.kind)).toEqual(['below', 'fem'])
  })

  it('leaves a word without tense dots alone', () => {
    expect(prepareWord('ܐܢܳܐ', 'past')).toEqual({ word: 'ܐܢܳܐ', marks: [], raise: null })
  })
})

describe('prepareWord, present and future', () => {
  it('measures the present\'s dot above against the first letter\'s vowel', () => {
    const he = `ܟ${QUSHOYO}ܳ${ABOVE}ܬ${RUKKOKHO}ܶܒ${RUKKOKHO}`

    const prepared = prepareWord(he, 'present')

    expect(prepared.word).toBe(`ܟ${QUSHOYO}ܳܬ${RUKKOKHO}ܶܒ${RUKKOKHO}`)
    expect(prepared.marks).toEqual([{
      kind: 'above',
      marked: `ܟ${QUSHOYO}ܳ${ABOVE}ܬ${RUKKOKHO}ܶܒ${RUKKOKHO}`,
      firstLength: 3,
      bare: `ܟ${QUSHOYO}ܬ${RUKKOKHO}ܶܒ${RUKKOKHO}`,
    }])
    expect(prepared.raise).toBeNull()
  })

  it('in the future, measures against a ghost zqofo and raises the vowel of "I" and "we"', () => {
    const rows = grammarTable('future-ktab')!.rows
    const i = prepareWord(rows[0]!.cells[0]![0]!, 'future')
    const he = prepareWord(rows[4]!.cells[0]![0]!, 'future')
    const we = prepareWord(rows[6]!.cells[0]![0]!, 'future')

    expect(i.marks[0]!.kind).toBe('above')
    expect(i.marks[0]!.ghost).toContain('ܐܳ')
    expect(i.raise).toBe(i.marks[0]!.bare)
    expect(we.raise).not.toBeNull()
    // "he" and "we" are the same letters: only the dot tells them apart
    expect(he.marks[0]!.kind).toBe('below')
    expect(he.raise).toBeNull()
    expect(he.word).toBe(we.word)
  })
})

describe('the Grammar book\'s tables', () => {
  it('has a table for every tense, with unique slugs that name their tense', () => {
    const slugs = GRAMMAR_TABLES.map(table => table.slug)

    expect(new Set(slugs).size).toBe(slugs.length)
    for (const tense of GRAMMAR_TENSES) expect(grammarTablesOf(tense).length).toBeGreaterThan(0)
    for (const table of GRAMMAR_TABLES) expect(table.slug.startsWith(`${table.tense}-`)).toBe(true)
  })

  it('keeps the slugs readers have marked', () => {
    // A reader's ܀ mark and red ribbon point at these: a renamed slug orphans them.
    for (const slug of ['past-ktab', 'past-gbo', 'present-ktab', 'present-qam', 'future-ktab']) expect(grammarTable(slug)).not.toBeNull()
  })

  it('gives every row one cell per column, and every form its tense dot', () => {
    for (const table of GRAMMAR_TABLES) {
      for (const row of table.rows) {
        expect(row.cells.length).toBe(table.columns.length)
        const forms = row.cells[0]!
        for (const form of forms) {
          const prepared = prepareWord(form.split(' ')[0]!, table.tense)
          expect(prepared.marks.length, `${table.slug} ${row.person}`).toBeGreaterThan(0)
        }
      }
    }
  })

  it('explains every tense', () => {
    for (const tense of GRAMMAR_TENSES) {
      expect(GRAMMAR_TEXT[tense].points.length).toBeGreaterThan(0)
      // Braces must pair up, or the page shows them.
      const prose = [GRAMMAR_TEXT[tense].dots, ...Object.values(GRAMMAR_TEXT[tense].categories), ...GRAMMAR_TEXT[tense].points.flatMap(p => [p.lead, p.text ?? ''])]
      for (const text of prose) expect(text.split('{').length).toBe(text.split('}').length)
    }
  })
})
