import { expect, test } from '@playwright/test'

test.describe('landing page', () => {
  test('renders the app shell with brand, switchers, and a Syriac sample', async ({ page }) => {
    await page.goto('/')

    // The brand link doubles as home. Translations are deferred, so the primary
    // nav holds no public items for an anonymous visitor.
    await expect(page.getByRole('link', { name: 'Sabro' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Translations', exact: true })).toHaveCount(0)
    await expect(page.getByText('letter by letter.')).toBeVisible()

    // The corrected copy: mission-bearing lede, plain-speech cards, and the
    // one unnamed purple tease — nothing about what it is.
    await expect(page.getByText('Sabro means hope', { exact: false })).toBeVisible()
    await expect(page.getByText('A Wordle in Syriac', { exact: false })).toBeVisible()
    await expect(page.getByText("A new game is taking shape. More when it's ready.")).toBeVisible()
    await expect(page.getByText('Shmo')).toHaveCount(0)

    // The script + language switchers are custom dropdowns: a trigger button that
    // opens a listbox of options (replacing the native <select> popup).
    const scriptTrigger = page.getByRole('button', { name: 'Syriac script' })
    await expect(scriptTrigger).toBeVisible()
    await expect(page.getByRole('button', { name: 'Language' })).toBeVisible()

    // Opening the script menu reveals the three scripts as a listbox.
    await scriptTrigger.click()
    await expect(page.getByRole('option', { name: 'Maherboyo' })).toBeVisible()
    await page.keyboard.press('Escape')

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

    // The locale cookie switches rendered text; assert French copy from the
    // home page (the mission lede) is present.
    await expect(page.getByText('Sabro veut dire espoir', { exact: false })).toBeVisible()
  })

  test('renders the Western font when the sabro_script_variant cookie is serto', async ({ page, context, baseURL }) => {
    await context.addCookies([{ name: 'sabro_script_variant', value: 'serto', url: baseURL! }])
    await page.goto('/')

    const syriac = page.locator('span[lang="syc"]').first()
    await expect(syriac).toHaveCSS('font-family', /Noto Sans Syriac Western/)
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
