import { expect, test } from '@playwright/test'

test.describe('Beth Ṣalme', () => {
  test('is linked from the header and reports a failed roster gracefully', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('navigation', { name: 'Primary navigation' }).getByRole('link', { name: 'Beth Ṣalme' }).click()

    await expect(page).toHaveURL(/\/beth-salme$/)
    await expect(page.getByRole('heading', { name: 'The gallery' })).toBeVisible()
    await expect(page.getByRole('link', { name: /Play today’s Shmo/ })).toBeVisible()
    // No backend runs during e2e, so the roster fetch fails: the page says so
    // instead of crashing or showing an empty grid.
    await expect(page.getByText('The figures could not be loaded.')).toBeVisible()
  })
})
