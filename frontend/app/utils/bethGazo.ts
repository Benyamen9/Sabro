import type { BethGazoBookChantDto, BethGazoBookDto, BethGazoBookMelodyDto, BethGazoBookSectionDto, BethGazoDescriptionDto } from '~/types/api'

/** The five languages the ecosystem serves, in the order the backoffice lists them. */
export const BETH_GAZO_LANGUAGES = ['en', 'fr', 'nl', 'de', 'sv'] as const

/** A section's or a mode's descriptions as one text box per language. */
export function linesOf(descriptions: readonly BethGazoDescriptionDto[] | null | undefined): Record<string, string> {
  const lines: Record<string, string> = Object.fromEntries(BETH_GAZO_LANGUAGES.map(language => [language, '']))
  for (const d of descriptions ?? []) lines[d.language] = d.text
  return lines
}

/** The boxes back as descriptions: empty ones are left out rather than sent blank. */
export function descriptionsOf(lines: Record<string, string>): BethGazoDescriptionDto[] {
  return BETH_GAZO_LANGUAGES
    .map(language => ({ language, text: (lines[language] ?? '').trim() }))
    .filter(d => d.text.length > 0)
}

/** The line in the reader's language, else English, else none. */
export function lineFor(descriptions: readonly BethGazoDescriptionDto[] | null | undefined, locale: string): string | null {
  const list = descriptions ?? []
  return (list.find(d => d.language === locale) ?? list.find(d => d.language === 'en'))?.text ?? null
}

/**
 * The label each mode carries on the book page: the moded sections' common modes
 * are numbered in order (the farde 1–8), and a mode only some admit — today the
 * mshaḥelfotho — is marked ✶ rather than given the next number. Derived from the
 * section rows, as Nahlo derives its conditional modes; never a count of eight.
 */
export function modeLabels(book: BethGazoBookDto): Map<string, string> {
  const moded = book.sections.filter(s => s.allowedModeIds.length > 0)
  const common = moded.length === 0
    ? new Set(book.modes.map(m => m.id))
    : moded.slice(1).reduce(
        (set, section) => new Set([...set].filter(id => section.allowedModeIds.includes(id))),
        new Set(moded[0]!.allowedModeIds),
      )
  const labels = new Map<string, string>()
  let number = 0
  for (const mode of [...book.modes].sort((a, b) => Number(a.position) - Number(b.position))) {
    labels.set(mode.id, common.has(mode.id) ? String(++number) : '✶')
  }
  return labels
}

/** Folds case and the dots and breves of romanised names, so "hablo" finds "Ḥablo". */
export function foldForSearch(text: string): string {
  return text.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[ʿʾ'’]/g, '').toLocaleLowerCase()
}

/**
 * The book as the page shows it: only the chants in the chosen mode (all of
 * them when none is chosen), only the melodies whose name or incipit matches the
 * search, and, once either is set, only the sections with something left. With
 * neither set every section stays, recorded or not, so the book keeps its shape.
 */
export function filterBook(
  book: BethGazoBookDto,
  query: string,
  modeId: string | null,
): BethGazoBookSectionDto[] {
  const needle = foldForSearch(query.trim())
  const narrowed = needle.length > 0 || modeId !== null
  return book.sections
    .map(section => ({
      ...section,
      melodies: section.melodies
        .filter(melody => !needle
          || foldForSearch(melody.name).includes(needle)
          || melody.chants.some(c => [c.syriacIncipit, c.syriacIncipitVocalized].some(i => i && i.includes(query.trim()))))
        .map(melody => ({ ...melody, chants: melody.chants.filter(c => modeId === null || c.modeId === modeId) }))
        .filter(melody => melody.chants.length > 0),
    }))
    .filter(section => !narrowed || section.melodies.length > 0)
}

/** How many chants a list of sections holds. */
export function chantCount(sections: readonly BethGazoBookSectionDto[]): number {
  return sections.reduce((sum, s) => sum + s.melodies.reduce((n, m) => n + m.chants.length, 0), 0)
}

/** A mode's place on a melody's line: the chant in its own right, and any shuḥlofe or ḥrone beside it. */
export interface ModeSlot {
  modeId: string
  main: BethGazoBookChantDto
  variants: BethGazoBookChantDto[]
}

/**
 * A melody's chants as one button per mode, in the order the API sends them (mode
 * position, then the plain chant before its variants). A mode recorded only as a
 * variant still gets a button, playing that variant. Chants without a mode are
 * left out: a section without modes lists its chants by incipit instead.
 */
export function modeSlots(chants: readonly BethGazoBookChantDto[]): ModeSlot[] {
  const slots: ModeSlot[] = []
  for (const chant of chants) {
    if (!chant.modeId) continue
    const slot = slots.find(s => s.modeId === chant.modeId)
    if (!slot) slots.push({ modeId: chant.modeId, main: chant, variants: [] })
    else if (slot.main.variantKind !== 'None' && chant.variantKind === 'None') {
      slot.variants.unshift(slot.main)
      slot.main = chant
    }
    else slot.variants.push(chant)
  }
  return slots
}

/** Whether the search found this melody by an incipit rather than by its name, so the page unfolds it. */
export function foundByIncipit(melody: BethGazoBookMelodyDto, query: string): boolean {
  const needle = query.trim()
  if (!needle || foldForSearch(melody.name).includes(foldForSearch(needle))) return false
  return melody.chants.some(c => [c.syriacIncipit, c.syriacIncipitVocalized].some(i => i && i.includes(needle)))
}

/** Where a chant sits in the book, for the ribbon: its section and melody, or null once it is gone. */
export function locateChant(
  book: BethGazoBookDto,
  chantId: string,
): { section: BethGazoBookSectionDto, melody: BethGazoBookMelodyDto, chant: BethGazoBookChantDto } | null {
  for (const section of book.sections) {
    for (const melody of section.melodies) {
      const chant = melody.chants.find(c => c.id === chantId)
      if (chant) return { section, melody, chant }
    }
  }
  return null
}
