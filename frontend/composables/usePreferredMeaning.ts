import type { MelthoPuzzleMeaningDto } from '~/types/api'

/**
 * Returns a picker that resolves the gloss for the active UI locale, falling back
 * to English and then to the first available meaning. Shared by the library list
 * and detail pages.
 */
export function usePreferredMeaning() {
  const { locale } = useI18n()

  return (meanings: readonly MelthoPuzzleMeaningDto[] | undefined): string => {
    if (!meanings || meanings.length === 0) return ''
    const match
      = meanings.find(m => m.language === locale.value)
        ?? meanings.find(m => m.language === 'en')
        ?? meanings[0]
    return match?.text ?? ''
  }
}
