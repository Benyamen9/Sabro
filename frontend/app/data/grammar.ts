import raw from './grammar-tables.json'
import type { Tense } from '~/utils/tenseDots'

/**
 * The Grammar book's content (Owner, 2026-11-09): the verb in the past, the present and the future,
 * as the Owner teaches it from his grammar. The tables come from `grammar-tables.json`, written by
 * `Hito/grammar/export_hub.py` from his typed tables and confirmed rules: never edit a form here.
 *
 * The explanations are content, not interface: English first, the other four languages later
 * (Owner, 2026-11-09). Syriac inside them goes in {braces} and is set in the book's own font.
 *
 * A table's slug is what a reader's ܀ mark and red ribbon point at: never rename one.
 */
export const GRAMMAR_TENSES = ['past', 'present', 'future'] as const satisfies readonly Tense[]

export type GrammarCategory = 'soundPtoho' | 'soundRbosho' | 'doubled' | 'weakMiddle' | 'weakEnd' | 'alaphFirst' | 'yudhFirst' | 'nunFirst'
export type GrammarPerson = 'i' | 'iM' | 'iF' | 'youM' | 'youF' | 'he' | 'she' | 'we' | 'weM' | 'weF' | 'youMPl' | 'youFPl' | 'theyM' | 'theyF'
export type GrammarColumn = 'form' | 'long' | 'short' | 'future' | 'imperative'

export interface GrammarRow {
  person: GrammarPerson
  pronoun: string
  /** One list of forms per column; two forms where the book gives two, none where it gives none. */
  cells: string[][]
}

export interface GrammarTable {
  slug: string
  tense: Tense
  category: GrammarCategory
  /** The book's own name for this kind of root. */
  categorySyriac: string
  /** The model verb as the book cites it. */
  model: string
  /** `book`: typed from the Owner's grammar. `confirmed`: made by its rules and confirmed by him. */
  source: 'book' | 'confirmed'
  columns: GrammarColumn[]
  rows: GrammarRow[]
}

export const GRAMMAR_TABLES = raw.tables as GrammarTable[]
/** The book's own title for each tense's table. */
export const GRAMMAR_TITLES = raw.titles as Record<Tense, string>

export function isGrammarTense(value: string): value is Tense {
  return (GRAMMAR_TENSES as readonly string[]).includes(value)
}

export function grammarTable(slug: string): GrammarTable | null {
  return GRAMMAR_TABLES.find(table => table.slug === slug) ?? null
}

export function grammarTablesOf(tense: Tense): GrammarTable[] {
  return GRAMMAR_TABLES.filter(table => table.tense === tense)
}

/** The plural begins here: the tables draw a heavier line above this row, as the book does. */
export function startsPlural(person: GrammarPerson): boolean {
  return person === 'we' || person === 'weM'
}

export interface GrammarPoint {
  lead: string
  text?: string
}

export interface GrammarTenseText {
  /** One line under the tense's name. */
  line: string
  /** How the tense is built, in the order a learner meets it. */
  points: GrammarPoint[]
  /** What the red dots are doing in this tense. */
  dots: string
  /** What is particular to one kind of root. */
  categories: Partial<Record<GrammarCategory, string>>
}

