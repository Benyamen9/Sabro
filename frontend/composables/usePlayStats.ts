import type { GameResultDto, PagedResult } from '~/types/api'

// Meltho gives six tries; its results carry attempts 1..6 on a win and
// attempts == max on a loss (solved=false). The guess distribution buckets the
// wins by attempt count — the familiar Wordle histogram.
const MELTHO_GAME_ID = 'meltho'
const MAX_ATTEMPTS = 6
const MAX_PAGE_SIZE = 200

export interface MelthoStats {
  played: number
  wins: number
  losses: number
  winRate: number // 0..100, rounded
  currentStreak: number
  maxStreak: number
  averageGuesses: number // over wins; 0 when none
  distribution: number[] // length MAX_ATTEMPTS, index 0 = solved in 1 try
  maxBucket: number // largest distribution value, for histogram scaling
  lastPlayed: string | null // ISO date (yyyy-mm-dd) of the most recent game
  maxAttempts: number
}

function emptyStats(): MelthoStats {
  return {
    played: 0,
    wins: 0,
    losses: 0,
    winRate: 0,
    currentStreak: 0,
    maxStreak: 0,
    averageGuesses: 0,
    distribution: Array.from({ length: MAX_ATTEMPTS }, () => 0),
    maxBucket: 0,
    lastPlayed: null,
    maxAttempts: MAX_ATTEMPTS,
  }
}

// Whole days between two yyyy-mm-dd dates (b - a). Parsed as UTC so DST never
// shifts the count.
function dayGap(a: string, b: string): number {
  const ms = Date.parse(`${b}T00:00:00Z`) - Date.parse(`${a}T00:00:00Z`)
  return Math.round(ms / 86_400_000)
}

/** Derive the Meltho stats from a player's raw results (no server aggregate). */
export function computeMelthoStats(results: GameResultDto[]): MelthoStats {
  const games = results
    .filter(r => r.gameId === MELTHO_GAME_ID)
    .sort((a, b) => a.playedOn.localeCompare(b.playedOn)) // oldest → newest

  if (games.length === 0) return emptyStats()

  const stats = emptyStats()
  stats.played = games.length

  let winningGuessTotal = 0
  for (const g of games) {
    if (g.solved) {
      stats.wins += 1
      const bucket = Math.min(Math.max(Number(g.attempts), 1), MAX_ATTEMPTS)
      const idx = bucket - 1
      stats.distribution[idx] = (stats.distribution[idx] ?? 0) + 1
      winningGuessTotal += bucket
    }
    else {
      stats.losses += 1
    }
  }

  stats.winRate = Math.round((stats.wins / stats.played) * 100)
  stats.averageGuesses = stats.wins > 0 ? winningGuessTotal / stats.wins : 0
  stats.maxBucket = Math.max(...stats.distribution)
  stats.lastPlayed = games[games.length - 1]!.playedOn

  // Longest run of consecutive calendar days that were all solved.
  let run = 0
  let previousDate: string | null = null
  for (const g of games) {
    if (g.solved && previousDate !== null && dayGap(previousDate, g.playedOn) === 1) {
      run += 1
    }
    else if (g.solved) {
      run = 1
    }
    else {
      run = 0
    }
    stats.maxStreak = Math.max(stats.maxStreak, run)
    previousDate = g.playedOn
  }

  // Current streak: trailing run of consecutive-day wins ending at the latest game.
  let current = 0
  for (let i = games.length - 1; i >= 0; i--) {
    const g = games[i]!
    if (!g.solved) break
    if (i === games.length - 1) {
      current = 1
    }
    else if (dayGap(g.playedOn, games[i + 1]!.playedOn) === 1) {
      current += 1
    }
    else {
      break
    }
  }
  stats.currentStreak = current

  return stats
}

/**
 * Loads the signed-in player's Meltho stats, derived client-side from
 * /play/results/me (Sabro stores raw results; streaks and aggregates are not
 * persisted). Fetches every page so streaks span the full history.
 */
export function usePlayStats() {
  const { isConfigured, isSignedIn } = useAuth()
  const api = useSabroApi()

  const stats = useState<MelthoStats | null>('sabro-play-stats', () => null)
  const loading = useState<boolean>('sabro-play-stats-loading', () => false)
  const loaded = useState<boolean>('sabro-play-stats-loaded', () => false)

  async function load() {
    if (!isConfigured.value || !isSignedIn.value || loaded.value) return
    loading.value = true
    try {
      const all: GameResultDto[] = []
      let page = 1
      // Guard against a runaway loop; 200 × 50 pages is far beyond any real history.
      for (let guard = 0; guard < 50; guard++) {
        const result = await api<PagedResult<GameResultDto>>('/play/results/me', {
          query: { page, pageSize: MAX_PAGE_SIZE },
        })
        all.push(...result.items)
        if (all.length >= result.total || result.items.length === 0) break
        page += 1
      }
      stats.value = computeMelthoStats(all)
      loaded.value = true
    }
    catch {
      // Signed out / network error: leave stats null so the page shows nothing.
    }
    finally {
      loading.value = false
    }
  }

  return { stats, loading, loaded, load }
}
