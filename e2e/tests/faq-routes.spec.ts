import { expect, test, type Page } from '@playwright/test';

import { expectData, open, watchDialogs } from '../support/helpers';
import { faq, faqs, textOf } from '../support/seed';

// Widget routes registered under /faq-test by the seeded zone items:
//   Single Faq  -> /faq-test/{slug:regex(^[a-z0-9-]+$)}   (a List-mode Faq owns no route)
//   FaqTopic    -> /faq-test/topics/{topic} and /faq-test/topics/{topic}/page/{pageNumber:int}
// On a widget route the List-mode Faq yields; FaqCount and the configured Single fallback still render.

const general = faqs.filter((f) => f.published && f.slug.startsWith('general-'));
const billing = faqs.filter((f) => f.published && f.slug.startsWith('billing-'));

async function topicQuestions(page: Page): Promise<string[]> {
  return page.getByTestId('faq-topic').locator('li a').allTextContents();
}

test.describe('Faq {slug} route', () => {
  test('resolves the FAQ named by the URL slug', async ({ page }) => {
    await open(page, '/faq-test/general-reset-password');
    const reset = faq('general-reset-password');
    const detail = page.getByTestId('faq-detail');
    await expectData(detail, { source: 'route', slug: reset.slug, 'faq-rating': String(reset.rating) });
    await expect(detail.locator('.faq-detail-title')).toHaveText(reset.title);
    await expect(detail.locator('.faq-detail-question')).toHaveText(reset.question);
    await expect(detail.locator('.faq-detail-answer em')).toHaveText('Forgot your password?');
    // The back link strips the route's parameter segment back to the page URL.
    await expect(detail.locator('a.faq-detail-back')).toHaveAttribute('href', '/faq-test');
  });

  test('the List-mode Faq yields on a widget route; FaqCount and FaqTopic still render', async ({ page }) => {
    await open(page, '/faq-test/general-reset-password');
    await expect(page.getByTestId('faq-list')).toHaveCount(0);
    await expect(page.locator('main').getByTestId('faq-count')).toHaveAttribute('data-count', '6');
    await expectData(page.getByTestId('faq-topic'), { topic: '', 'match-count': '0' });
  });

  test('the sanitized Refunds answer renders without script or handlers', async ({ page }) => {
    const dialogs = watchDialogs(page);
    await open(page, '/faq-test/billing-refunds');
    const answer = page.getByTestId('faq-detail').locator('.faq-detail-answer');
    await expect(answer.locator('p')).toHaveText(['Refunds are processed within 30 days.', 'Contact billing for help.']);
    await expect(answer.locator('script, [onclick]')).toHaveCount(0);
    expect(dialogs).toEqual([]);
  });

  for (const slug of ['nope', 'draft-unpublished']) {
    test(`an unresolvable slug (${slug}) falls back to the configured FAQ`, async ({ page }) => {
      await open(page, `/faq-test/${slug}`);
      const account = faq('general-account-page');
      const detail = page.getByTestId('faq-detail');
      await expectData(detail, { source: 'config', slug, 'faq-rating': String(account.rating) });
      await expect(detail.locator('.faq-detail-title')).toHaveText(account.title);
      await expect(page.locator('body')).not.toContainText(faq('draft-unpublished').question);
    });
  }

  test('a slug failing the inline regex constraint is a 404', async ({ page }) => {
    await open(page, '/faq-test/Not_A_Slug', 404);
    await expect(page.getByTestId('faq-detail')).toHaveCount(0);
  });
});

test.describe('FaqTopic routes', () => {
  test('topics/general page 1 shows two of the four general FAQs', async ({ page }) => {
    await open(page, '/faq-test/topics/general');
    await expectData(page.getByTestId('faq-topic'), {
      topic: 'general',
      page: '1',
      'page-size': '2',
      'match-count': String(general.length),
    });
    expect(general).toHaveLength(4);
    await expect(page.getByTestId('faq-topic').locator('li')).toHaveCount(2);
  });

  test('topics/general pages 1 and 2 together list exactly the general FAQs', async ({ page }) => {
    await open(page, '/faq-test/topics/general');
    const first = await topicQuestions(page);
    await open(page, '/faq-test/topics/general/page/2');
    await expectData(page.getByTestId('faq-topic'), { topic: 'general', page: '2', 'match-count': '4' });
    const second = await topicQuestions(page);
    expect(second).toHaveLength(2);
    expect([...first, ...second].sort()).toEqual(general.map((f) => f.question).sort());
  });

  test('each listed FAQ links to its slug anchor and carries its rating', async ({ page }) => {
    await open(page, '/faq-test/topics/billing');
    const items = page.getByTestId('faq-topic').locator('li');
    await expect(items).toHaveCount(billing.length);
    for (const f of billing) {
      const item = items.filter({ hasText: f.question });
      await expect(item).toHaveAttribute('data-faq-rating', String(f.rating));
      await expect(item.locator('a')).toHaveAttribute('href', `#${f.slug}`);
    }
  });

  test('topics/billing matches the two billing FAQs', async ({ page }) => {
    await open(page, '/faq-test/topics/billing');
    await expectData(page.getByTestId('faq-topic'), { topic: 'billing', 'match-count': '2' });
    expect((await topicQuestions(page)).sort()).toEqual(billing.map((f) => f.question).sort());
  });

  test('a page past the end renders the empty message', async ({ page }) => {
    await open(page, '/faq-test/topics/billing/page/2');
    const topic = page.getByTestId('faq-topic');
    await expectData(topic, { topic: 'billing', page: '2', 'match-count': '2' });
    await expect(topic.getByTestId('faq-topic-empty')).toHaveText('No FAQs for this topic.');
  });

  test('a non-integer page number fails the :int constraint (404)', async ({ page }) => {
    await open(page, '/faq-test/topics/general/page/abc', 404);
    await expect(page.getByTestId('faq-topic')).toHaveCount(0);
  });

  test('the configured Single FAQ still renders on topic routes; the list yields', async ({ page }) => {
    await open(page, '/faq-test/topics/general');
    await expectData(page.getByTestId('faq-detail'), { source: 'config', slug: '' });
    await expect(page.getByTestId('faq-list')).toHaveCount(0);
    // The FAQ answers never leak into topic lists.
    await expect(page.getByTestId('faq-topic')).not.toContainText(textOf(faq('general-reset-password').answer));
  });
});
