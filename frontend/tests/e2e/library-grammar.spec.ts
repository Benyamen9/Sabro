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
})
