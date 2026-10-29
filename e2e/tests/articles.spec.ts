import { expect, test } from '@playwright/test';

import { open, zone, zoneItems } from '../support/helpers';
import { content, textOf } from '../support/seed';

// /articles-test is a GenericPage whose Main zone holds an Article widget in List mode over the
// "Test Articles" list (newest first). The List item owns /articles-test/{slug} and renders the
// article there with view.default, or _NotFound for a slug outside its list.

const first = content('first-seeded-article');
const second = content('second-seeded-article');

test.describe('/articles-test (Article list)', () => {
  test('emits the seeded meta description', async ({ page }) => {
    await open(page, '/articles-test');
    await expect(page.locator('head meta[name="description"]')).toHaveAttribute(
      'content',
      'Seeded Article widget test page.',
    );
  });

  test('lists the articles newest first with dates, summaries and detail links', async ({ page }) => {
    await open(page, '/articles-test');
    const items = zoneItems(zone(page.locator('main'), 'Main'));
    await expect(items).toHaveCount(1);

    const cards = items.first().locator('.card-content');
    await expect(cards).toHaveCount(2);
    await expect(cards.locator('p.title')).toHaveText([second.title, first.title]);
    await expect(cards.locator('p.subtitle')).toHaveText(['February 10, 2026', 'January 10, 2026']);
    await expect(cards.locator('p.subtitle + p')).toHaveText([second.summary, first.summary]);
    await expect(cards.nth(0).locator('p.title a')).toHaveAttribute('href', `/articles-test/${second.slug}`);
    await expect(cards.nth(1).locator('p.title a')).toHaveAttribute('href', `/articles-test/${first.slug}`);
    // The list view shows no author and no body.
    const listText = await items.first().textContent();
    expect(listText).not.toContain(first.authorName);
    expect(listText).not.toContain(textOf(first.body));
  });

  test('a list link navigates to the article detail', async ({ page }) => {
    await open(page, '/articles-test');
    await page.getByRole('link', { name: first.title }).click();
    await expect(page).toHaveURL(new RegExp(`/articles-test/${first.slug}$`));
    await expect(page.locator('main .card-content p.title')).toHaveText(first.title);
  });
});

test.describe('/articles-test/{slug} (Article detail on the list item route)', () => {
  for (const [article, date] of [
    [first, 'January 10, 2026'],
    [second, 'February 10, 2026'],
  ] as const) {
    test(`renders ${article.slug} with date, author and HTML body`, async ({ page }) => {
      await open(page, `/articles-test/${article.slug}`);
      const card = page.locator('main .card-content');
      await expect(card).toHaveCount(1);
      await expect(card.locator('p.title')).toHaveText(article.title);
      await expect(card.locator('p.subtitle')).toHaveText(`${date} · ${article.authorName}`);
      await expect(card.locator('hr + p')).toHaveText(textOf(article.body));
      await expect(card).not.toContainText(article.summary);
      await expect(page.locator('main a[href^="/articles-test/"]')).toHaveCount(0);
    });
  }

  test('an unknown slug renders the not-found notice', async ({ page }) => {
    await open(page, '/articles-test/nope');
    await expect(page.locator('main .notification.is-warning')).toHaveText('Article not found.');
    await expect(page.locator('main .card-content')).toHaveCount(0);
  });

  test('a slug failing the inline regex constraint is a 404', async ({ page }) => {
    await open(page, '/articles-test/Not_A_Slug', 404);
  });
});
