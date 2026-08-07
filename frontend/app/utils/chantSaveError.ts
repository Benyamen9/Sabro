import type { FetchError } from 'ofetch'
import { classifySaveFailure } from '~/utils/saveError'

/**
 * Which message a failed chant save deserves.
 *
 * On top of the three shared outcomes ({@link classifySaveFailure}), a chant has
 * a fourth: the identity constraint. A melody name recurs across modes and
 * shuḥlofe, so only the triple `(melody, mode, shuḥlofo)` is unique — and a
 * duplicate is a 409 with every individual field perfectly valid. Reporting it
 * as "could not be saved" would send the editor hunting through a form where
 * nothing is wrong.
 */
export function chantSaveErrorKey(error: unknown): string {
  if ((error as FetchError | undefined)?.statusCode === 409) return 'admin.chants.saveConflict'

  const failure = classifySaveFailure(error)
  return failure.kind === 'forbidden'
    ? 'admin.chants.saveForbidden'
    : failure.kind === 'fields'
      ? 'admin.chants.saveFailedFields'
      : 'admin.chants.saveFailed'
}
