import { describe, expect, it, vi } from 'vitest'
import { createTokenCache, TOKEN_MARGIN_MS, tokenExpiry } from '~/utils/tokenCache'

// A token shaped like Logto's: header.payload.signature, base64url, `exp` in seconds.
function tokenExpiringAt(ms: number, extra = '') {
  const payload = btoa(JSON.stringify({ exp: ms / 1000, sub: `player${extra}` }))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `eyJhbGciOiJFUzM4NCJ9.${payload}.signature`
}

describe('tokenExpiry', () => {
  it('reads exp from the payload, in ms', () => {
    expect(tokenExpiry(tokenExpiringAt(1_800_000_000_000))).toBe(1_800_000_000_000)
  })

  it('gives null for anything that is not a readable token', () => {
    expect(tokenExpiry('opaque')).toBeNull()
    expect(tokenExpiry('a.!!!.c')).toBeNull()
    expect(tokenExpiry(`a.${btoa('{"sub":"x"}')}.c`)).toBeNull()
  })
})

describe('createTokenCache', () => {
  const start = 1_800_000_000_000

  it('fetches once and reuses the token until shortly before it expires', async () => {
    let clock = start
    const token = tokenExpiringAt(start + 3_600_000)
    const fetchToken = vi.fn(async () => token)
    const cache = createTokenCache(fetchToken, () => clock)

    expect(await cache.get()).toBe(token)
    clock = start + 3_600_000 - TOKEN_MARGIN_MS - 1
    expect(await cache.get()).toBe(token)
    expect(fetchToken).toHaveBeenCalledTimes(1)

    clock += 1
    await cache.get()
    expect(fetchToken).toHaveBeenCalledTimes(2)
  })

  it('shares one fetch between calls made at the same time', async () => {
    const fetchToken = vi.fn(async () => tokenExpiringAt(start + 3_600_000))
    const cache = createTokenCache(fetchToken, () => start)
    const [a, b, c] = await Promise.all([cache.get(), cache.get(), cache.get()])
    expect(a).toBe(b)
    expect(b).toBe(c)
    expect(fetchToken).toHaveBeenCalledTimes(1)
  })

  it('never keeps a missing or unreadable token', async () => {
    const fetchToken = vi.fn<() => Promise<string | null>>()
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce('opaque')
      .mockResolvedValueOnce(tokenExpiringAt(start + 3_600_000))
    const cache = createTokenCache(fetchToken, () => start)
    expect(await cache.get()).toBeNull()
    expect(await cache.get()).toBe('opaque')
    await cache.get()
    await cache.get()
    expect(fetchToken).toHaveBeenCalledTimes(3)
  })

  it('fetches again after clear', async () => {
    const fetchToken = vi.fn(async () => tokenExpiringAt(start + 3_600_000))
    const cache = createTokenCache(fetchToken, () => start)
    await cache.get()
    cache.clear()
    await cache.get()
    expect(fetchToken).toHaveBeenCalledTimes(2)
  })

  it('fetches again after a failed fetch', async () => {
    const fetchToken = vi.fn<() => Promise<string | null>>()
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce(tokenExpiringAt(start + 3_600_000))
    const cache = createTokenCache(fetchToken, () => start)
    await expect(cache.get()).rejects.toThrow('offline')
    expect(await cache.get()).not.toBeNull()
  })
})
