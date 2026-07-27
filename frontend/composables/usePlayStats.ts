import type { GameResultDto, PagedResult } from '~/types/api'

// Meltho and Mno give six tries; results carry attempts 1..6 on a win and
// attempts == max on a loss (solved=false). Shmo has no attempt cap (unlimited
// guesses, "give up" instead of losing) — its wins can carry any attempt
// count, so the histogram's last bucket becomes an explicit overflow ("6+")
// rather than silently implying "exactly 6". The guess distribution buckets
// the wins by attempt count — the familiar Wordle histogram.
const MAX_ATTEMPTS = 6
const MAX_PAGE_SIZE = 200

export interface GameStats {
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
  /** True when the last distribution bucket is an overflow ("6+") rather than
   *  an exact count — only possible for unlimited-guess games like Shmo. */
  overflowLastBucket: boolean
}

function emptyStats(): GameStats {
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
    overflowLastBucket: false,
  }
}

// Whole days between two yyyy-mm-dd dates (b - a). Parsed as UTC so DST never
// shifts the count.
function dayGap(a: string, b: string): number {
  const ms = Date.parse(`${b}T00:00:00Z`) - Date.parse(`${a}T00:00:00Z`)
  return Math.round(ms / 86_400_000)
}

/**
 * Derive one game's stats from a player's raw results (no server aggregate).
 * `unlimitedGuesses` (Shmo only) marks that attempts have no hard cap: wins
 * past MAX_ATTEMPTS still land in the last bucket, but `overflowLastBucket`
 * tells the UI to label it "6+" instead of implying every one of those wins
 * took exactly 6 guesses.
 */
export function computeGameStats(
  results: GameResultDto[],
  gameId: string,
  options: { unlimitedGuesses?: boolean } = {},
): GameStats {
  const games = results
    .filter(r => r.gameId === gameId)
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
      winningGuessTotal += Number(g.attempts)
      if (options.unlimitedGuesses && Number(g.attempts) > MAX_ATTEMPTS) {
        stats.overflowLastBucket = true
      }
    }
    else {
      stats.losses += 1
    }
  }

  stats.winRate = Math.round((stats.wins / stats.played) * 100)
  stats.averageGuesses = stats.wins > 0 ? winningGuessTotal / stats.wins : 0
  stats.maxBucket = Math.max(...stats.distribution)
  if (options.unlimitedGuesses) stats.maxAttempts = Infinity
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
 * Loads the signed-in player's play stats, derived client-side from
 * /play/results/me (Sabro stores raw results; streaks and aggregates are not
 * persisted). One fetch serves every game: results are kept raw and each
 * game's stats are derived from them. Fetches every page so streaks span the
 * full history.
 */
export function usePlayStats() {
  const { isConfigured, isSignedIn } = useAuth()
  const api = useSabroApi()

  const results = useState<GameResultDto[] | null>('sabro-play-results', () => null)
  const loading = useState<boolean>('sabro-play-stats-loading', () => false)
  const loaded = useState<boolean>('sabro-play-stats-loaded', () => false)

  const melthoStats = computed(() => (results.value ? computeGameStats(results.value, 'meltho') : null))
  const mnoStats = computed(() => (results.value ? computeGameStats(results.value, 'mno') : null))
  const shmoStats = computed(() => (
    results.value ? computeGameStats(results.value, 'shmo', { unlimitedGuesses: true }) : null
  ))

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
      results.value = all
      loaded.value = true
    }
    catch {
      // Signed out / network error: leave results null so the page shows nothing.
    }
    finally {
      loading.value = false
    }
  }

  return { melthoStats, mnoStats, shmoStats, loading, loaded, load }
}
