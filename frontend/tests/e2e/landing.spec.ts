import { expect, test } from '@playwright/test'

test.describe('landing page', () => {
  test('renders the app shell with brand, nav, switchers, and a Syriac sample', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('link', { name: 'Sabro' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Home' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Translations' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Lexicon' })).toBeVisible()

    await expect(page.getByRole('combobox', { name: 'Syriac script' })).toBeVisible()
    await expect(page.getByRole('combobox', { name: 'Language' })).toBeVisible()

    // With no Logto config set in the test env, the UserMenu degrades to a
    // "Auth not configured" notice rather than rendering a broken sign-in
    // button.
    await expect(page.getByText('Auth not configured')).toBeVisible()

    const syriac = page.locator('span[lang="syc"]').first()
    await expect(syriac).toBeVisible()
    await expect(syriac).toHaveAttribute('dir', 'rtl')
  })

  test('renders in French when the sabro_locale cookie is fr', async ({ page, context, baseURL }) => {
    await context.addCookies([{ name: 'sabro_locale', value: 'fr', url: baseURL! }])
    await page.goto('/')

    await expect(page.getByRole('link', { name: 'Accueil' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Traductions' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Lexique' })).toBeVisible()
  })

  test('renders Serto when the sabro_script_variant cookie is serto', async ({ page, context, baseURL }) => {
    await context.addCookies([{ name: 'sabro_script_variant', value: 'serto', url: baseURL! }])
    await page.goto('/')

    const syriac = page.locator('span[lang="syc"]').first()
    await expect(syriac).toHaveCSS('font-family', /Serto Jerusalem/)
  })
})

test.describe('translations page', () => {
  test('renders the translations heading and gracefully reports API failure', async ({ page }) => {
    await page.goto('/translations')

    await expect(page.getByRole('heading', { name: 'Translations' })).toBeVisible()
    // No backend is running during e2e, so listSources fails — the page must
    // surface the i18n failure message instead of crashing.
    await expect(page.getByText('Could not load translations.')).toBeVisible()
  })
})
