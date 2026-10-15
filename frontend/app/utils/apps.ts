/**
 * The app launcher's list: Sabro and every game, each with its badge and
 * colour (owner, 2026-10-13: the launcher replaces the row of game pills, so
 * the header stays on one line). Each tile keeps its app's DAY colour in both
 * themes — it is that app's mark.
 *
 * Order: Sabro (here) first, then the games in launch order. The same list
 * lives in all five game clients (Meltho, Mno, Shmo, Nahlo, Sofro), each with
 * itself first — when a game is added, add it to all six.
 */
export type AppId = 'sabro' | 'meltho' | 'mno' | 'shmo' | 'nahlo' | 'sofro'

export interface EcosystemApp {
  id: AppId
  name: string
  /** The badge glyph(s); Mno's carries its numeral mark. */
  badge: string
  color: string
  /** Runtime-config key holding the app's URL; null for the app itself. */
  urlKey: 'melthoUrl' | 'mnoUrl' | 'shmoUrl' | 'nahloUrl' | 'sofroUrl' | null
}

export const ECOSYSTEM_APPS: readonly EcosystemApp[] = [
  { id: 'sabro', name: 'Sabro', badge: 'ܣ', color: '#8c2f39', urlKey: null },
  { id: 'meltho', name: 'Meltho', badge: 'ܡ', color: '#1a6b66', urlKey: 'melthoUrl' },
  { id: 'mno', name: 'Mno', badge: 'ܐ݈', color: '#a87514', urlKey: 'mnoUrl' },
  { id: 'shmo', name: 'Shmo', badge: 'ܫ', color: '#587b5a', urlKey: 'shmoUrl' },
  { id: 'nahlo', name: 'Nahlo', badge: 'ܢ', color: '#6d4b9e', urlKey: 'nahloUrl' },
  { id: 'sofro', name: 'Sofro', badge: 'ܣܦ', color: '#2f3639', urlKey: 'sofroUrl' },
]
