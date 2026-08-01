import type { SuggestedEditDto, SuggestedEditStatus, SuggestedEditTargetType } from '~/types/api'

/**
 * Where to go to apply an accepted proposal.
 *
 * Field proposals name their target module. Prose targets (`Segment`,
 * `Annotation`) belong to the deferred Translations module and have no backoffice
 * page yet, so they get `null` — no link at all rather than one that 404s.
 */
export function editLinkFor(
  targetType: SuggestedEditTargetType,
  targetId: string,
): string | null {
  switch (targetType) {
    case 'LexiconEntry':
      return `/admin/lexicon/${targetId}`
    case 'HistoricalFigure':
      return `/admin/historical-figures/${targetId}`
    default:
      return null
  }
}

/**
 * The Owner's review queue: corrections proposed by area reviewers, awaiting a
 * decision.
 *
 * Accepting records a decision — it does not write to the entry. The Owner then
 * applies the change through the entry's own edit page, which is what keeps
 * "suggestions never modify content directly" true.
 */
export function useProposals() {
  const api = useSabroApi()

  function list(status: SuggestedEditStatus = 'Pending', page = 1, pageSize = 50) {
    return api<{ items: SuggestedEditDto[], total: number }>('/suggested-edits', {
      query: { status, page, pageSize },
    })
  }

  /**
   * Accepts a proposal. `acceptChangedTarget` re-sends the decision after the
   * server has refused it because the field moved since the proposal was filed —
   * never send it on the first attempt, or the guard is defeated by the client
   * that was supposed to honour it.
   */
  function accept(id: string, note?: string, acceptChangedTarget = false) {
    return api<SuggestedEditDto>(`/suggested-edits/${id}/accept`, {
      method: 'POST',
      body: { note, acceptChangedTarget },
    })
  }

  function reject(id: string, note?: string) {
    return api<SuggestedEditDto>(`/suggested-edits/${id}/reject`, {
      method: 'POST',
      body: { note },
    })
  }

  return { list, accept, reject, editLinkFor }
}
