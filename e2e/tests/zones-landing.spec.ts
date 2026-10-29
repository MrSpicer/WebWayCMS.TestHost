import { expect, test } from '@playwright/test';

import { expectData, open, zone, zoneItems } from '../support/helpers';
import { content, faq, page as seedPage, testPattern, textOf } from '../support/seed';

// /zones-landing is a ZoneShowcasePage on the Landing view (showDiagnostics false). Seeded zones:
//   LandingHero  ContentBlock "Landing Hero Block", then Image (test pattern, ShowCaption)
//   PageMain     Faq List (list.default, "Landing FAQs", MaxItems 4, ShowAnswers off)
//   LandingCta   FaqCount

test.describe('/zones-landing (ZoneShowcasePage, Landing)', () => {
  test('renders the Landing view without diagnostics or the note', async ({ page }) => {
    await open(page, '/zones-landing');
    const root = page.getByTestId('showcase-view-landing');
    await expect(root.locator('h1')).toHaveText(String(seedPage('zones-landing').config.heading));
    await expect(page.getByTestId('page-diagnostics')).toHaveCount(0);
    // The Landing view has no note slot.
    await expect(root).not.toContainText(String(seedPage('zones-landing').config.note));
  });

  test('LandingHero holds the hero block then the captioned image', async ({ page }) => {
    await open(page, '/zones-landing');
    const hero = zone(page.getByTestId('zone-landing-hero'), 'LandingHero');
    const items = zoneItems(hero);
    await expect(items).toHaveCount(2);
    await expect(items.nth(0).locator('p')).toHaveText(textOf(content('landing-hero-block').content));

    const image = content('test-pattern');
    const figure = items.nth(1).locator('figure.cms-image');
    const img = figure.locator('img');
    await expect(img).toHaveAttribute('src', new RegExp(`^/media/${testPattern.hash}/[A-Za-z0-9._-]+$`));
    await expect(img).toHaveAttribute('alt', image.altText);
    await expect(img).toHaveAttribute('width', String(testPattern.width));
    await expect(img).toHaveAttribute('height', String(testPattern.height));
    await expect(img).toHaveAttribute('loading', 'lazy');
    await expect(figure.locator('figcaption')).toHaveText(image.caption);
  });

  test('the image actually loads in the browser', async ({ page }) => {
    await open(page, '/zones-landing');
    const img = page.locator('figure.cms-image img');
    await img.scrollIntoViewIfNeeded();
    await expect
      .poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth))
      .toBe(testPattern.width);
  });

  test('PageMain renders list.default with the first four FAQs by title and no answers', async ({ page }) => {
    await open(page, '/zones-landing');
    const list = zone(page.getByTestId('zone-page-main'), 'PageMain').getByTestId('faq-list');
    await expectData(list, {
      view: 'Default',
      'faq-count': '4',
      'cfg-heading': 'Landing FAQs',
      'cfg-maxitems': '4',
      'cfg-showanswers': 'false',
      'cfg-testmarker': 'landing-list',
      'cfg-featuredfaqid': '',
    });
    await expect(list.locator('h2.faq-list-heading')).toHaveText('Landing FAQs');

    const expected = ['general-account-page', 'general-contact-support', 'billing-refunds', 'general-reset-password'].map(faq);
    const terms = list.locator('dl.faq-list-definitions > dt');
    await expect(terms).toHaveText(expected.map((f) => f.question));
    for (let i = 0; i < expected.length; i++) {
      await expect(terms.nth(i)).toHaveAttribute('data-faq-rating', String(expected[i].rating));
    }
    await expect(list.locator('dd')).toHaveCount(0);
    await expect(list.locator('.faq-list-intro')).toHaveCount(0);
    await expect(list.getByTestId('faq-list-featured')).toHaveCount(0);
    await expect(list.locator('.faq-list-footnote, a.faq-list-more')).toHaveCount(0);
  });

  test('LandingCta holds FaqCount', async ({ page }) => {
    await open(page, '/zones-landing');
    await expect(page.getByTestId('zone-landing-cta').getByTestId('faq-count')).toHaveAttribute('data-count', '6');
  });
});
