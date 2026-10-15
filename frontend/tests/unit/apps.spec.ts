import { describe, expect, it } from 'vitest'
import { ECOSYSTEM_APPS } from '~/utils/apps'

describe('the app launcher list', () => {
  it('holds Sabro and all five games, each once, Sabro first', () => {
    const ids = ECOSYSTEM_APPS.map(app => app.id)
    expect(ids).toEqual(['sabro', 'meltho', 'mno', 'shmo', 'nahlo', 'sofro'])
    expect(new Set(ids).size).toBe(ids.length)
  })

  it('links every app but Sabro itself, which is marked as here', () => {
    for (const app of ECOSYSTEM_APPS) {
      expect(app.urlKey === null).toBe(app.id === 'sabro')
    }
  })

  it('every linked app has its URL in the runtime config', () => {
    const config = useRuntimeConfig().public as Record<string, unknown>
    for (const app of ECOSYSTEM_APPS) {
      if (app.urlKey) expect(config[app.urlKey]).toMatch(/^https?:\/\//)
    }
  })

  it("gives Sofro the two-letter badge, so it never reads as Sabro's ܣ", () => {
    const sofro = ECOSYSTEM_APPS.find(app => app.id === 'sofro')!
    const sabro = ECOSYSTEM_APPS.find(app => app.id === 'sabro')!
    expect(sofro.badge).toBe('ܣܦ')
    expect(sofro.badge).not.toBe(sabro.badge)
  })
})
