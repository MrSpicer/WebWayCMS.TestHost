import { defineConfig, devices } from '@playwright/test';

/**
 * End-to-end suite for the TestHost. It asserts what the content seeded by `contentseed/` and
 * `Fixtures/` renders through the host's views, so it must run against a stack booted on a FRESH
 * database (`./scripts/TearDownIntegrationhost.sh -v && ./scripts/StartIntegrationHost.sh`).
 * Every test is read-only except the `*.write.spec.ts` files, which run in the `writes` project after
 * all read-only tests and sweep what they create, so the suite can be re-run against the same stack.
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:45847',
    trace: 'retain-on-failure',
  },
  projects: [
    // Logs the seeded admin in once and saves the session for admin.spec.ts (the login endpoint is
    // rate-limited and the account locks after 5 failures, so never log in per test).
    { name: 'setup', testMatch: /auth\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
      testIgnore: /\.write\.spec\.ts/,
    },
    // Specs that create, publish and delete content: only after every read-only spec, which asserts the
    // seeded state exactly (page lists, navbar), has finished.
    {
      name: 'writes',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['chromium'],
      testMatch: /\.write\.spec\.ts/,
    },
  ],
});
