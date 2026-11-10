import { expect, test } from '@playwright/test'

test.describe('Grammar book', () => {
  test('lists its tables by tense and opens one', async ({ page }) => {
    await page.goto('/library/grammar')

    await expect(page.getByRole('heading', { name: 'The verb, tense by tense' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'The past' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'The future' })).toBeVisible()

    await page.getByRole('main').locator('a[href="/library/grammar/present-ktab"]').click()
    await expect(page).toHaveURL(/\/library\/grammar\/present-ktab$/)
    const table = page.getByRole('table', { name: 'The present · Sound root, with ptoho' })
    await expect(table.getByRole('row')).toHaveCount(13)
    await expect(table.getByRole('columnheader', { name: 'Joined' })).toBeVisible()
    // The tense dots are drawn by the page, in red, once the book's font has loaded.
    await expect(table.locator('.tense-dot').first()).toBeVisible()
  })

  test('moves between tenses, keeps the place, and can be marked', async ({ page }) => {
    await page.goto('/library/grammar/past-gbo')

    await page.getByRole('navigation', { name: 'Tense' }).getByRole('link', { name: 'The present' }).click()
    await expect(page).toHaveURL(/\/library\/grammar\/present-gbo$/)
    // The button answers only once the page has hydrated, so press until it says so.
    await expect(async () => {
      await page.getByRole('button', { name: /^Mark The present/ }).click({ timeout: 1000 })
      await expect(page.getByRole('button', { name: /^Remove the mark from The present/ })).toBeVisible({ timeout: 1000 })
    }).toPass()

    await page.goto('/library')
    await expect(page.getByRole('button', { name: /Marked 1/ })).toBeVisible()
    await page.goto('/library/grammar')
    await expect(page.getByRole('link', { name: /Where you left off/ })).toHaveAttribute('href', '/library/grammar/present-gbo')
  })

  test('says so when a table does not exist', async ({ page }) => {
    const response = await page.goto('/library/grammar/past-nothing')

    expect(response?.status()).toBe(404)
    await expect(page.getByText('This table is not in the Grammar.')).toBeVisible()
  })
})
