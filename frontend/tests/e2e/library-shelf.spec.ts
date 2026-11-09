import { expect, test } from '@playwright/test'

test.describe('Library shelf', () => {
  test('offers each book as a cover leading into it', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('navigation', { name: 'Primary navigation' }).getByRole('link', { name: 'Library' }).click()

    await expect(page).toHaveURL(/\/library$/)
    await expect(page.getByRole('heading', { name: 'Choose a book' })).toBeVisible()
    const main = page.getByRole('main')
    await expect(main.getByRole('link', { name: /Lexicon/ })).toHaveAttribute('href', '/library/lexicon')
    await expect(main.getByRole('link', { name: /Beth Gazo/ })).toHaveAttribute('href', '/library/beth-gazo')
    await expect(main.getByRole('link', { name: /Beth Ṣalme/ })).toHaveAttribute('href', '/library/beth-salme')
  })

  test('sends the old addresses to their new places', async ({ page }) => {
    await page.goto('/beth-gazo')
    await expect(page).toHaveURL(/\/library\/beth-gazo$/)

    await page.goto('/beth-salme')
    await expect(page).toHaveURL(/\/library\/beth-salme$/)

    await page.goto('/library/3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f')
    await expect(page).toHaveURL(/\/library\/lexicon\/3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f$/)

    await page.goto('/library?view=meltho')
    await expect(page).toHaveURL(/\/library\/lexicon\?view=meltho$/)
  })
})
