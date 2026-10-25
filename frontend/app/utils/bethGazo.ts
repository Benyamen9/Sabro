import type { BethGazoDescriptionDto } from '~/types/api'

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
