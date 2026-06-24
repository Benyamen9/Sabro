import { expect, test } from '@playwright/test'

test.describe('library page', () => {
  test('renders the library heading and gracefully reports API failure', async ({ page }) => {
    await page.goto('/library')

    await expect(page.getByRole('heading', { name: 'Meltho word library' })).toBeVisible()
    // No backend is running during e2e, so listWords fails — the page must surface
    // the i18n failure message instead of crashing.
    await expect(page.getByText('Could not load the word library.')).toBeVisible()
  })
})
