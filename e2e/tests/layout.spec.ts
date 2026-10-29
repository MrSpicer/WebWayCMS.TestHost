import { expect, test } from '@playwright/test';

import { open, zone, zoneItems } from '../support/helpers';
import { pages } from '../support/seed';

// Every public page uses the host _Layout: brand link, a RouteNavigation navbar built from the route
// table (published pages are named after their title on first publish; [CmsRoute] attributes carry
// their NavigationName), and the global SiteFooter zone seeded with FaqCount.

const LAYOUT_PAGES = ['/', '/faq-test', '/zones', '/zones-landing', '/page-test', '/page-test/nested', '/configured', '/articles-test', '/code-test'];

// Top-level navbar entries (order follows the route table, so they are compared as a set).
const TOP_LEVEL = [
  ['Home', '/'],
  ['FAQ Test', '/faq-test'],
  ['Zone Showcase', '/zones'],
  ['Zone Landing', '/zones-landing'],
  ['Configured Page', '/configured'],
  ['Articles Test', '/articles-test'],
];

function sortPairs(pairs: string[][]): string[][] {
  return [...pairs].sort((a, b) => a[1].localeCompare(b[1]));
}

test.describe('site layout', () => {
  for (const url of LAYOUT_PAGES) {
    test(`${url} renders the brand and the SiteFooter global zone`, async ({ page }) => {
      await open(page, url);
      await expect(page.locator('a.navbar-brand')).toHaveText('Integration-Host');
      await expect(page.locator('a.navbar-brand')).toHaveAttribute('href', '/');
      const footerZone = zone(page.locator('footer'), 'SiteFooter');
      await expect(zoneItems(footerZone)).toHaveCount(1);
      await expect(footerZone.getByTestId('faq-count')).toHaveAttribute('data-count', '6');
    });
  }

  test('the navbar lists every seeded page and the named [CmsRoute] routes', async ({ page }) => {
    await open(page, '/');
    const nav = page.locator('header ul.navbar-nav');

    const plain = await nav.locator(':scope > li.nav-item:not(.dropdown) > a.nav-link').evaluateAll((els) =>
      els.map((e) => [e.textContent!.trim(), e.getAttribute('href')!]),
    );
    expect(sortPairs(plain)).toEqual(sortPairs(TOP_LEVEL));

    const dropdowns = nav.locator(':scope > li.nav-item.dropdown');
    await expect(dropdowns).toHaveCount(2);

    // A page's parent is its route's path prefix: Nested Page sits under Page Test Hub.
    const hub = dropdowns.filter({ has: page.locator('a.dropdown-toggle', { hasText: 'Page Test Hub' }) });
    const hubItems = await hub.locator('a.dropdown-item').evaluateAll((els) =>
      els.map((e) => [e.textContent!.trim(), e.getAttribute('href')!]),
    );
    expect(hubItems).toEqual([
      ['Page Test Hub', '/page-test'],
      ['Nested Page', '/page-test/nested'],
    ]);

    const codeTest = dropdowns.filter({ has: page.locator('a.dropdown-toggle', { hasText: 'CmsRoute Tests' }) });
    const codeItems = await codeTest.locator('a.dropdown-item').evaluateAll((els) =>
      els.map((e) => [e.textContent!.trim(), e.getAttribute('href')!]),
    );
    expect(codeItems[0]).toEqual(['CmsRoute Tests', '/code-test']);
    expect(sortPairs(codeItems.slice(1))).toEqual(
      sortPairs([
        ['Simple', '/code-test/simple'],
        ['Multi A', '/code-test/multi/a'],
        ['Multi B', '/code-test/multi/b'],
      ]),
    );
  });

  test('every seeded page title appears in the navbar exactly once', async ({ page }) => {
    await open(page, '/');
    const links = page.locator('header ul.navbar-nav a:not(.dropdown-toggle)');
    for (const p of pages) {
      await expect(links.filter({ hasText: new RegExp(`^\\s*${p.title}\\s*$`) })).toHaveCount(1);
    }
  });

  test('the navbar omits admin, reserved and parameterized routes', async ({ page }) => {
    await open(page, '/');
    const hrefs = await page.locator('header ul.navbar-nav a').evaluateAll((els) => els.map((e) => e.getAttribute('href')!));
    expect(hrefs.filter((h) => h.startsWith('/wadmin'))).toEqual([]);
    expect(hrefs.filter((h) => h.startsWith('/media'))).toEqual([]);
    expect(hrefs.filter((h) => h.includes('{'))).toEqual([]);
  });

  test('the default Home page renders an empty main area', async ({ page }) => {
    await open(page, '/');
    await expect(page.locator('main')).toBeEmpty();
  });
});
