import { describe, expect, it } from 'vitest'
import { classifySaveFailure } from '~/utils/saveError'

function fetchError(statusCode: number, data?: unknown) {
  return Object.assign(new Error('failed'), { statusCode, data })
}

describe('classifySaveFailure', () => {
  it('calls a refusal a refusal, not a validation problem', () => {
    // The reported bug: a reviewer's 403 came back as "check the highlighted
    // fields" on a form whose fields were all filled in.
    expect(classifySaveFailure(fetchError(403))).toEqual({ kind: 'forbidden' })
    expect(classifySaveFailure(fetchError(401))).toEqual({ kind: 'forbidden' })
  })

  it('treats a refusal as a refusal even if a body comes with it', () => {
    // A 403 body is a ProblemDetails, but if one ever carried an `errors` bag,
    // marking fields would still be the wrong thing to tell the editor.
    const error = fetchError(403, { errors: { syriacUnvocalized: ['Required.'] } })
    expect(classifySaveFailure(error)).toEqual({ kind: 'forbidden' })
  })

  it('surfaces the fields the server named', () => {
    const error = fetchError(400, { errors: { syriacUnvocalized: ['Required.'] } })
    expect(classifySaveFailure(error)).toEqual({
      kind: 'fields',
      fields: { syriacUnvocalized: ['Required.'] },
    })
  })

  it('falls back when there is nothing specific to say', () => {
    expect(classifySaveFailure(fetchError(500))).toEqual({ kind: 'unknown' })
    expect(classifySaveFailure(fetchError(400, { errors: {} }))).toEqual({ kind: 'unknown' })
    expect(classifySaveFailure(fetchError(400, {}))).toEqual({ kind: 'unknown' })
    expect(classifySaveFailure(new Error('network'))).toEqual({ kind: 'unknown' })
    expect(classifySaveFailure(undefined)).toEqual({ kind: 'unknown' })
  })
})
