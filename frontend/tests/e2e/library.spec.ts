import { expect, test } from '@playwright/test'

test.describe('library page', () => {
  test('renders the tabbed library and gracefully reports API failure', async ({ page }) => {
    await page.goto('/library')

    await expect(page.getByRole('heading', { name: 'Word library' })).toBeVisible()
    // Both views are present as tabs; the dictionary is the default.
    await expect(page.getByRole('link', { name: 'Dictionary' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'From Meltho' })).toBeVisible()
    // No backend is running during e2e, so the fetch fails — the page must
    // surface the i18n failure message instead of crashing.
    await expect(page.getByText('Could not load the word library.')).toBeVisible()
  })

  test('switches to the Meltho view via its tab', async ({ page }) => {
    await page.goto('/library')
    await page.getByRole('link', { name: 'From Meltho' }).click()

    await expect(page).toHaveURL(/view=meltho/)
    // The game view brings its sort chips; its fetch fails just as gracefully.
    await expect(page.getByRole('button', { name: /Recently shown/ })).toBeVisible()
    await expect(page.getByText('Could not load the word library.')).toBeVisible()
  })
})
