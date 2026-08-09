/**
 * The daily circuit — ecosystem-wide "what have I played today" state.
 *
 * One cookie shared across *.sabro.be (the same pattern as the locale and
 * script-variant cookies): `{ d: 'YYYY-MM-DD', g: ['meltho', …] }`, where the
 * date is the UTC puzzle date the daily games are keyed by. The games mark
 * themselves when a day's play ends; the home page reads it to badge played
 * doors and point the primary CTA at the next unplayed game. Mirrors the
 * useDailyCircuit composables in Meltho, Mno and Shmo — keep the shape in sync.
 */
export const CIRCUIT_GAMES = ['meltho', 'mno', 'shmo', 'nahlo'] as const
export type CircuitGame = (typeof CIRCUIT_GAMES)[number]

/**
 * The games currently offered as a hand-off destination.
 *
 * Nahlo is deployed and reachable, but its treasury holds no recordings yet, so
 * `/play/nahlo/today` answers 409 and the game cannot serve a puzzle. Handing a
 * player there ends their circuit on a closed door, so it is not offered.
 *
 * It deliberately stays a `CircuitGame`: Nahlo still marks itself played, and
 * nothing about the cookie's vocabulary changes. This is a temporary omission,
 * not a removal.
 *
 * ⚠️ Put 'nahlo' back here — in EVERY copy of this file (five: Sabro, Meltho,
 * Mno, Shmo, Nahlo) — as soon as the recordings land.
 */
const CIRCUIT_HANDOFF: readonly CircuitGame[] = ['meltho', 'mno', 'shmo']

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

  /** The first circuit game not yet played on `date`, or null when the day is done. */
  function nextUnplayed(date: string = circuitToday()): CircuitGame | null {
    const games = playedOn(date)
    return CIRCUIT_HANDOFF.find((g) => !games.includes(g)) ?? null
  }

  return { hasPlayed, nextUnplayed }
}
