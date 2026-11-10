import { expect, test } from '@playwright/test'

// No backend and no sign-in run during e2e, so these are a signed-out reader's marks: kept on the
// device and listed from there.
const MARKS = [
  { book: 'lexicon', itemKey: '3f2b8c1e-4d5a-4b6c-9e7f-0a1b2c3d4e5f', markedAt: new Date().toISOString(), label: { title: 'king', syriac: 'ܡܠܟܐ' } },
  { book: 'beth-gazo', itemKey: '7a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d', markedAt: '2026-10-28T09:00:00.000Z', label: { title: 'Hablo l\'itokh', subtitle: 'Tlithoyo' } },
]

test.describe('Library marks', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript((marks) => {
      if (!localStorage.getItem('sabro:library:marks')) localStorage.setItem('sabro:library:marks', JSON.stringify(marks))
    }, MARKS)
  })

  test('lists what the reader marked, by book, each row leading to its item', async ({ page }) => {
    await page.goto('/library')
    await page.getByRole('button', { name: /Marked 2/ }).click()

    const panel = page.getByRole('dialog', { name: 'Marked' })
    await expect(panel.getByRole('link', { name: /king/ })).toHaveAttribute('href', `/library/lexicon/${MARKS[0]!.itemKey}`)
    await expect(panel.getByRole('link', { name: /Hablo l'itokh · Tlithoyo/ })).toHaveAttribute('href', `/library/beth-gazo?chant=${MARKS[1]!.itemKey}`)

    await panel.getByRole('button', { name: 'Beth Gazo' }).click()
    await expect(panel.getByRole('link')).toHaveCount(1)
    await panel.getByRole('button', { name: 'Beth Ṣalme' }).click()
    await expect(panel.getByText('Nothing marked in this book.')).toBeVisible()
  })

  test('unmarks on closing the panel, and Undo puts the mark back', async ({ page }) => {
    await page.goto('/library')
    await page.getByRole('button', { name: /Marked 2/ }).click()
    const panel = page.getByRole('dialog', { name: 'Marked' })

    await panel.getByRole('button', { name: 'Keep king marked' }).click()
    // Unticked, the row stays until the panel closes.
    await expect(panel.getByRole('link', { name: /king/ })).toBeVisible()
    await expect(panel.getByText(/tick it again to keep it/)).toBeVisible()

    await panel.getByRole('button', { name: 'Close' }).click()
    await expect(page.getByRole('button', { name: /Marked 1/ })).toBeVisible()
    await expect(page.getByText('1 mark removed')).toBeVisible()

    await page.getByRole('button', { name: 'Undo' }).click()
    await expect(page.getByRole('button', { name: /Marked 2/ })).toBeVisible()
    const stored = await page.evaluate(() => JSON.parse(localStorage.getItem('sabro:library:marks') ?? '[]').length)
    expect(stored).toBe(2)
  })
})
