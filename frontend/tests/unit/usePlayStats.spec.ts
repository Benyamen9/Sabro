import { describe, expect, it } from 'vitest'
import type { GameResultDto } from '~/types/api'
import { computeGameStats } from '~/composables/usePlayStats'

// The derivation is game-agnostic; these specs exercise it through Meltho.
const computeMelthoStats = (results: GameResultDto[]) => computeGameStats(results, 'meltho')

// Minimal result factory — only the fields the stats derivation reads matter.
function result(playedOn: string, solved: boolean, attempts: number, gameId = 'meltho'): GameResultDto {
  return {
    id: `id-${playedOn}`,
    logtoUserId: 'u1',
    gameId,
    playedOn,
    solved,
    attempts,
    detailJson: null,
    createdAt: `${playedOn}T00:00:00Z`,
    updatedAt: `${playedOn}T00:00:00Z`,
  } as GameResultDto
}

describe('computeMelthoStats', () => {
  it('returns an empty, zeroed shape when there are no games', () => {
    const s = computeMelthoStats([])
    expect(s.played).toBe(0)
    expect(s.wins).toBe(0)
    expect(s.winRate).toBe(0)
    expect(s.currentStreak).toBe(0)
    expect(s.maxStreak).toBe(0)
    expect(s.distribution).toEqual([0, 0, 0, 0, 0, 0])
    expect(s.lastPlayed).toBeNull()
  })

  it('counts played, wins, losses and win rate', () => {
    const s = computeMelthoStats([
      result('2026-06-01', true, 3),
      result('2026-06-02', false, 6),
      result('2026-06-03', true, 4),
      result('2026-06-04', true, 2),
    ])
    expect(s.played).toBe(4)
    expect(s.wins).toBe(3)
    expect(s.losses).toBe(1)
    expect(s.winRate).toBe(75)
  })

  it('buckets wins into the guess distribution and averages them', () => {
    const s = computeMelthoStats([
      result('2026-06-01', true, 1),
      result('2026-06-02', true, 3),
      result('2026-06-03', true, 3),
      result('2026-06-04', false, 6), // loss is excluded from the distribution
    ])
    expect(s.distribution).toEqual([1, 0, 2, 0, 0, 0])
    expect(s.maxBucket).toBe(2)
    expect(s.averageGuesses).toBeCloseTo((1 + 3 + 3) / 3)
  })

  it('breaks the current streak on a loss and counts only the trailing run', () => {
    const s = computeMelthoStats([
      result('2026-06-01', true, 2),
      result('2026-06-02', false, 6),
      result('2026-06-03', true, 2),
      result('2026-06-04', true, 3),
    ])
    expect(s.currentStreak).toBe(2)
  })

  it('breaks streaks when a calendar day is skipped', () => {
    const s = computeMelthoStats([
      result('2026-06-01', true, 2),
      result('2026-06-02', true, 2),
      // 06-03 skipped
      result('2026-06-04', true, 2),
      result('2026-06-05', true, 2),
    ])
    expect(s.maxStreak).toBe(2)
    expect(s.currentStreak).toBe(2)
  })

  it('tracks the longest historical streak independently of the current one', () => {
    const s = computeMelthoStats([
      result('2026-06-01', true, 2),
      result('2026-06-02', true, 2),
      result('2026-06-03', true, 2),
      result('2026-06-04', false, 6),
      result('2026-06-05', true, 2),
    ])
    expect(s.maxStreak).toBe(3)
    expect(s.currentStreak).toBe(1)
    expect(s.lastPlayed).toBe('2026-06-05')
  })

  it('ignores results from other games', () => {
    const s = computeMelthoStats([
      result('2026-06-01', true, 2),
      result('2026-06-02', true, 1, 'shmo'),
    ])
    expect(s.played).toBe(1)
    expect(s.wins).toBe(1)
  })

  it('derives per-game stats from one mixed result set', () => {
    const mixed = [
      result('2026-06-01', true, 2),
      result('2026-06-01', true, 4, 'mno'),
      result('2026-06-02', false, 6, 'mno'),
    ]
    const mno = computeGameStats(mixed, 'mno')
    expect(mno.played).toBe(2)
    expect(mno.wins).toBe(1)
    expect(computeGameStats(mixed, 'meltho').played).toBe(1)
  })

  it('is order-independent (sorts by date before deriving)', () => {
    const s = computeMelthoStats([
      result('2026-06-04', true, 3),
      result('2026-06-01', true, 2),
      result('2026-06-03', true, 2),
      result('2026-06-02', true, 2),
    ])
    expect(s.currentStreak).toBe(4)
    expect(s.maxStreak).toBe(4)
  })
})
