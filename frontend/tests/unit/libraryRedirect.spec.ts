import { describe, expect, it } from 'vitest'
import { libraryRedirect } from '~/utils/libraryRedirect'

const WORD = '3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f'

describe('libraryRedirect', () => {
  it('moves the two books that lived at the top', () => {
    expect(libraryRedirect('/beth-gazo', '')).toBe('/library/beth-gazo')
    expect(libraryRedirect('/beth-salme/', '')).toBe('/library/beth-salme')
  })

  it('moves a word page, as Meltho links it', () => {
    expect(libraryRedirect(`/library/${WORD}`, '')).toBe(`/library/lexicon/${WORD}`)
    expect(libraryRedirect(`/library/${WORD.toUpperCase()}`, '')).toBe(`/library/lexicon/${WORD}`)
  })

  it('keeps the query string', () => {
    expect(libraryRedirect('/beth-salme', '?utm=shmo')).toBe('/library/beth-salme?utm=shmo')
  })

  it('sends the word list\'s old filters and the site search to the Lexicon', () => {
    expect(libraryRedirect('/library', '?view=meltho')).toBe('/library/lexicon?view=meltho')
    expect(libraryRedirect('/library', '?q=malko')).toBe('/library/lexicon?q=malko')
  })

  it('leaves the current addresses alone', () => {
    expect(libraryRedirect('/library', '')).toBeNull()
    expect(libraryRedirect('/library/lexicon', '')).toBeNull()
    expect(libraryRedirect(`/library/lexicon/${WORD}`, '')).toBeNull()
    expect(libraryRedirect('/library/beth-gazo', '')).toBeNull()
    expect(libraryRedirect('/library/not-a-word', '')).toBeNull()
    expect(libraryRedirect('/', '')).toBeNull()
  })
})
