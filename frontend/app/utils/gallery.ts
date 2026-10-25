import type { HistoricalFigureListItem, ShmoCollectionCardDto } from '~/types/api'

/** How far the player has come with a figure. "locked" means not met yet. */
export type GalleryTier = 'found' | 'met' | 'locked'

/** The filter pills above the collection. */
export type GalleryFilter = 'all' | 'found' | 'illuminated' | 'met' | 'locked'

/** One card of Beth Ṣalme: a roster figure joined with the player's progress. */
export interface GalleryFigure extends Omit<HistoricalFigureListItem, 'era'> {
  /** Signed century, as a number (the generated type also admits a numeric string). */
  era: number
  /** Card number, through time: the oldest figures come first. Zero-padded to three. */
  no: string
  tier: GalleryTier
  timesMet: number
  firstFoundOn: string | null
  fewestAttempts: number | null
  illuminated: boolean
  descriptions: { language: string, text: string }[]
}

/**
 * Joins the public roster with the player's collection. Every roster figure gets
 * a card; one the player has not met is locked. Sorted A–Z, numbered by era so
 * the numbers read as a timeline (ties broken by name, so they never shift
 * between visits).
 */
export function buildGallery(
  roster: readonly HistoricalFigureListItem[],
  cards: readonly ShmoCollectionCardDto[],
): GalleryFigure[] {
  const byId = new Map(cards.map(card => [card.historicalFigureId, card]))
  const numbers = new Map(
    [...roster]
      .sort((a, b) => Number(a.era) - Number(b.era) || a.name.localeCompare(b.name))
      .map((figure, index) => [figure.id, String(index + 1).padStart(3, '0')]),
  )

  return [...roster]
    .sort((a, b) => a.name.localeCompare(b.name))
    .map((figure) => {
      const card = byId.get(figure.id)
      return {
        ...figure,
        era: Number(figure.era),
        no: numbers.get(figure.id)!,
        tier: card ? (card.status === 'Found' ? 'found' : 'met') : 'locked',
        timesMet: Number(card?.timesMet ?? 0),
        firstFoundOn: card?.firstFoundOn ?? null,
        fewestAttempts: card?.fewestAttempts == null ? null : Number(card.fewestAttempts),
        illuminated: card?.illuminated ?? false,
        descriptions: card?.descriptions ?? [],
      } satisfies GalleryFigure
    })
}

/** Whether a card passes the chosen pill and the search box. */
export function matchesGallery(figure: GalleryFigure, filter: GalleryFilter, query: string): boolean {
  const pill = filter === 'all'
    || (filter === 'illuminated' ? figure.illuminated : figure.tier === filter)
  const q = query.trim().toLocaleLowerCase()
  return pill && (q.length === 0 || figure.name.toLocaleLowerCase().includes(q))
}

/** The description in the reader's language, else English, else whatever exists. */
export function pickDescription(
  descriptions: readonly { language: string, text: string }[],
  locale: string,
): string | null {
  return (
    descriptions.find(d => d.language === locale)
    ?? descriptions.find(d => d.language === 'en')
    ?? descriptions[0]
  )?.text ?? null
}

/** Category to the token prefix of its card tone (see --figure-* in main.css). */
export function toneOf(category: HistoricalFigureListItem['category']): 'ot' | 'nt' | 'pa' {
  if (category === 'BiblicalOldTestament') return 'ot'
  if (category === 'BiblicalNewTestament') return 'nt'
  return 'pa'
}
