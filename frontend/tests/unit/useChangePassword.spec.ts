import { describe, expect, it } from 'vitest'
import { MIN_PASSWORD_LENGTH, validatePasswordChange } from '~/composables/useChangePassword'

const valid = { current: 'oldpassword1', next: 'newpassword1', confirm: 'newpassword1' }

describe('validatePasswordChange', () => {
  it('accepts a well-formed change', () => {
    expect(validatePasswordChange(valid)).toBeNull()
  })

  it('requires every field', () => {
    expect(validatePasswordChange({ ...valid, current: '' })).toBe('required')
    expect(validatePasswordChange({ ...valid, next: '' })).toBe('required')
    expect(validatePasswordChange({ ...valid, confirm: '' })).toBe('required')
  })

  it('rejects a new password shorter than the minimum', () => {
    const short = 'a'.repeat(MIN_PASSWORD_LENGTH - 1)
    expect(validatePasswordChange({ current: 'oldpassword1', next: short, confirm: short })).toBe('tooShort')
  })

  it('rejects when confirmation does not match', () => {
    expect(validatePasswordChange({ ...valid, confirm: 'different1' })).toBe('mismatch')
  })

  it('rejects when the new password equals the current one', () => {
    expect(validatePasswordChange({ current: 'samepassword1', next: 'samepassword1', confirm: 'samepassword1' }))
      .toBe('sameAsCurrent')
  })

  it('checks length before mismatch', () => {
    // A short new password that also mismatches should report the length first.
    expect(validatePasswordChange({ current: 'oldpassword1', next: 'short', confirm: 'nope' })).toBe('tooShort')
  })
})
