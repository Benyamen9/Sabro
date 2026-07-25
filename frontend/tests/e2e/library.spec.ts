import { expect, test } from '@playwright/test'

test.describe('library page', () => {
  test('renders the unified library with its toggle and gracefully reports API failure', async ({ page }) => {
    await page.goto('/library')

    await expect(page.getByRole('heading', { name: 'Word library' })).toBeVisible()
    // The dictionary state is the default: the switch is off, labelled "Dictionary".
    const toggle = page.getByRole('switch', { name: /Meltho/ })
    await expect(toggle).toBeVisible()
    await expect(toggle).toHaveAttribute('aria-checked', 'false')
    await expect(page.getByText('Dictionary')).toBeVisible()
    // Recent sort only applies once filtered to Meltho words — hidden in this state.
    await expect(page.getByRole('button', { name: /Recently shown/ })).not.toBeVisible()
    // No backend is running during e2e, so the fetch fails — the page must
    // surface the i18n failure message instead of crashing.
    await expect(page.getByText('Could not load the word library.')).toBeVisible()
  })

  test('switches to the Meltho filter via the toggle', async ({ page }) => {
    await page.goto('/library')
    // The switch has no native fallback (unlike the old tab links' real <a href>), so a click
    // before Vue hydration attaches its listener is a silent no-op — wait for hydration first.
    await page.waitForLoadState('networkidle')
    await page.getByRole('switch', { name: /Meltho/ }).click()

    await expect(page).toHaveURL(/view=meltho/)
    await expect(page.getByRole('switch', { name: /Meltho/ })).toHaveAttribute('aria-checked', 'true')
    await expect(page.getByText('From Meltho')).toBeVisible()
    // Recent sort becomes available once the Meltho filter is on.
    await expect(page.getByRole('button', { name: /Recently shown/ })).toBeVisible()
    // The fetch fails just as gracefully in this state.
    await expect(page.getByText('Could not load the word library.')).toBeVisible()
  })

  test('remembers the toggle across a fresh visit with no query string', async ({ page }) => {
    await page.goto('/library')
    // The switch has no native fallback (unlike the old tab links' real <a href>), so a click
    // before Vue hydration attaches its listener is a silent no-op — wait for hydration first.
    await page.waitForLoadState('networkidle')
    await page.getByRole('switch', { name: /Meltho/ }).click()
    await expect(page).toHaveURL(/view=meltho/)

    await page.goto('/library')

    await expect(page.getByRole('switch', { name: /Meltho/ })).toHaveAttribute('aria-checked', 'true')
  })
})
