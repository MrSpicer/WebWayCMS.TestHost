import { existsSync } from 'node:fs';

import { expect, request, test as setup } from '@playwright/test';

import { ADMIN_STATE } from '../support/admin';

// Credentials match WebWayCMS.TestHost/docker-compose.yml (a throwaway test stack).
const email = process.env.ADMIN_EMAIL ?? 'admin@example.com';
const password = process.env.ADMIN_PASSWORD ?? 'XKX%aeOtKcte7jBcHn';

setup('log in as the seeded admin', async ({ page, baseURL }) => {
  // The Identity endpoints allow 5 requests per minute per client IP (GETs included), so reuse a
  // saved session while it still opens /wadmin instead of logging in on every run.
  if (existsSync(ADMIN_STATE)) {
    const saved = await request.newContext({ baseURL, storageState: ADMIN_STATE });
    const probe = await saved.get('/wadmin', { maxRedirects: 0 });
    await saved.dispose();
    if (probe.status() === 200) return;
  }

  await page.goto('/Identity/Account/Login');
  await page.locator('input[name="Input.Email"]').fill(email);
  await page.locator('input[name="Input.Password"]').fill(password);
  await page.locator('#login-submit').click();
  await page.waitForURL((url) => !url.pathname.startsWith('/Identity/Account/Login'));

  await page.goto('/wadmin');
  await expect(page).toHaveURL(/\/wadmin/);
  await page.context().storageState({ path: ADMIN_STATE });
});
