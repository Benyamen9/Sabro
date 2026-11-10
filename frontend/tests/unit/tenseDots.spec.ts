import { describe, expect, it } from 'vitest'
import { GRAMMAR_CHAPTERS, GRAMMAR_TABLES, GRAMMAR_TENSES, GRAMMAR_VERBS, chapterForTense, grammarChapter, grammarVerbFor } from '~/data/grammar'
import { clusters, prepareWord } from '~/utils/tenseDots'

const BELOW = '̣'
const ABOVE = '̇'
const FEMININE = '݀'
const QUSHOYO = '݁'
const RUKKOKHO = '݂'

const table = (slug: string) => GRAMMAR_TABLES.find(item => item.slug === slug)!

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
    const i = table('past-ktab').rows[0]!.cells[0]![0]!

    const prepared = prepareWord(i, 'past')

    expect(prepared.word).not.toContain(ABOVE)
    expect(prepared.marks.map(mark => mark.kind)).toEqual(['first'])
    expect(prepared.marks[0]!.marked.endsWith(QUSHOYO)).toBe(true)
  })

  it('gives "she" both the dot below and the feminine dot', () => {
    const she = table('past-ktab').rows[4]!.cells[0]![0]!

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
    const rows = table('future-ktab').rows
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

describe('the verbs\' tables', () => {
  it('gives every model verb its tenses, and every row one cell per column', () => {
    expect(GRAMMAR_VERBS.length).toBe(9)
    for (const verb of GRAMMAR_VERBS) {
      expect(verb.tables.map(item => item.tense)).toEqual(expect.arrayContaining(['past', 'present']))
      for (const item of verb.tables) {
        for (const row of item.rows) expect(row.cells.length).toBe(item.columns.length)
      }
    }
  })

  it('gives every form of a tense its tense dot', () => {
    for (const item of GRAMMAR_TABLES) {
      for (const row of item.rows) {
        for (const form of row.cells[0]!) {
          expect(prepareWord(form.split(' ')[0]!, item.tense).marks.length, `${item.slug} ${row.person}`).toBeGreaterThan(0)
        }
      }
    }
  })

  it('finds a verb\'s tables from its Lexicon entry, by class and spelling', () => {
    expect(grammarVerbFor({ grammaticalCategory: 'Verb', syriacUnvocalized: 'ܟܬܒ' })?.verb).toBe('ktab')
    expect(grammarVerbFor({ grammaticalCategory: 'Verb', syriacUnvocalized: 'ܩܡ' })?.verb).toBe('qam')
    // The same letters as a noun are another word.
    expect(grammarVerbFor({ grammaticalCategory: 'Noun', syriacUnvocalized: 'ܟܬܒ' })).toBeNull()
    expect(grammarVerbFor({ grammaticalCategory: 'Verb', syriacUnvocalized: 'ܡܠܟ' })).toBeNull()
  })
})

describe('the Grammar book\'s chapters', () => {
  it('keeps the slugs readers have marked', () => {
    // A reader's ܀ mark and red ribbon point at these: a renamed slug orphans them.
    expect(GRAMMAR_CHAPTERS.map(chapter => chapter.slug)).toEqual(['kinds-of-root', 'past', 'present', 'future', 'tense-dots', 'qushoyo-rukkokho'])
    expect(grammarChapter('past-ktab')).toBeNull()
  })

  it('has a chapter for every tense a table links to', () => {
    for (const tense of GRAMMAR_TENSES) expect(chapterForTense(tense).tense).toBe(tense)
  })

  it('pairs its braces and gives every example its dots', () => {
    for (const chapter of GRAMMAR_CHAPTERS) {
      for (const section of chapter.sections) {
        const prose = [section.intro ?? '', ...(section.points ?? []).flatMap(point => [point.lead, point.text ?? ''])]
        // Braces must pair up, or the page shows them.
        for (const text of prose) expect(text.split('{').length, text).toBe(text.split('}').length)
        for (const example of section.examples ?? []) expect(prepareWord(example.form, example.tense).marks.length, example.caption).toBeGreaterThan(0)
      }
    }
  })
})
