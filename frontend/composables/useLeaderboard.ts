import type { MelthoLeaderboardDto } from '~/types/api'

/**
 * Loads the Meltho leaderboard for the signed-in user: the top players by longest
 * streak (opted-in only) plus the caller's own standing. Signed-in only — the
 * endpoint requires a token, so this no-ops when signed out.
 */
export function useLeaderboard() {
  const { isConfigured, isSignedIn } = useAuth()
  const api = useSabroApi()

  const board = useState<MelthoLeaderboardDto | null>('sabro-leaderboard', () => null)
  const loading = useState<boolean>('sabro-leaderboard-loading', () => false)
  const loaded = useState<boolean>('sabro-leaderboard-loaded', () => false)

  async function load(force = false) {
    if (!isConfigured.value || !isSignedIn.value) return
    if (loaded.value && !force) return
    loading.value = true
    try {
      board.value = await api<MelthoLeaderboardDto>('/play/meltho/leaderboard')
      loaded.value = true
    }
    catch {
      // Signed out / network error: leave the board null so the card shows nothing.
    }
    finally {
      loading.value = false
    }
  }

  return { board, loading, loaded, load }
}
