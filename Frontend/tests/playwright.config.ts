import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  globalSetup: './helpers/global-setup.ts',
  workers: 1,
  timeout: 30000,
  expect: { timeout: 10000 },
  retries: process.env.CI ? 1 : 0,
  use: {
    baseURL: process.env.E2E_FRONTEND_URL ?? 'http://localhost:5500',
    browserName: 'chromium',
    ...devices['Desktop Chrome'],
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'on-first-retry',
    ignoreHTTPSErrors: true
  },
  projects: [
    {
      name: 'auth',
      testMatch: /auth\.spec\.ts|smoke\.spec\.ts/
    },
    {
      name: 'app',
      testIgnore: /auth\.spec\.ts|smoke\.spec\.ts/,
    }
  ]
});
