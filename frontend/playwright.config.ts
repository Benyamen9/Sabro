/// <reference types="node" />
import { defineConfig, devices } from '@playwright/test'

const PORT = 3100
const baseURL = `http://localhost:${PORT}`

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [
    ['list'],
    ['html', { open: 'never' }],
    ...(process.env.CI ? [['github'] as ['github']] : []),
  ],
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: `npm run dev -- --port ${PORT}`,
    url: baseURL,
    // Always boot our own server rather than adopting whatever is already on
    // this port. A reused server carries someone else's environment, which is
    // precisely the thing the `env` block below exists to control — adopting it
    // would quietly reintroduce the difference this config is pinning down.
    reuseExistingServer: false,
    timeout: 120_000,
    // Pin the auth state instead of inheriting it. The suite asserts the
    // unconfigured-Logto degradation ("Auth not configured"), which held in CI
    // only because CI has no .env: any developer with a working local .env got
    // a real sign-in button and a red test that said nothing about their work.
    // Nuxt's dotenv loading does not overwrite variables already present in the
    // process environment, so setting them here wins over .env.
    env: {
      NUXT_PUBLIC_LOGTO_ENDPOINT: '',
      NUXT_LOGTO_ENDPOINT: '',
    },
  },
})
