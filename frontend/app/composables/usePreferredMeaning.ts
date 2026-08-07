import type { MelthoPuzzleMeaningDto } from '~/types/api'

/**
 * Returns a picker that resolves the gloss for the active UI locale, falling back
 * to English and then to the first available meaning. Shared by the library list
 * and detail pages.
 */
export function usePreferredMeaning() {
  // Explicitly the global scope. These composables run from many components and
  // want the app-wide locale, never a component-local one — and since @nuxtjs/i18n
  // v10 a bare useI18n() outside a component that owns a scope warns
  // "Not found parent scope. use the global scope." and then does this anyway.
  const { locale } = useI18n({ useScope: 'global' })

  return (meanings: readonly MelthoPuzzleMeaningDto[] | undefined): string => {
    if (!meanings || meanings.length === 0) return ''
    const match
      = meanings.find(m => m.language === locale.value)
        ?? meanings.find(m => m.language === 'en')
        ?? meanings[0]
    return match?.text ?? ''
  }
}
