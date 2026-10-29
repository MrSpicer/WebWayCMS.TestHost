import { expect, test } from '@playwright/test';

import { open } from '../support/helpers';

// The CMS default Content-Security-Policy (CspPolicyBuilder; the TestHost configures no "Csp" section).
const DEFAULT_CSP =
  "default-src 'self'; script-src 'self' https://cdn.ckeditor.com; " +
  "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.ckeditor.com; " +
  "img-src 'self' data: https://cdn.ckeditor.com; font-src 'self' https://cdnjs.cloudflare.com; " +
  "connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";

const PUBLIC_PAGES = ['/faq-test', '/zones', '/zones-landing', '/configured', '/articles-test'];

test.describe('security headers', () => {
  for (const url of PUBLIC_PAGES) {
    test(`${url} sends the default security headers`, async ({ page }) => {
      const headers = (await open(page, url)).headers();
      expect(headers['content-security-policy']).toBe(DEFAULT_CSP);
      expect(headers['x-frame-options']).toBe('DENY');
      expect(headers['x-content-type-options']).toBe('nosniff');
      expect(headers['referrer-policy']).toBe('strict-origin-when-cross-origin');
      expect(headers['permissions-policy']).toBe('geolocation=(), microphone=(), camera=()');
    });
  }
});

test.describe('anonymous visitor', () => {
  for (const url of PUBLIC_PAGES) {
    test(`${url} renders no zone-editor affordances`, async ({ page }) => {
      await open(page, url);
      // Edit mode marks zone items with data-version / data-page-node-id and adds widget controls.
      await expect(page.locator('[data-version], [data-page-node-id], [data-zone-id]')).toHaveCount(0);
      await expect(page.getByRole('button', { name: /add widget/i })).toHaveCount(0);
      await expect(page.locator('script[src*="content-zone"]')).toHaveCount(0);
    });
  }

  test('/wadmin redirects to the Identity login page', async ({ request }) => {
    // Not followed: the login page is rate-limited per IP, and auth.setup.ts needs that budget.
    const response = await request.get('/wadmin', { maxRedirects: 0 });
    expect(response.status()).toBe(302);
    expect(response.headers()['location']).toMatch(/\/Identity\/Account\/Login\?ReturnUrl=%2Fwadmin$/);
  });

  test('a draft preview URL is a 404 without an Admin/Editor session', async ({ page }) => {
    await open(page, '/_preview/00000000-0000-0000-0000-000000000001', 404);
  });

  test('an unknown URL is a 404', async ({ page }) => {
    await open(page, '/this-page-does-not-exist', 404);
  });
});
