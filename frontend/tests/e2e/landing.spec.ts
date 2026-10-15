import { expect, test } from '@playwright/test'

test.describe('landing page', () => {
  test('renders the app shell with brand, switchers, and a Syriac sample', async ({ page }) => {
    // The game deck deals itself every 2 s; reduced motion holds it still, so
    // what the page shows is deterministic while we assert on it.
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/')

    // The brand link doubles as home. Translations are deferred, so the primary
    // nav holds no public items for an anonymous visitor.
    await expect(page.getByRole('link', { name: 'Sabro' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'Translations', exact: true })).toHaveCount(0)
    await expect(page.getByText('letter by letter.')).toBeVisible()

    // The corrected copy: mission-bearing lede, plain-speech cards.
    await expect(page.getByText('Sabro means hope', { exact: false })).toBeVisible()
    await expect(page.getByText('A Wordle in Syriac', { exact: false })).toBeVisible()
    // All five apps are in the game deck, one badge each.
    for (const game of ['Meltho', 'Mno', 'Shmo', 'Nahlo', 'Sofro']) {
      await expect(page.getByRole('button', { name: `Show ${game}` })).toBeVisible()
    }

    // The script + language switchers are custom dropdowns: a trigger button that
    // opens a listbox of options (replacing the native <select> popup).
    const scriptTrigger = page.getByRole('button', { name: 'Syriac script' })
    await expect(scriptTrigger).toBeVisible()
    await expect(page.getByRole('button', { name: 'Language' })).toBeVisible()

    // Opening the script menu reveals the three scripts as a listbox. The trigger has no
    // native fallback — a click before Vue's handler attaches is a silent no-op — so wait for
    // hydration first (prior toBeVisible() assertions only prove the SSR HTML rendered, not
    // that client JS has taken over).
    await page.waitForLoadState('networkidle')
    await scriptTrigger.click()
    await expect(page.getByRole('option', { name: 'Maherboyo' })).toBeVisible()
    await page.keyboard.press('Escape')

    // The UserMenu degrades to an "Auth not configured" notice rather than
    // rendering a broken sign-in button. The unconfigured state is pinned by
    // playwright.config's webServer.env, not inherited from whatever .env the
    // machine happens to have — this assertion used to pass only on machines
    // without one.
    await expect(page.getByText('Auth not configured')).toBeVisible()

    const syriac = page.locator('span[lang="syc"]').first()
    await expect(syriac).toBeVisible()
    await expect(syriac).toHaveAttribute('dir', 'rtl')
  })

  test('the app launcher lists Sabro and every game, Sabro marked as here', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('networkidle')

    const apps = page.getByRole('navigation', { name: "Sabro's apps" })
    await page.getByRole('button', { name: "Sabro's apps" }).click()
    await expect(apps).toBeVisible()
    for (const name of ['Meltho', 'Mno', 'Shmo', 'Nahlo', 'Sofro']) {
      await expect(apps.getByRole('link', { name: new RegExp(name) })).toBeVisible()
    }
    await expect(apps.locator('[aria-current="page"]')).toContainText('Sabro')
  })

  test('the header stays on one line on a narrow phone', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 })
    await page.goto('/')

    const brand = page.getByRole('link', { name: 'Sabro', exact: true })
    const launcher = page.getByRole('button', { name: "Sabro's apps" })
    for (const el of [brand, launcher]) await expect(el).toBeVisible()
    const middle = async (el: typeof brand) => { const b = (await el.boundingBox())!; return b.y + b.height / 2 }
    expect(Math.abs((await middle(launcher)) - (await middle(brand)))).toBeLessThan(12)
    // Nothing on the page pushes it sideways — header or game deck.
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(320)

    // On phones the page links move from the header into the launcher panel.
    const nav = page.getByRole('navigation', { name: 'Primary navigation' })
    await expect(nav).toBeHidden()
    await page.waitForLoadState('networkidle')
    await launcher.click()
    const apps = page.getByRole('navigation', { name: "Sabro's apps" })
    await expect(apps.getByRole('link', { name: /Library/ })).toBeVisible()
  })

  test('on a wide screen the page links stay in the header', async ({ page }) => {
    await page.goto('/')
    const nav = page.getByRole('navigation', { name: 'Primary navigation' })
    await expect(nav.getByRole('link', { name: 'Library', exact: true })).toBeVisible()
    await page.waitForLoadState('networkidle')
    await page.getByRole('button', { name: "Sabro's apps" }).click()
    const apps = page.getByRole('navigation', { name: "Sabro's apps" })
    await expect(apps.getByRole('link', { name: /Library/ })).toBeHidden()
  })

  test('the game deck follows the game on top', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/')
    await page.waitForLoadState('networkidle')

    const deck = page.getByRole('region', { name: 'Sabro’s games' })
    // A fresh visitor starts on Meltho: its heading, description and button.
    await expect(deck.getByRole('heading', { level: 2, name: /Meltho/ })).toBeVisible()
    await expect(deck.getByRole('link', { name: 'Play today’s word →' })).toBeVisible()

    // The section's own arrow deals the next card, and the side follows it.
    await deck.getByRole('button', { name: 'Next game' }).click()
    await expect(deck.getByRole('heading', { level: 2, name: /Mno/ })).toBeVisible()

    // A badge jumps straight to its game — Sofro, the one with no daily puzzle.
    await deck.getByRole('button', { name: 'Show Sofro' }).click()
    await expect(deck.getByRole('heading', { level: 2, name: /Sofro/ })).toBeVisible()
    await expect(deck.getByText('Writing · Any time')).toBeVisible()
    await expect(deck.getByRole('link', { name: 'Start writing →' })).toHaveAttribute('href', /sofro/)

    await deck.getByRole('button', { name: 'Previous game' }).click()
    await expect(deck.getByRole('heading', { level: 2, name: /Nahlo/ })).toBeVisible()
  })

  test('the game deck opens on the next unplayed game', async ({ page, context, baseURL }) => {
    const today = new Date().toISOString().slice(0, 10)
    await context.addCookies([{
      name: 'sabro_daily_played',
      value: encodeURIComponent(JSON.stringify({ d: today, g: ['meltho', 'mno'] })),
      url: baseURL!,
    }])
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/')

    const deck = page.getByRole('region', { name: 'Sabro’s games' })
    await expect(deck.getByRole('heading', { level: 2, name: /Shmo/ })).toBeVisible()
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
