import { defineConfig, devices } from '@playwright/test';
import { gatewayUrl } from './helpers/urls';

// Set SKIP_WEBSERVER=1 when the stack is already running (e.g. in CI, where
// the workflow starts docker compose in its own step).
const skipWebServer = process.env.SKIP_WEBSERVER === '1';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : [['list'], ['html']],
  timeout: 30_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: gatewayUrl,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: skipWebServer
    ? undefined
    : {
        command: 'node scripts/start-stack.mjs',
        url: `${gatewayUrl}/healthz`,
        reuseExistingServer: true,
        timeout: 10 * 60 * 1000,
        stdout: 'pipe',
        stderr: 'pipe',
      },
});