export const GRAMMAR_TEXT: Record<Tense, GrammarTenseText> = {
  past: {
    line: 'Ten persons, one set of endings for every kind of root.',
    points: [
      { lead: 'The endings are the same in every kind of root.', text: 'Only the part before them changes.' },
      { lead: 'A {ܬ} in the ending is soft after a vowel and hard after a consonant:', text: '{ܟ݁ܶܬ݂ܒ݁ܶܬ݂} but {ܟ݁ܬ݂ܰܒ݂ܬ݁}.' },
      { lead: 'The same holds inside the root:', text: '{ܒ} is soft in {ܟ݁ܬ݂ܰܒ݂} and hard in {ܟ݁ܶܬ݂ܒ݁ܶܬ݂}, where {ܬ} now closes the syllable before it.' },
      { lead: '"I" and "she" move the vowel to the first letter:', text: '{ܟ݁ܬ݂ܰܒ݂} becomes {ܟ݁ܶܬ݂ܒ݁ܶܬ݂} and {ܟ݁ܶܬ݂ܒ݁ܰܬ݂}.' },
      { lead: 'Before the {ܬ} of an ending the last root letter stays soft,', text: 'except {ܕ} and {ܬ}, which turn hard: {ܐܶܒ݂ܰܕ݁ܬ݁} but {ܟ݁ܬ݂ܰܒ݂ܬ݁}.' },
    ],
    dots: 'Without vowels, "I", "you" and "she" are all written {ܟܬܒܬ}. Three dots tell them apart, drawn in red here: a dot below the first letter on every form except "I", a dot on the final {ܬ} of "I", and the feminine dot beside the final {ܬ} of "she". The dot below the first letter is the mark of the past.',
    categories: {
      soundPtoho: 'The book\'s model. Its vowel is ptoho: {ܟ݁ܬ݂ܰܒ݂}.',
      soundRbosho: 'Exactly like {ܟ݁ܬ݂ܰܒ݂}, with rbosho wherever it has ptoho: {ܪܓ݂ܶܙ}.',
      doubled: 'The second letter comes back, hard, only in "I" and "she": {ܦ݁ܰܟ݂} becomes {ܦ݁ܶܟ݁ܶܬ݂}.',
      weakMiddle: 'A long zqofo throughout: {ܬ݁ܳܒ݂}, {ܬ݁ܳܒ݂ܬ݁}.',
      weakEnd: 'The final {ܐ} becomes {ܝ} in "I" and before an ending, and {ܘ} in "they": {ܓ݁ܒ݂ܺܝܼܬ݂}, {ܓ݁ܒ݂ܰܝܬ݁}, {ܓ݁ܒ݂ܰܘ}. In "they" feminine the stem\'s own {ܝ} takes the seyame: {ܓ݁ܒ݂ܰܝ̈}.',
      alaphFirst: 'The {ܐ} takes rbosho where other roots have no vowel: {ܐܶܒ݂ܰܕ݂}.',
      yudhFirst: 'The {ܝ} takes hbosho where other roots have no vowel: {ܝܼܺܨܶܦ݂}.',
      nunFirst: 'The {ܢ} stays in the past, so the forms follow {ܟ݁ܬ݂ܰܒ݂}: {ܢܬ݂ܰܪ}.',
    },
  },
  present: {
    line: 'One word in four shapes, with the pronoun after it or joined to it.',
    points: [
      { lead: 'The present is built from one word in four shapes,', text: 'with the pronoun after it: {ܟ݁ܳܬ݂ܶܒ݂} for a man, {ܟ݁ܳܬ݂ܒ݁ܳܐ} for a woman, {ܟ݁ܳܬ݂ܒ݁ܺܝܼܢ} for men, {ܟ݁ܳܬ݂ܒ݁̈ܳܢ} for women. "He", "she" and "they" use the shape alone.' },
      { lead: 'So "I" and "we" have a masculine and a feminine,', text: 'which the past did not: the shape follows the speaker.' },
      { lead: 'In the short form the pronoun joins the word', text: 'and loses its first letter: {ܟ݁ܳܬ݂ܒ݁ܰܬ݁}, {ܟ݁ܳܬ݂ܒ݁ܺܝܼܢܰܢ}, {ܟ݁ܳܬ݂ܒ݁ܺܝܼܬ݁ܽܘܿܢ}.' },
      { lead: 'The pronoun\'s {ܬ} stays hard when it joins,', text: 'even after a vowel ({ܟ݁ܳܬ݂ܒ݁ܺܝܼܬ݁ܽܘܿܢ}), as it is in {ܐܰܢ̱ܬ݁}. In the past, an ending {ܬ} after a vowel was soft ({ܟ݁ܶܬ݂ܒ݁ܶܬ݂}).' },
      { lead: 'After the word, the pronoun\'s first vowel is silent:', text: '{ܐܢܳܐ}, {ܐܢ̱ܬ݁ܽܘܿܢ}.' },
      { lead: 'The feminine plurals carry seyame,', text: 'so "they" feminine {ܟ݁ܳܬ݂ܒ݁̈ܳܢ} is not written like the short "I" feminine {ܟ݁ܳܬ݂ܒ݁ܳܢ}.' },
    ],
    dots: 'Without vowels, "he wrote" and "he writes" are both {ܟܬܒ}. The dot is the only difference left: below the first letter for the past, above it for the present. Every form of the present has zqofo on its first letter and the dot above it.',
    categories: {
      soundPtoho: 'The book\'s model: {ܟ݁ܳܬ݂ܶܒ݂}.',
      soundRbosho: 'Exactly like {ܟ݁ܳܬ݂ܶܒ݂}: the rbosho of the past does not reach the present.',
      doubled: 'The masculine singular takes an {ܐ}: {ܦ݁ܳܐܶܟ݂}. In every other shape the doubled letter follows the zqofo directly, hard: {ܦ݁ܳܟ݁ܳܐ}.',
      weakMiddle: '{ܐ} in the masculine singular, {ܝ} everywhere else: {ܩܳܐܶܡ}, {ܩܳܝܡܳܐ}. A letter after that {ܝ} is hard: {ܬ݁ܳܝܒ݁ܳܐ}. The short "you" forms drop the {ܝ}, and the letter after the zqofo is soft again: {ܬ݁ܳܒ݂ܰܬ݁}.',
      weakEnd: 'The final {ܐ} becomes {ܝ}: {ܓ݁ܳܒ݂ܝܳܐ}, {ܓ݁ܳܒ݂ܶܝܢ}. "I" masculine has a short form in this kind of root only.',
      alaphFirst: 'The {ܐ} takes zqofo like any first letter: {ܐܳܒ݂ܶܕ݂}.',
      yudhFirst: 'The {ܝ} is an ordinary first letter here, with zqofo: {ܝܳܨܶܦ݂}.',
      nunFirst: 'The {ܢ} stays, as in the past. The masculine singular has ptoho: {ܢܳܬ݂ܰܪ}.',
    },
  },
  future: {
    line: 'A letter in front of the root, and the imperative beside it.',
    points: [
      { lead: 'The future is built with a letter in front,', text: 'not a pronoun after: {ܐ} for "I", {ܬ} for "you" and "she", {ܢ} for "he", "we" and "they". Each takes an rbosho.' },
      { lead: 'The root then has {ܘ} before its last letter:', text: '{ܐܶܟ݂ܬ݁ܽܘܿܒ݂}. An ending pushes the {ܘ} out: {ܬ݁ܶܟ݂ܬ݁ܒ݂ܽܘܼܢ}.' },
      { lead: 'The front letter is hard;', text: 'the first root letter after its vowel turns soft, and the second goes hard.' },
      { lead: '{ܒ} stays soft even when the {ܘ} goes', text: '({ܬ݁ܶܟ݂ܬ݁ܒ݂ܺܝܼܢ}). In the present it went hard in the same place ({ܟ݁ܳܬ݂ܒ݁ܳܐ}): a soft letter remembers the vowel that was there.' },
      { lead: '"I" is the same for a man and a woman,', text: 'and "we" has one form for both.' },
      { lead: 'The imperative is the future without its front letter:', text: '{ܬ݁ܶܟ݂ܬ݁ܽܘܿܒ݂} gives {ܟ݁ܬ݂ܽܘܿܒ݂}. It exists only for "you", and every person but "you" masculine has a short and a longer form.' },
    ],
    dots: 'Two tense dots: above the first letter for "I" and "we", below it for "you", "she", "he" and "they". So "he" and "we", both {ܢܶܟ݂ܬ݁ܽܘܿܒ݂}, differ only by the dot. The imperative has none.',
    categories: {
      soundPtoho: 'The book\'s model: {ܢܶܟ݂ܬ݁ܽܘܿܒ݂}. The other kinds of root will follow.',
    },
  },
}
