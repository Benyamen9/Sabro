import type { ContentArea } from '~/types/api'

/**
 * How many playable words the pool is aiming at.
 *
 * Launch needed roughly 30–50 and the pool passed that; the owner's stated
 * target is 100–200, so the bar is set in the middle of it. One constant rather
 * than a number written into a template, because the moment it appears in two
 * places they disagree.
 */
export const PLAYABLE_POOL_TARGET = 150

export interface AdminOverview {
  /** Proposals waiting on the Owner, or null when this person cannot see them. */
  pendingProposals: number | null
  /** Published entries flagged playable, or null if the count could not be read. */
  playableWords: number | null
  poolTarget: number
}

/**
 * The state of things, for the backoffice's front page.
 *
 * Deliberately not a to-do list. It reports what is genuinely waiting on a
 * decision and where the pool stands — it does not count unfinished work and
 * present it as a demand, because the owner's own answer is that the backoffice
 * is not currently where their hours go. A dashboard that nags about work
 * somebody has chosen not to do gets ignored, and then so does the one number
 * on it that mattered.
 *
 * Every count degrades to null rather than failing the page. The proposals count
 * is Owner-only and 403s for other staff; the word count comes through
 * Meilisearch, which can be down while Postgres is fine. Neither is worth a
 * broken hub.
 */
export function useAdminOverview() {
  const { list: listProposals } = useProposals()
  const { list: listEntries } = useLexiconAdmin()
  const { isOwner, canViewBackoffice } = useMyAccess()

  const overview = useState<AdminOverview>('sabro-admin-overview', () => ({
    pendingProposals: null,
    playableWords: null,
    poolTarget: PLAYABLE_POOL_TARGET,
  }))
  const loading = ref(false)

  async function countPendingProposals(): Promise<number | null> {
    if (!isOwner.value) return null
    try {
      return (await listProposals('Pending', 1, 1)).total
    }
    catch {
      return null
    }
  }

  async function countPlayableWords(): Promise<number | null> {
    if (!canViewBackoffice('Lexicon' as ContentArea)) return null
    try {
      return (await listEntries({ page: 1, pageSize: 1, status: 'Published', playableInMeltho: true })).total
    }
    catch {
      return null
    }
  }

  async function refresh() {
    loading.value = true
    try {
      // Independent counts, so they are fetched together rather than in sequence.
      const [pendingProposals, playableWords] = await Promise.all([
        countPendingProposals(),
        countPlayableWords(),
      ])
      overview.value = { pendingProposals, playableWords, poolTarget: PLAYABLE_POOL_TARGET }
    }
    finally {
      loading.value = false
    }
  }

  return { overview, loading, refresh }
}
