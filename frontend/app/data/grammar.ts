import raw from './grammar-tables.json'
import type { Tense } from '~/utils/tenseDots'

/**
 * The grammar of the verb (Owner, 2026-11-09 and 2026-11-10), in two halves:
 *
 * - **The tables** belong to the words. A verb's page in the Lexicon carries its conjugation in the
 *   past, the present and the future. They come from `grammar-tables.json`, written by
 *   `Hito/grammar/export_hub.py` from the Owner's typed tables and confirmed rules: never edit a
 *   form here.
 * - **The rules** are the Grammar book: a few chapters that explain how the tables are built, each
 *   pointing at the verbs to see it in.
 *
 * The explanations are content, not interface: English first, the other four languages later
 * (Owner, 2026-11-09). Syriac inside them goes in {braces} and is set in the book's own font.
 *
 * A chapter's slug is what a reader's ܀ mark and red ribbon point at: never rename one.
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
  /** The model verb this table conjugates (`ktab`, `qam` ...). */
  verb: string
  /** That verb as the Lexicon spells it: the past "he" form without points. */
  lemma: string
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

/** A verb the Grammar has tables for, with what the Lexicon needs to recognise it. */
export interface GrammarVerb {
  verb: string
  lemma: string
  model: string
  category: GrammarCategory
  categorySyriac: string
  tables: GrammarTable[]
}

export const GRAMMAR_VERBS: GrammarVerb[] = []
for (const table of GRAMMAR_TABLES) {
  let entry = GRAMMAR_VERBS.find(item => item.verb === table.verb)
  if (!entry) {
    entry = { verb: table.verb, lemma: table.lemma, model: table.model, category: table.category, categorySyriac: table.categorySyriac, tables: [] }
    GRAMMAR_VERBS.push(entry)
  }
  entry.tables.push(table)
}

/** Letters only: a Lexicon spelling compared without points or other marks. */
const bare = (text: string) => Array.from(text).filter(ch => /\p{L}/u.test(ch)).join('')

/**
 * The tables for a Lexicon entry, or null. Only an entry of class Verb whose spelling is one of the
 * model verbs carries them: the same letters can also spell a noun.
 */
export function grammarVerbFor(entry: { grammaticalCategory?: string | null, syriacUnvocalized: string }): GrammarVerb | null {
  if (entry.grammaticalCategory !== 'Verb') return null
  const spelling = bare(entry.syriacUnvocalized)
  return GRAMMAR_VERBS.find(item => item.lemma === spelling) ?? null
}

/** The plural begins here: the tables draw a heavier line above this row, as the book does. */
export function startsPlural(person: GrammarPerson): boolean {
  return person === 'we' || person === 'weM'
}

export interface GrammarPoint {
  lead: string
  text?: string
}

export interface GrammarSection {
  /** English, like all the chapter's prose. Absent on a chapter's opening section. */
  heading?: string
  intro?: string
  points?: GrammarPoint[]
  /** Forms shown large with their tense dots in red, each with a caption. */
  examples?: { form: string, tense: Tense, caption: string }[]
}

export interface GrammarChapter {
  slug: string
  /** The tense whose table a verb's page opens on when reached from this chapter. */
  tense?: Tense
  /** The book's own title for the tense's table, where the chapter is a tense. */
  syriacTitle?: string
  sections: GrammarSection[]
}

/** What is particular to each kind of root, shown in the tense chapters. */
const KINDS: Record<GrammarCategory, string> = {
  soundPtoho: 'Sound, with ptoho',
  soundRbosho: 'Sound, with rbosho',
  doubled: 'Doubled',
  weakMiddle: 'Weak in the middle',
  weakEnd: 'Weak at the end',
  alaphFirst: 'Olaf at the start',
  yudhFirst: 'Yudh at the start',
  nunFirst: 'Nun at the start',
}

const kind = (category: GrammarCategory, text: string): GrammarPoint => ({ lead: `${KINDS[category]}.`, text })

