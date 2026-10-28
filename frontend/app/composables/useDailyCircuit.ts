/**
 * The daily circuit — ecosystem-wide "what have I played today" state.
 *
 * One cookie shared across *.sabro.be (the same pattern as the locale and
 * script-variant cookies): `{ d: 'YYYY-MM-DD', g: ['meltho', …] }`, where the
 * date is the UTC puzzle date the daily games are keyed by. The games mark
 * themselves when a day's play ends; the home page reads it to badge played
 * doors and point the primary CTA at the next unplayed game.
 * Six copies, one per app (Sabro, Meltho, Mno, Shmo, Nahlo, Sofro): keep the
 * cookie shape AND both game lists in sync across all of them.
 */
export const CIRCUIT_GAMES = ['meltho', 'mno', 'shmo', 'nahlo', 'sofro'] as const
export type CircuitGame = (typeof CIRCUIT_GAMES)[number]

/**
 * The games currently offered as a hand-off destination.
 *
 * All four puzzles since 2026-10-12, when Nahlo's first melody group was
 * published, and Sofro's word of the day last since 2026-10-28 (owner: a fifth
 * daily game, written after the four). Kept separate from `CIRCUIT_GAMES` so a
 * game that cannot serve a puzzle (its `/today` answering 409) can be withheld
 * from the hand-off without changing the cookie's vocabulary.
 */
const CIRCUIT_HANDOFF: readonly CircuitGame[] = ['meltho', 'mno', 'shmo', 'nahlo', 'sofro']

interface CircuitState {
  d: string
  g: string[]
}

const cookieKey = 'sabro_daily_played'

/** Today's puzzle date — UTC, matching how the daily puzzles are keyed. */
export function circuitToday(): string {
  return new Date().toISOString().slice(0, 10)
}

export function useDailyCircuit() {
  const cookieDomain = useRuntimeConfig().public.cookieDomain
  const cookie = useCookie<CircuitState | null>(cookieKey, {
    default: () => null,
    // Only today's state means anything — two days covers every timezone.
    maxAge: 60 * 60 * 48,
    sameSite: 'lax',
    domain: cookieDomain || undefined,
    secure: Boolean(cookieDomain),
  })

  function playedOn(date: string): string[] {
    return cookie.value && cookie.value.d === date ? cookie.value.g : []
  }

  function hasPlayed(game: CircuitGame, date: string = circuitToday()): boolean {
    return playedOn(date).includes(game)
  }

  /** Record a day of one game as played. Idempotent; a new date starts a fresh
   *  circuit. The hub calls it with a signed-in player's saved results, so a
   *  round played on another device counts here too. */
  function markPlayed(game: CircuitGame, date: string) {
    const games = playedOn(date)
    if (!games.includes(game)) cookie.value = { d: date, g: [...games, game] }
  }

  /** The first circuit game not yet played on `date`, or null when the day is done. */
  function nextUnplayed(date: string = circuitToday()): CircuitGame | null {
    const games = playedOn(date)
    return CIRCUIT_HANDOFF.find((g) => !games.includes(g)) ?? null
  }

  return { markPlayed, hasPlayed, nextUnplayed }
}
