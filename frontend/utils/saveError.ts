import type { FetchError } from 'ofetch'

/**
 * Why a save failed, as far as the caller needs to distinguish.
 *
 * These are genuinely different situations and the editor acts differently on
 * each, so telling them apart is the whole point: a refusal has no field to fix,
 * and reporting it as one sends someone hunting through a form that is already
 * complete. That is exactly what happened — every failure shared one message
 * telling the editor to check the highlighted fields.
 */
export type SaveFailure =
  | { kind: 'forbidden' }
  | { kind: 'fields', fields: Record<string, string[]> }
  | { kind: 'unknown' }

export function classifySaveFailure(error: unknown): SaveFailure {
  const fetchError = error as FetchError | undefined

  // Checked first: a refusal carries no field errors to fall back on, and it is
  // the one outcome no amount of editing will change.
  const status = fetchError?.statusCode
  if (status === 401 || status === 403) return { kind: 'forbidden' }

  // `ValidationProblemDetails.errors` — camelCase property paths to messages.
  const problem = fetchError?.data as { errors?: Record<string, string[]> } | undefined
  const fields = problem?.errors
  if (fields && Object.keys(fields).length > 0) return { kind: 'fields', fields }

  return { kind: 'unknown' }
}
