import { GRAMMAR_VERBS, grammarChapter, grammarVerbFor, type GrammarChapter } from '~/data/grammar'
import type { DictionaryEntryListItem, PagedResult } from '~/types/api'
import type { LibraryPlaceLabel } from '~/utils/libraryPlaces'

/** Names for the Grammar book's chapters in the reader's language, and its way into the Lexicon. */
export function useGrammar() {
  const { t } = useI18n()
  const api = useSabroApi()

  const chapterTitle = (chapter: GrammarChapter) => t(`grammar.chapters.${chapter.slug}.title`)
  const chapterLine = (chapter: GrammarChapter) => t(`grammar.chapters.${chapter.slug}.line`)

  /**
   * How a chapter is named on the shelf's cover and in the Marked panel. Sabro sends no label for a
   * grammar page (its pages live in the hub), so the hub names it from the slug.
   */
  function label(slug: string): LibraryPlaceLabel | null {
    const chapter = grammarChapter(slug)
    return chapter ? { title: chapterTitle(chapter) } : null
  }

  /**
   * The model verbs as they stand in the Lexicon: each with its entry's id once the Owner has
   * published it, so a chapter can link to the verb's page. A verb not published yet has no id and
   * is shown without a link.
   */
  function useVerbs() {
    return useAsyncData('grammar-verbs', async () => {
      const ids = new Map<string, string>()
      try {
        const verbs = await api<PagedResult<DictionaryEntryListItem>>('/dictionary', { query: { category: 'Verb', page: 1, pageSize: 100 } })
        for (const entry of verbs.items) {
          const verb = grammarVerbFor(entry)
          if (verb && !ids.has(verb.verb)) ids.set(verb.verb, entry.id)
        }
      }
      catch {
        // Sabro unreachable: the chapter still reads, its verbs unlinked.
      }
      return GRAMMAR_VERBS.map(verb => ({ verb: verb.verb, id: ids.get(verb.verb) ?? null }))
    }, { default: () => GRAMMAR_VERBS.map(verb => ({ verb: verb.verb, id: null as string | null })) })
  }

  return { chapterTitle, chapterLine, label, useVerbs }
}
