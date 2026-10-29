import { expect, test } from '@playwright/test';

import { expectData, open, watchDialogs, zone, zoneItems } from '../support/helpers';
import { faq, GUID, textOf } from '../support/seed';

// /faq-test is a GenericPage whose Main zone is seeded with, in order: FaqCount, a List-mode Faq
// (list.Accordion), a Single-mode Faq (configured FAQ fallback) and FaqTopic. Published FAQs sorted
// by title: Account Page, Contact Support, Refunds, Reset Password, Support Hours, Update Card.

const LIST_ORDER = [
  'general-account-page',
  'general-contact-support',
  'billing-refunds',
  'general-reset-password',
  'general-support-hours',
];

test.describe('/faq-test', () => {
  test('emits the seeded meta description from the page config', async ({ page }) => {
    await open(page, '/faq-test');
    await expect(page.locator('head meta[name="description"]')).toHaveAttribute(
      'content',
      'Seeded FAQ widget test page.',
    );
  });

  test('renders the Main zone items in seeded order', async ({ page }) => {
    await open(page, '/faq-test');
    const items = zoneItems(zone(page.locator('main'), 'Main'));
    await expect(items).toHaveCount(4);
    await expect(items.nth(0).getByTestId('faq-count')).toBeVisible();
    await expect(items.nth(1).getByTestId('faq-list')).toBeVisible();
    await expect(items.nth(2).getByTestId('faq-detail')).toBeVisible();
    await expect(items.nth(3).getByTestId('faq-topic')).toHaveCount(1);
  });

  test('FaqCount counts only the six published FAQs', async ({ page }) => {
    await open(page, '/faq-test');
    const count = page.locator('main').getByTestId('faq-count');
    await expect(count).toHaveAttribute('data-count', '6');
    await expect(count.locator('.faq-count-value')).toHaveText('6');
  });

  test('the List-mode Faq renders list.Accordion with its seeded configuration', async ({ page }) => {
    await open(page, '/faq-test');
    const list = page.getByTestId('faq-list');
    await expect(list).toHaveCount(1);
    await expectData(list, {
      view: 'Accordion',
      'faq-count': '5',
      'cfg-heading': 'Seeded FAQ List',
      'cfg-intro': 'Every FAQ, seeded.',
      'cfg-maxitems': '5',
      'cfg-showanswers': 'true',
      'cfg-sortorder': 'title-asc',
      'cfg-testmarker': 'faq-test-list',
      'cfg-headingicon': 'fa-solid fa-circle-question',
      'cfg-featuredfaqid': GUID,
      'cfg-viewname': '',
    });
    const heading = list.locator('h2.faq-list-heading');
    await expect(heading).toHaveText('Seeded FAQ List');
    await expect(heading.locator('i.fa-solid.fa-circle-question')).toHaveCount(1);
  });

  test('the accordion lists the first five FAQs by title with their ratings', async ({ page }) => {
    await open(page, '/faq-test');
    const items = page.getByTestId('faq-list').locator('.accordion-item');
    await expect(items.locator('.accordion-button')).toHaveText(LIST_ORDER.map((s) => faq(s).question));
    for (let i = 0; i < LIST_ORDER.length; i++) {
      await expect(items.nth(i)).toHaveAttribute('data-faq-rating', String(faq(LIST_ORDER[i]).rating));
    }
    // The first item is expanded, the rest are collapsed.
    await expect(items.nth(0).locator('.accordion-collapse')).toHaveClass(/\bshow\b/);
    await expect(items.nth(1).locator('.accordion-collapse')).not.toHaveClass(/\bshow\b/);
  });

  test('answers render as sanitized HTML, not escaped markup', async ({ page }) => {
    await open(page, '/faq-test');
    const bodies = page.getByTestId('faq-list').locator('.accordion-body');

    const reset = bodies.nth(LIST_ORDER.indexOf('general-reset-password'));
    await expect(reset.locator('p em')).toHaveText('Forgot your password?');
    await expect(reset).toHaveText(textOf(faq('general-reset-password').answer));
    await expect(reset).not.toContainText('<p>');

    const contact = bodies.nth(LIST_ORDER.indexOf('general-contact-support'));
    await expect(contact.locator('strong')).toHaveText('support@example.com');
  });

  test('the Refunds XSS probe is sanitized: no script, no event handler, no dialog', async ({ page }) => {
    const dialogs = watchDialogs(page);
    await open(page, '/faq-test');
    const refunds = page.getByTestId('faq-list').locator('.accordion-body').nth(LIST_ORDER.indexOf('billing-refunds'));
    await expect(refunds.locator('p')).toHaveText(['Refunds are processed within 30 days.', 'Contact billing for help.']);
    await expect(refunds.locator('script')).toHaveCount(0);
    await expect(page.locator('main [onclick]')).toHaveCount(0);
    expect(await page.locator('main').innerHTML()).not.toContain("alert('xss')");
    expect(dialogs).toEqual([]);
  });

  test('the Single-mode Faq falls back to its configured FAQ on the plain page', async ({ page }) => {
    await open(page, '/faq-test');
    const account = faq('general-account-page');
    const detail = page.getByTestId('faq-detail');
    await expect(detail).toHaveCount(1);
    await expectData(detail, {
      view: 'Default',
      source: 'config',
      slug: '',
      'faq-rating': String(account.rating),
      'cfg-faqid': GUID,
      'cfg-showbacklink': 'true',
      'cfg-backlinktext': 'Back to FAQs',
    });
    await expect(detail.locator('.faq-detail-title')).toHaveText(account.title);
    await expect(detail.locator('.faq-detail-question')).toHaveText(account.question);
    await expect(detail.locator('.faq-detail-answer p')).toHaveText(textOf(account.answer));
    const back = detail.locator('a.faq-detail-back');
    await expect(back).toHaveText('Back to FAQs');
    await expect(back).toHaveAttribute('href', '/faq-test');
  });

  test('FaqTopic renders its empty state when no topic is in the URL', async ({ page }) => {
    await open(page, '/faq-test');
    const topic = page.getByTestId('faq-topic');
    await expectData(topic, {
      view: 'Default',
      topic: '',
      page: '1',
      'page-size': '2',
      'match-count': '0',
      'cfg-pagesize': '2',
      'cfg-emptymessage': 'No FAQs for this topic.',
    });
    await expect(topic.getByTestId('faq-topic-empty')).toHaveText('No FAQs for this topic.');
  });

  test('the unpublished draft FAQ never renders', async ({ page }) => {
    await open(page, '/faq-test');
    const draft = faq('draft-unpublished');
    expect(draft.published).toBe(false);
    await expect(page.locator('body')).not.toContainText(draft.question);
    await expect(page.locator('body')).not.toContainText(draft.title);
  });
});
