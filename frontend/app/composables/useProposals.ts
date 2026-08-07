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
   *
   * `apply` writes the value onto the entry as part of accepting, instead of
   * only recording the decision. The staleness guard applies either way: with
   * `apply` the refusal happens before anything is written.
   */
  function accept(id: string, note?: string, acceptChangedTarget = false, apply = false) {
    return api<SuggestedEditDto>(`/suggested-edits/${id}/accept`, {
      method: 'POST',
      body: { note, acceptChangedTarget, apply },
    })
  }

  function reject(id: string, note?: string) {
    return api<SuggestedEditDto>(`/suggested-edits/${id}/reject`, {
      method: 'POST',
      body: { note },
    })
  }

  function getById(id: string) {
    return api<SuggestedEditDto>(`/suggested-edits/${id}`)
  }

  /**
   * The fields this target type accepts proposals for, from the server. Never
   * hardcode a copy: the list lives with the module that owns the entity, and a
   * second copy drifts silently — offering a field the API refuses, or hiding one
   * it would have taken.
   */
  function proposableFields(targetType: SuggestedEditTargetType) {
    return api<string[]>(`/suggested-edits/fields/${targetType}`)
  }

  /** Files a reviewer's proposed value for one field. */
  function proposeField(input: {
    targetType: SuggestedEditTargetType
    targetId: string
    field: string
    proposedValue: string
    rationale?: string
  }) {
    return api<SuggestedEditDto>('/suggested-edits/field', {
      method: 'POST',
      body: input,
    })
  }

  return { list, getById, accept, reject, proposableFields, proposeField, editLinkFor }
}
