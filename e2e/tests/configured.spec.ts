import { expect, test } from '@playwright/test';

import { expectData, open } from '../support/helpers';
import { GUID, page as seedPage, textOf } from '../support/seed';

// /configured is a ConfiguredPage: one config value per EditorType, echoed as data-cfg-* attributes,
// proving the seeded ConfigurationJson -> CMS:PageConfig round-trip. FeaturedFaqId is an @seed:
// reference to the general-reset-password FAQ, resolved to that FAQ's generated node id.

const cfg = seedPage('configured').config as Record<string, string | number | boolean>;

test.describe('/configured (ConfiguredPage)', () => {
  test('echoes every seeded configuration value', async ({ page }) => {
    await open(page, '/configured');
    const root = page.getByTestId('configured-page');
    await expectData(root, {
      'cfg-heading': String(cfg.heading),
      'cfg-intro': String(cfg.intro),
      'cfg-body': String(cfg.body),
      'cfg-maxitems': '7',
      'cfg-showdetails': 'true',
      'cfg-sortorder': 'title-asc',
      'cfg-anchorslug': 'configured-anchor',
      'cfg-accentcolor': '#336699',
      'cfg-moreurl': 'https://example.com/more',
      'cfg-contactemail': 'hello@example.com',
      // Dates are formatted by FormValueFormatter (yyyy-MM-dd / yyyy-MM-ddTHH:mm).
      'cfg-effectivedate': '2026-01-15',
      'cfg-reviewedat': '2026-01-15T09:30',
      'cfg-featuredfaqid': GUID,
      'cfg-viewname': 'Default',
      'cfg-detailpagecontroller': 'GenericPage',
      'cfg-testmarker': 'seeded-marker',
      'cfg-headingicon': 'fa-solid fa-star',
    });
    await expect(root).toHaveAttribute('id', 'configured-anchor');
  });

  test('the @seed: FeaturedFaqId resolves to the same FAQ node as the /faq-test list', async ({ page }) => {
    // Both configs reference seed 11111111-…-0001 (general-reset-password).
    await open(page, '/configured');
    const configured = await page.getByTestId('configured-page').getAttribute('data-cfg-featuredfaqid');
    await open(page, '/faq-test');
    const listFeatured = await page.getByTestId('faq-list').getAttribute('data-cfg-featuredfaqid');
    expect(configured).toMatch(GUID);
    expect(configured).toBe(listFeatured);
  });

  test('renders the heading with its icon, the intro and the rich-text body', async ({ page }) => {
    await open(page, '/configured');
    const root = page.getByTestId('configured-page');
    await expect(root.locator('h1')).toHaveText(String(cfg.heading));
    await expect(root.locator('h1 i.fa-solid.fa-star')).toHaveCount(1);
    await expect(root.locator(':scope > p')).toHaveText(String(cfg.intro));
    await expect(root.locator(':scope > div > p')).toHaveText(textOf(String(cfg.body)));
  });
});
