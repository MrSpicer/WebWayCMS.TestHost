import { expect, test } from '@playwright/test';

import { expectData, open, zone, zoneItems } from '../support/helpers';
import { content, faq, GUID, page as seedPage, textOf } from '../support/seed';

// /zones is a ZoneShowcasePage on the TwoColumn view. Seeded zones:
//   PageHeader     ContentBlock "Showcase Header Block"
//   PageMain       Layout (TwoColumnSidebar) -> nested Main: ContentBlock "Layout Main Block"
//                                            -> nested Sidebar: Faq List (list.Compact, MaxItems 3)
//   PageAside      FaqCount
//   PageFooter     (nothing)
//   ShowcaseGlobal ContentBlock "Showcase Global Block" (global zone)

test.describe('/zones (ZoneShowcasePage, TwoColumn)', () => {
  test('renders the TwoColumn view with the seeded heading and note', async ({ page }) => {
    await open(page, '/zones');
    const root = page.getByTestId('showcase-view-two-column');
    await expect(root).toHaveCount(1);
    await expect(root.locator('h1')).toHaveText(String(seedPage('zones').config.heading));
    await expect(root.locator('p.text-muted')).toHaveText(String(seedPage('zones').config.note));
  });

  test('the page diagnostics reflect the published page and its route', async ({ page }) => {
    await open(page, '/zones');
    await expect(page.getByTestId('active-view')).toHaveText('TwoColumn');
    await expect(page.getByTestId('page-controller')).toHaveText('ZoneShowcasePage');
    await expect(page.getByTestId('page-viewname')).toHaveText('TwoColumn');
    await expect(page.getByTestId('page-slug')).toHaveText('zones');
    await expect(page.getByTestId('page-node-id')).toHaveText(GUID);
    await expect(page.getByTestId('page-version')).toHaveText(/^\d+ \(Published\)$/);
    await expect(page.getByTestId('route-pattern')).toHaveText('/zones');
  });

  test('PageHeader holds the header content block', async ({ page }) => {
    await open(page, '/zones');
    const items = zoneItems(zone(page.getByTestId('zone-page-header'), 'PageHeader'));
    await expect(items).toHaveCount(1);
    await expect(items.locator('p')).toHaveText(textOf(content('showcase-header-block').content));
  });

  test('PageMain holds a TwoColumnSidebar Layout with nested Main and Sidebar zones', async ({ page }) => {
    await open(page, '/zones');
    const pageMain = zone(page.getByTestId('zone-page-main'), 'PageMain');
    const items = zoneItems(pageMain);
    await expect(items).toHaveCount(1);

    // The host's Bootstrap Layout override: Main in col-md-8, Sidebar in col-md-4.
    const layout = items.first().locator(':scope > .container.py-5 > .row');
    const nestedMain = zone(layout.locator(':scope > .col-md-8'), 'Main');
    const nestedSidebar = zone(layout.locator(':scope > .col-md-4'), 'Sidebar');

    await expect(zoneItems(nestedMain)).toHaveCount(1);
    await expect(nestedMain.locator('p')).toHaveText(textOf(content('layout-main-block').content));
    await expect(zoneItems(nestedSidebar)).toHaveCount(1);
  });

  test('the nested Sidebar Faq List renders list.Compact with the first three FAQs by title', async ({ page }) => {
    await open(page, '/zones');
    const list = zone(page.getByTestId('zone-page-main'), 'Sidebar').getByTestId('faq-list');
    await expectData(list, {
      view: 'Compact',
      'faq-count': '3',
      'cfg-heading': 'Sidebar FAQs',
      'cfg-maxitems': '3',
      'cfg-showanswers': 'false',
      'cfg-testmarker': 'zones-sidebar-list',
      'cfg-headingicon': '',
    });
    await expect(list.locator('h2.faq-list-heading')).toHaveText('Sidebar FAQs');
    await expect(list.locator('h2 i')).toHaveCount(0);

    const expected = ['general-account-page', 'general-contact-support', 'billing-refunds'].map(faq);
    const items = list.locator('ul.faq-list-links > li');
    await expect(items).toHaveText(expected.map((f) => f.question));
    for (let i = 0; i < expected.length; i++) {
      await expect(items.nth(i)).toHaveAttribute('data-faq-rating', String(expected[i].rating));
      await expect(items.nth(i).locator('a')).toHaveAttribute('href', `#${expected[i].slug}`);
    }
  });

  test('PageAside holds FaqCount', async ({ page }) => {
    await open(page, '/zones');
    await expect(page.getByTestId('zone-page-aside').getByTestId('faq-count')).toHaveAttribute('data-count', '6');
  });

  test('the empty PageFooter zone renders no zone markup', async ({ page }) => {
    await open(page, '/zones');
    await expect(page.getByTestId('zone-page-footer').locator('.content-zone')).toHaveCount(0);
  });

  test('the global ShowcaseGlobal zone holds its content block', async ({ page }) => {
    await open(page, '/zones');
    const global = zone(page.getByTestId('zone-showcase-global'), 'ShowcaseGlobal');
    await expect(zoneItems(global)).toHaveCount(1);
    await expect(global.locator('p')).toHaveText(textOf(content('showcase-global-block').content));
  });

  test('page-scoped and nested zone names stay distinct in the DOM', async ({ page }) => {
    await open(page, '/zones');
    const names = await page.locator('main .content-zone').evaluateAll((els) =>
      els.map((e) => e.getAttribute('data-zone-name')),
    );
    expect(names).toEqual(['PageHeader', 'PageMain', 'Main', 'Sidebar', 'PageAside', 'ShowcaseGlobal']);
  });
});
