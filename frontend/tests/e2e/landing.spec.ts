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
