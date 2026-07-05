// Renders the Sabro brand assets by screenshotting exact-size elements of
// render-brand.html — the badge/lockup/og marks therefore stay pixel-faithful
// to the app's own CSS recipe rather than being hand-drawn. Run from frontend/:
//   node scripts/brand/render-brand-assets.mjs scripts/brand/render-brand.html .
import { chromium } from 'playwright'
import { mkdir } from 'node:fs/promises'
import path from 'node:path'
import { pathToFileURL } from 'node:url'

const [, , htmlPath, outRoot] = process.argv
if (!htmlPath || !outRoot) throw new Error('usage: node render-brand-assets.mjs <html> <outRoot>')

// [selector, relative output path, deviceScaleFactor, transparent]
const shots = [
  ['#icon-512', 'public/brand/app-icon-512.png', 1, true],
  ['#icon-180', 'public/apple-touch-icon.png', 1, false],
  ['#icon-32', 'public/favicon-32.png', 1, true],
  ['#icon-16', 'public/favicon-16.png', 1, true],
  ['#lockup-light', 'public/brand/logo-lockup-light.png', 2, true],
  ['#lockup-dark', 'public/brand/logo-lockup-dark.png', 2, true],
  ['#seal-light', 'public/brand/seal-light.png', 2, true],
  ['#seal-dark', 'public/brand/seal-dark.png', 2, true],
  ['#og-light', 'public/brand/og-image.png', 1, false],
  ['#og-dark', 'public/brand/og-image-dark.png', 1, false],
]

const browser = await chromium.launch()
for (const dsf of [...new Set(shots.map((s) => s[2]))]) {
  const context = await browser.newContext({ deviceScaleFactor: dsf, viewport: { width: 1400, height: 900 } })
  const page = await context.newPage()
  await page.goto(pathToFileURL(htmlPath).href)
  // The Syriac glyphs must be drawn with the real webfont, not a fallback.
  await page.evaluate(() => document.fonts.ready)
  await page.waitForTimeout(400)
  for (const [selector, rel, shotDsf, transparent] of shots) {
    if (shotDsf !== dsf) continue
    const file = path.join(outRoot, rel)
    await mkdir(path.dirname(file), { recursive: true })
    await page.locator(selector).screenshot({ path: file, omitBackground: transparent })
    console.log(`${selector} -> ${rel} (@${dsf}x${transparent ? ', transparent' : ''})`)
  }
  await context.close()
}
await browser.close()
console.log('done')
