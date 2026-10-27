import { expect, test } from '@playwright/test'

test.describe('Beth Gazo', () => {
  test('is linked from the header and reports a failed book gracefully', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('navigation', { name: 'Primary navigation' }).getByRole('link', { name: 'Beth Gazo' }).click()

    await expect(page).toHaveURL(/\/beth-gazo$/)
    await expect(page.getByRole('heading', { name: 'The treasury of chants' })).toBeVisible()
    await expect(page.getByRole('link', { name: /Play today’s Nahlo/ })).toBeVisible()
    // No backend runs during e2e, so the book fetch fails: the page says so
    // instead of crashing or showing an empty book.
    await expect(page.getByText('The chants could not be loaded.')).toBeVisible()
  })
})
