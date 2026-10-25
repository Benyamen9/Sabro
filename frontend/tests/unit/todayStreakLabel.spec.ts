import { describe, expect, it } from 'vitest'

// The home page's streak shows from the first day, so its label must read
// "1 day", never "1 days" — in every locale.
describe('the "Today" streak label', () => {
  it('is singular for one day and plural after', () => {
    const { t } = useNuxtApp().$i18n
    expect(t('home.today.streak', { days: 1 }, 1)).toBe('1 day')
    expect(t('home.today.streak', { days: 5 }, 5)).toBe('5 days')
  })
})
