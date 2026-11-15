import { expect, test } from '@playwright/test'

test.describe('Grammar book', () => {
  test('lists its chapters and opens one', async ({ page }) => {
    await page.goto('/library/grammar')

    await expect(page.getByRole('heading', { name: 'The rules of the verb' })).toBeVisible()
    await expect(page.getByRole('main').locator('ol a')).toHaveCount(6)

    await page.getByRole('link', { name: /The tense dots/ }).click()
    await expect(page).toHaveURL(/\/library\/grammar\/tense-dots$/)
    await expect(page.getByRole('heading', { name: 'The tense dots', level: 1 })).toBeVisible()
    // The examples' dots are drawn by the page, in red, once the book's font has loaded.
    await expect(page.locator('.tense-dot').first()).toBeVisible()
    // No backend runs during e2e, so no verb is published: the model verbs are shown unlinked.
    await expect(page.getByText(/Their pages in the Lexicon/)).toBeVisible()
  })

  test('keeps the place and can be marked', async ({ page }) => {
    await page.goto('/library/grammar/present')

    // The button answers only once the page has hydrated, so press until it says so.
    await expect(async () => {
      await page.getByRole('button', { name: 'Mark The present' }).click({ timeout: 1000 })
      await expect(page.getByRole('button', { name: 'Remove the mark from The present' })).toBeVisible({ timeout: 1000 })
    }).toPass()

    await page.goto('/library')
    await expect(page.getByRole('button', { name: /Marked 1/ })).toBeVisible()
    await page.goto('/library/grammar')
    await expect(page.getByRole('link', { name: /Where you left off/ })).toHaveAttribute('href', '/library/grammar/present')
  })

  test('says so when a chapter does not exist', async ({ page }) => {
    const response = await page.goto('/library/grammar/past-ktab')

    expect(response?.status()).toBe(404)
    await expect(page.getByText('This chapter is not in the Grammar.')).toBeVisible()
  })

  test('a verb in the text opens its page in the Lexicon, on its tense', async ({ page }) => {
    // One model verb is published. The chapter is reached by a click, so the browser asks for the
    // verbs itself and this stand-in answers (no backend runs during e2e).
    const id = '5b1c2d3e-4f5a-4b6c-8d7e-9f0a1b2c3d4e'
    await page.route('**/dictionary?*', route => route.fulfill({
      json: { items: [{ id, syriacUnvocalized: 'ܟܬܒ', grammaticalCategory: 'Verb', meanings: [] }], total: 1, page: 1, pageSize: 100 },
    }))
    await page.goto('/library/grammar')
    // A click before the page has hydrated would load the chapter from the server, which has no
    // Sabro to ask: wait until the app is mounted, so the click stays in the browser.
    await page.waitForFunction(() => '__vue_app__' in (document.querySelector('#__nuxt') ?? {}))
    await page.getByRole('link', { name: /The present/ }).click()
    await expect(page).toHaveURL(/\/library\/grammar\/present$/)

    const text = page.getByRole('main').locator('ol').first()
    // A form of the present opens the verb on the present; one quoted from the past, on the past.
    await expect(text.locator('a', { hasText: 'ܟ݁ܳܬ݂ܒ݁ܳܐ' }).first()).toHaveAttribute('href', `/library/lexicon/${id}?tense=present#conjugation`)
    await expect(text.locator('a', { hasText: 'ܟ݁ܶܬ݂ܒ݁ܶܬ݂' }).first()).toHaveAttribute('href', `/library/lexicon/${id}?tense=past#conjugation`)
    // A verb that is not published stays plain text.
    await expect(page.getByRole('main').locator('a', { hasText: 'ܩܳܐܶܡ' })).toHaveCount(0)
    await expect(page.getByText('Each opens the verb in the Lexicon')).toBeVisible()
  })
})