export const GRAMMAR_CHAPTERS: GrammarChapter[] = [
  {
    slug: 'kinds-of-root',
    sections: [
      {
        intro: 'A verb is built on a root, most often of three letters. The grammar sorts roots into kinds by the letters they are made of, because each kind bends the same endings a little differently. Each kind has a model verb, and the tables are given for that verb.',
        points: [
          { lead: '{ܫܰܠܡܬ݂ܳܐ ܦ݂ܬ݂ܺܝܼܚܬ݁ܳܐ}, sound with ptoho.', text: 'Three firm letters, and ptoho in the past: {ܟ݁ܬ݂ܰܒ݂}.' },
          { lead: '{ܫܰܠܡܬ݂ܳܐ ܪܒ݂ܺܝܼܨܬ݁ܳܐ}, sound with rbosho.', text: 'The same, with rbosho where the first kind has ptoho: {ܪܓ݂ܶܙ}.' },
          { lead: '{ܥܦ݂ܺܝܼܦ݂ܬ݁ܳܐ}, doubled.', text: 'The second letter counts twice and is written once: {ܦ݁ܰܟ݂}.' },
          { lead: '{ܟ݁ܪܺܝܼܗܰܬ݂ ܡܶܨܥܳܝܳܐ}, weak in the middle.', text: 'The middle of the root is weak and shows as a long zqofo in the past: {ܬ݁ܳܒ݂}, {ܩܳܡ}.' },
          { lead: '{ܟ݁ܪܺܝܼܗܰܬ݂ ܫܽܘܼܠܳܡܳܐ}, weak at the end.', text: 'The last letter is weak and written {ܐ}: {ܓ݁ܒ݂ܳܐ}.' },
          { lead: '{ܐܳܠܰܐܦ݂ܳܝܰܬ݂ ܫܽܘܼܪܳܝܳܐ}, olaf at the start.', text: 'The root begins with {ܐ}, which needs a vowel of its own: {ܐܶܒ݂ܰܕ݂}.' },
          { lead: '{ܟ݁ܪܺܝܼܗܰܬ݂ ܫܽܘܼܪܳܝܳܐ}, yudh at the start.', text: 'The root begins with {ܝ}, which also takes a vowel of its own: {ܝܼܺܨܶܦ݂}.' },
          { lead: '{ܢܽܘܼܢܳܝܬ݁ܳܐ}, nun at the start.', text: 'The root begins with {ܢ}: {ܢܬ݂ܰܪ}.' },
        ],
      },
    ],
  },
  {
    slug: 'past',
    tense: 'past',
    syriacTitle: GRAMMAR_TITLES.past,
    sections: [
      {
        intro: 'Ten persons, and one set of endings for every kind of root.',
        points: [
          { lead: 'The endings are the same in every kind of root.', text: 'Only the part before them changes.' },
          { lead: 'A {ܬ} in the ending is soft after a vowel and hard after a consonant:', text: '{ܟ݁ܶܬ݂ܒ݁ܶܬ݂} but {ܟ݁ܬ݂ܰܒ݂ܬ݁}.' },
          { lead: 'The same holds inside the root:', text: '{ܒ} is soft in {ܟ݁ܬ݂ܰܒ݂} and hard in {ܟ݁ܶܬ݂ܒ݁ܶܬ݂}, where {ܬ} now closes the syllable before it.' },
          { lead: '"I" and "she" move the vowel to the first letter:', text: '{ܟ݁ܬ݂ܰܒ݂} becomes {ܟ݁ܶܬ݂ܒ݁ܶܬ݂} and {ܟ݁ܶܬ݂ܒ݁ܰܬ݂}.' },
          { lead: 'Before the {ܬ} of an ending the last root letter stays soft,', text: 'except {ܕ} and {ܬ}, which turn hard: {ܐܶܒ݂ܰܕ݁ܬ݁} but {ܟ݁ܬ݂ܰܒ݂ܬ݁}.' },
        ],
      },
      {
        heading: 'Kind by kind',
        points: [
          kind('soundRbosho', 'Exactly like {ܟ݁ܬ݂ܰܒ݂}, with rbosho wherever it has ptoho: {ܪܓ݂ܶܙ}.'),
          kind('doubled', 'The second letter comes back, hard, only in "I" and "she": {ܦ݁ܰܟ݂} becomes {ܦ݁ܶܟ݁ܶܬ݂}.'),
          kind('weakMiddle', 'A long zqofo throughout: {ܬ݁ܳܒ݂}, {ܬ݁ܳܒ݂ܬ݁}.'),
          kind('weakEnd', 'The final {ܐ} becomes {ܝ} in "I" and before an ending, and {ܘ} in "they": {ܓ݁ܒ݂ܺܝܼܬ݂}, {ܓ݁ܒ݂ܰܝܬ݁}, {ܓ݁ܒ݂ܰܘ}. In "they" feminine the stem\'s own {ܝ} takes the seyame: {ܓ݁ܒ݂ܰܝ̈}.'),
          kind('alaphFirst', 'The {ܐ} takes rbosho where other roots have no vowel: {ܐܶܒ݂ܰܕ݂}.'),
          kind('yudhFirst', 'The {ܝ} takes hbosho where other roots have no vowel: {ܝܼܺܨܶܦ݂}.'),
          kind('nunFirst', 'The {ܢ} stays in the past, so the forms follow {ܟ݁ܬ݂ܰܒ݂}: {ܢܬ݂ܰܪ}.'),
        ],
      },
    ],
  },
  {
    slug: 'present',
    tense: 'present',
    syriacTitle: GRAMMAR_TITLES.present,
    sections: [
      {
        intro: 'One word in four shapes, with the pronoun after it or joined to it.',
        points: [
          { lead: 'The present is built from one word in four shapes,', text: 'with the pronoun after it: {ܟ݁ܳܬ݂ܶܒ݂} for a man, {ܟ݁ܳܬ݂ܒ݁ܳܐ} for a woman, {ܟ݁ܳܬ݂ܒ݁ܺܝܼܢ} for men, {ܟ݁ܳܬ݂ܒ݁̈ܳܢ} for women. "He", "she" and "they" use the shape alone.' },
          { lead: 'So "I" and "we" have a masculine and a feminine,', text: 'which the past did not: the shape follows the speaker.' },
          { lead: 'In the short form the pronoun joins the word', text: 'and loses its first letter: {ܟ݁ܳܬ݂ܒ݁ܰܬ݁}, {ܟ݁ܳܬ݂ܒ݁ܺܝܼܢܰܢ}, {ܟ݁ܳܬ݂ܒ݁ܺܝܼܬ݁ܽܘܿܢ}.' },
          { lead: 'The pronoun\'s {ܬ} stays hard when it joins,', text: 'even after a vowel ({ܟ݁ܳܬ݂ܒ݁ܺܝܼܬ݁ܽܘܿܢ}), as it is in {ܐܰܢ̱ܬ݁}. In the past, an ending {ܬ} after a vowel was soft ({ܟ݁ܶܬ݂ܒ݁ܶܬ݂}).' },
          { lead: 'After the word, the pronoun\'s first vowel is silent:', text: '{ܐܢܳܐ}, {ܐܢ̱ܬ݁ܽܘܿܢ}.' },
          { lead: 'The feminine plurals carry seyame,', text: 'so "they" feminine {ܟ݁ܳܬ݂ܒ݁̈ܳܢ} is not written like the short "I" feminine {ܟ݁ܳܬ݂ܒ݁ܳܢ}.' },
        ],
      },
      {
        heading: 'Kind by kind',
        points: [
          kind('soundRbosho', 'Exactly like {ܟ݁ܳܬ݂ܶܒ݂}: the rbosho of the past does not reach the present.'),
          kind('doubled', 'The masculine singular takes an {ܐ}: {ܦ݁ܳܐܶܟ݂}. In every other shape the doubled letter follows the zqofo directly, hard: {ܦ݁ܳܟ݁ܳܐ}.'),
          kind('weakMiddle', '{ܐ} in the masculine singular, {ܝ} everywhere else: {ܩܳܐܶܡ}, {ܩܳܝܡܳܐ}. A letter after that {ܝ} is hard: {ܬ݁ܳܝܒ݁ܳܐ}. The short "you" forms drop the {ܝ}, and the letter after the zqofo is soft again: {ܬ݁ܳܒ݂ܰܬ݁}.'),
          kind('weakEnd', 'The final {ܐ} becomes {ܝ}: {ܓ݁ܳܒ݂ܝܳܐ}, {ܓ݁ܳܒ݂ܶܝܢ}. "I" masculine has a short form in this kind of root only.'),
          kind('alaphFirst', 'The {ܐ} takes zqofo like any first letter: {ܐܳܒ݂ܶܕ݂}.'),
          kind('yudhFirst', 'The {ܝ} is an ordinary first letter here, with zqofo: {ܝܳܨܶܦ݂}.'),
          kind('nunFirst', 'The {ܢ} stays, as in the past. The masculine singular has ptoho: {ܢܳܬ݂ܰܪ}.'),
        ],
      },
    ],
  },
  {
    slug: 'future',
    tense: 'future',
    syriacTitle: GRAMMAR_TITLES.future,
    sections: [
      {
        intro: 'A letter in front of the root, and the imperative beside it. So far the book\'s table is for the sound root; the other kinds will follow.',
        points: [
          { lead: 'The future is built with a letter in front,', text: 'not a pronoun after: {ܐ} for "I", {ܬ} for "you" and "she", {ܢ} for "he", "we" and "they". Each takes an rbosho.' },
          { lead: 'The root then has {ܘ} before its last letter:', text: '{ܐܶܟ݂ܬ݁ܽܘܿܒ݂}. An ending pushes the {ܘ} out: {ܬ݁ܶܟ݂ܬ݁ܒ݂ܽܘܼܢ}.' },
          { lead: 'The front letter is hard;', text: 'the first root letter after its vowel turns soft, and the second goes hard.' },
          { lead: '{ܒ} stays soft even when the {ܘ} goes', text: '({ܬ݁ܶܟ݂ܬ݁ܒ݂ܺܝܼܢ}). In the present it went hard in the same place ({ܟ݁ܳܬ݂ܒ݁ܳܐ}): a soft letter remembers the vowel that was there.' },
          { lead: '"I" is the same for a man and a woman,', text: 'and "we" has one form for both.' },
        ],
      },
      {
        heading: 'The imperative',
        points: [
          { lead: 'The imperative is the future without its front letter:', text: '{ܬ݁ܶܟ݂ܬ݁ܽܘܿܒ݂} gives {ܟ݁ܬ݂ܽܘܿܒ݂}.' },
          { lead: 'It exists only for "you",', text: 'and every person but "you" masculine has a short and a longer form.' },
        ],
      },
    ],
  },
  {
    slug: 'tense-dots',
    sections: [
      {
        intro: 'Written without vowels, many forms of a verb look the same. A dot on the word tells them apart. In the Lexicon\'s tables these dots are drawn in red.',
      },
      {
        heading: 'Below for the past, above for the present',
        intro: 'Without vowels, "he wrote" and "he writes" are both {ܟܬܒ}. The dot is the only difference left.',
        examples: [
          { form: 'ܟ̣݁ܬ݂ܰܒ݂', tense: 'past', caption: 'he wrote: the dot below the first letter' },
          { form: 'ܟ݁ܳ̇ܬ݂ܶܒ݂', tense: 'present', caption: 'he writes: the dot above it' },
        ],
      },
      {
        heading: 'Three dots in the past',
        intro: 'Without vowels, "I", "you" and "she" are all written {ܟܬܒܬ}. Every form but "I" has the dot below the first letter; "I" has a dot on its final {ܬ}; "she" has the feminine dot beside hers.',
        examples: [
          { form: 'ܟ݁ܶܬ݂ܒ݁ܶܬ݂̇', tense: 'past', caption: 'I wrote' },
          { form: 'ܟ̣݁ܬ݂ܰܒ݂ܬ݁', tense: 'past', caption: 'you wrote' },
          { form: 'ܟ̣݁ܶܬ݂ܒ݁ܰܬ݂݀', tense: 'past', caption: 'she wrote' },
        ],
      },
      {
        heading: 'Two dots in the future',
        intro: 'Above the first letter for "I" and "we", below it for "you", "she", "he" and "they". So "he" and "we" differ only by the dot. The imperative has none.',
        examples: [
          { form: 'ܢ̣ܶܟ݂ܬ݁ܽܘܿܒ݂', tense: 'future', caption: 'he will write' },
          { form: 'ܢܶ̇ܟ݂ܬ݁ܽܘܿܒ݂', tense: 'future', caption: 'we will write' },
        ],
      },
    ],
  },
  {
    slug: 'qushoyo-rukkokho',
    sections: [
      {
        intro: 'Six letters, {ܒ ܓ ܕ ܟ ܦ ܬ}, have a hard sound and a soft one. A dot above the letter (qushoyo) marks the hard sound, a dot below it (rukkokho) the soft one. Which one a letter takes depends on what stands before it, so it changes from one form of a verb to the next.',
        points: [
          { lead: 'At the start of a word the letter is hard:', text: '{ܟ݁ܬ݂ܰܒ݂}.' },
          { lead: 'After a vowel it is soft, after a consonant hard.', text: 'So the ending {ܬ} is soft in {ܟ݁ܶܬ݂ܒ݁ܶܬ݂} and hard in {ܟ݁ܬ݂ܰܒ݂ܬ݁}.' },
          { lead: 'The same letter changes inside one verb:', text: '{ܒ} is soft in {ܟ݁ܬ݂ܰܒ݂} and hard in {ܟ݁ܶܬ݂ܒ݁ܶܬ݂}, where {ܬ} closes the syllable before it.' },
          { lead: 'Before the {ܬ} of an ending the last root letter stays soft,', text: 'except {ܕ} and {ܬ}, which turn hard: {ܐܶܒ݂ܰܕ݁ܬ݁} but {ܟ݁ܬ݂ܰܒ݂ܬ݁}.' },
          { lead: 'A pronoun\'s {ܬ} stays hard when it joins the word,', text: 'even after a vowel: {ܟ݁ܳܬ݂ܒ݁ܺܝܼܬ݁ܽܘܿܢ}, as it is in {ܐܰܢ̱ܬ݁}.' },
          { lead: 'After a zqofo and {ܝ} the letter is hard:', text: '{ܬ݁ܳܝܒ݁ܳܐ}. Where the {ܝ} drops it is soft again: {ܬ݁ܳܒ݂ܰܬ݁}.' },
          { lead: 'A soft letter remembers the vowel that was there.', text: 'In the future {ܒ} stays soft even when the {ܘ} before it goes: {ܬ݁ܶܟ݂ܬ݁ܒ݂ܺܝܼܢ}.' },
        ],
      },
    ],
  },
]

export function grammarChapter(slug: string): GrammarChapter | null {
  return GRAMMAR_CHAPTERS.find(chapter => chapter.slug === slug) ?? null
}

/** The chapter that explains a tense: where a verb's table links for its rules. */
export function chapterForTense(tense: Tense): GrammarChapter {
  return GRAMMAR_CHAPTERS.find(chapter => chapter.tense === tense)!
}
