import { expect, test, type Page } from '@playwright/test';

import { ADMIN_STATE } from '../support/admin';
import { open } from '../support/helpers';
import { faqs, faqTopics, GUID } from '../support/seed';

// Read-only checks of the content-relation admin surface for the seeded content: the generic children
// panel a containment adds to the parent's edit screen, and the host FAQ → topic containment.
test.use({ storageState: ADMIN_STATE });

/** Opens a list page and follows the row link titled `title` to the item's edit screen. */
async function openEdit(page: Page, listUrl: string, title: string, contentType: string): Promise<string> {
  await open(page, listUrl);
  const href = await page.locator(`a[href*="/wadmin/${contentType}/edit/"]`, { hasText: new RegExp(`^\\s*${title}\\s*$`) })
    .first()
    .getAttribute('href');
  const id = href?.split('/').pop() ?? '';
  expect(id, `node id of ${contentType} '${title}'`).toMatch(GUID);
  await open(page, `/wadmin/${contentType}/edit/${id}`);
  return id;
}

function panel(page: Page, heading: string) {
  return page.locator('section[data-relation-children]', { has: page.locator('.card-header-title', { hasText: heading }) });
}

test.describe('children panel', () => {
  test('the Test Articles list edit screen lists both articles and preselects itself on New', async ({ page }) => {
    await open(page, '/wadmin/articlelists');
    const href = await page.locator('a.admin-list-title', { hasText: 'Test Articles' }).getAttribute('href');
    const listId = new URL(href!, 'http://x').searchParams.get('list') ?? '';
    expect(listId).toMatch(GUID);

    await open(page, `/wadmin/articlelists/edit/${listId}`);
    const articles = panel(page, 'Articles');
    await expect(articles).toHaveCount(1);
    await expect(articles.locator('.card-header-title .tag')).toHaveText('2');
    const titles = (await articles.locator('tbody tr td:first-child a').allTextContents()).map((t) => t.trim());
    expect(titles.sort()).toEqual(['First Seeded Article', 'Second Seeded Article']);
    await expect(articles.getByRole('link', { name: 'New Article' }))
      .toHaveAttribute('href', `/wadmin/articles/create?ArticleListId=${listId}`);
    // Not ordered: no Up/Down buttons.
    await expect(articles.locator('[data-move]')).toHaveCount(0);
  });

  test('New Article from the panel opens the form with the list preselected', async ({ page }) => {
    await open(page, '/wadmin/articlelists');
    const href = await page.locator('a.admin-list-title', { hasText: 'Test Articles' }).getAttribute('href');
    const listId = new URL(href!, 'http://x').searchParams.get('list') ?? '';

    await open(page, `/wadmin/articles/create?ArticleListId=${listId}`);
    await expect(page.locator('[name="ArticleListId"]')).toHaveValue(listId);
  });
});

test.describe('FAQ topics (host containment)', () => {
  test('/wadmin/faqtopics lists both seeded topics with their FAQ counts', async ({ page }) => {
    await open(page, '/wadmin/faqtopics');
    const rows = page.locator('table tbody tr');
    await expect(rows).toHaveCount(faqTopics.length);
    for (const topic of faqTopics) {
      const row = rows.filter({ has: page.locator('td', { hasText: new RegExp(`^${topic.slug}$`) }) });
      await expect(row.locator('td').first()).toHaveText(topic.title);
      await expect(row.getByTestId('faq-topic-count')).toHaveText(String(faqs.filter((f) => f.topic === topic.slug).length));
      // The delete confirm names the FAQs that go with the topic.
      await expect(row.locator('[data-delete-trigger]')).toHaveAttribute('data-delete-descendants', String(faqs.filter((f) => f.topic === topic.slug).length));
    }
  });

  test('a topic edit screen lists its FAQs in the children panel', async ({ page }) => {
    const billing = faqTopics.find((t) => t.slug === 'billing')!;
    const topicId = await openEdit(page, '/wadmin/faqtopics', billing.title, 'faqtopics');
    const children = panel(page, 'FAQs');
    const expected = faqs.filter((f) => f.topic === 'billing').map((f) => f.title).sort();
    await expect(children.locator('.card-header-title .tag')).toHaveText(String(expected.length));
    const titles = (await children.locator('tbody tr td:first-child a').allTextContents()).map((t) => t.trim());
    expect(titles.sort()).toEqual(expected);
    await expect(children.getByRole('link', { name: 'New FAQ' }))
      .toHaveAttribute('href', `/wadmin/faqs/create?FaqTopicId=${topicId}`);
  });

  test('/wadmin/faqs shows each FAQ\'s topic; the draft FAQ has none', async ({ page }) => {
    await open(page, '/wadmin/faqs');
    const rows = page.locator('table tbody tr');
    for (const f of faqs) {
      const row = rows.filter({ has: page.locator('td', { hasText: new RegExp(`^${f.slug}$`) }) });
      const title = faqTopics.find((t) => t.slug === f.topic)?.title ?? '';
      await expect(row.locator('td').nth(2)).toHaveText(title);
    }
  });

  test('the FAQ form renders the topic as a relation picker', async ({ page }) => {
    await open(page, '/wadmin/faqs/create');
    const picker = page.locator('[name="FaqTopicId"]');
    await expect(picker).toHaveCount(1);
    await expect(page.locator('[data-endpoint="/wadmin/faqtopics/api/list"]')).toHaveCount(1);
  });
});
