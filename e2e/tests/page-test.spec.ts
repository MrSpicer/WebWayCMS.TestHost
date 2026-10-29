import { expect, test } from '@playwright/test';

import { open } from '../support/helpers';
import { GUID, page as seedPage, textOf } from '../support/seed';

// /page-test is a PageTestHub page (no stored ViewName, showDiagnostics on, rich-text intro).
// /page-test/nested is a ZoneShowcasePage on the zero-zone Bare view, parented under page-test.

test.describe('/page-test (PageTestHub)', () => {
  test('renders the hub view with its title', async ({ page }) => {
    await open(page, '/page-test');
    await expect(page).toHaveTitle('Page Controller Tests - Integration-Host');
    await expect(page.getByTestId('page-test-hub').locator('h1')).toHaveText('Page Controller Tests');
  });

  test('the diagnostics show the Index view of a page with no stored view name', async ({ page }) => {
    await open(page, '/page-test');
    await expect(page.getByTestId('active-view')).toHaveText('Index');
    await expect(page.getByTestId('page-controller')).toHaveText('PageTestHub');
    await expect(page.getByTestId('page-viewname')).toHaveText('(none)');
    await expect(page.getByTestId('page-slug')).toHaveText('page-test');
    await expect(page.getByTestId('page-node-id')).toHaveText(GUID);
    await expect(page.getByTestId('page-version')).toHaveText(/^\d+ \(Published\)$/);
    await expect(page.getByTestId('route-pattern')).toHaveText('/page-test');
  });

  test('the seeded rich-text intro renders as HTML', async ({ page }) => {
    await open(page, '/page-test');
    const intro = String(seedPage('page-test').config.intro);
    await expect(page.getByTestId('page-test-hub').locator('div.mb-4 > p:not(.lead)')).toHaveText(textOf(intro));
    await expect(page.getByTestId('page-test-hub')).not.toContainText('<p>');
  });

  test('the cards link to the other seeded page-type pages', async ({ page }) => {
    await open(page, '/page-test');
    const hrefs = await page.locator('[data-testid="page-test-hub"] .card-footer a').evaluateAll((els) =>
      els.map((e) => e.getAttribute('href')),
    );
    expect(hrefs).toEqual(['/zones', '/zones-landing', '/configured']);
  });

  test('the unseeded HubNotice zone renders nothing', async ({ page }) => {
    await open(page, '/page-test');
    await expect(page.getByTestId('zone-hub-notice').locator('.content-zone')).toHaveCount(0);
  });
});

test.describe('/page-test/nested (ZoneShowcasePage, Bare, child page)', () => {
  test('resolves under its parent page URL with the Bare view', async ({ page }) => {
    await open(page, '/page-test/nested');
    const root = page.getByTestId('showcase-view-bare');
    await expect(root.locator('h1')).toHaveText(String(seedPage('nested').config.heading));
    await expect(page.getByTestId('active-view')).toHaveText('Bare');
    await expect(page.getByTestId('page-controller')).toHaveText('ZoneShowcasePage');
    await expect(page.getByTestId('page-viewname')).toHaveText('Bare');
    await expect(page.getByTestId('page-slug')).toHaveText('nested');
    await expect(page.getByTestId('route-pattern')).toHaveText('/page-test/nested');
  });

  test('the Bare view renders no zone markup', async ({ page }) => {
    await open(page, '/page-test/nested');
    await expect(page.getByTestId('showcase-view-bare')).toContainText('This view renders no content zones.');
    await expect(page.locator('main .content-zone')).toHaveCount(0);
  });

  test('the child page is not reachable at a top-level URL', async ({ page }) => {
    await open(page, '/nested', 404);
  });
});
