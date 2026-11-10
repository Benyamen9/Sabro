import { grammarTable, type GrammarTable } from '~/data/grammar'
import type { LibraryPlaceLabel } from '~/utils/libraryPlaces'

/** Names for the Grammar book's tables, in the reader's language. */
export function useGrammar() {
  const { t } = useI18n()

  const tenseName = (table: GrammarTable) => t(`grammar.tenses.${table.tense}`)
  const categoryName = (table: GrammarTable) => t(`grammar.categories.${table.category}`)

  /**
   * How a table is named on the shelf's cover and in the Marked panel. Sabro sends no label for a
   * grammar page (its pages live here), so the hub names it from the slug.
   */
  function label(slug: string): LibraryPlaceLabel | null {
    const table = grammarTable(slug)
    if (!table) return null
    return { title: tenseName(table), syriac: table.model, subtitle: categoryName(table) }
  }

  return { tenseName, categoryName, label }
}
