import { expect, test, type Page } from '@playwright/test';

import { ADMIN_STATE } from '../support/admin';
import { open } from '../support/helpers';
import { faq, faqs, GUID, pages } from '../support/seed';

// Read-only checks of the admin UI for the seeded content, as the seeded admin. Nothing here writes:
// the suite must stay re-runnable against the same stack.
test.use({ storageState: ADMIN_STATE });

async function pageNodeId(page: Page, title: string): Promise<string> {
  await open(page, '/wadmin/pages');
  const href = await page.locator('a.admin-list-title', { hasText: new RegExp(`^\\s*${title}\\s*$`) }).getAttribute('href');
  const id = href?.split('/').pop() ?? '';
  expect(id, `node id of page '${title}'`).toMatch(GUID);
  return id;
}

test.describe('admin (seeded content)', () => {
  test('/wadmin/faqs lists all seven FAQs; only the draft offers Publish', async ({ page }) => {
    await open(page, '/wadmin/faqs');
    const rows = page.locator('table tbody tr');
    await expect(rows).toHaveCount(faqs.length);
    for (const f of faqs) {
      const row = rows.filter({ has: page.locator('td', { hasText: new RegExp(`^${f.slug}$`) }) });
      await expect(row).toHaveCount(1);
      await expect(row.locator('td').first()).toHaveText(f.title);
      await expect(row.getByRole('button', { name: f.published ? 'Unpublish' : 'Publish', exact: true })).toHaveCount(1);
    }
  });

  test('/wadmin/pages lists every seeded page plus the default Home and Dashboard', async ({ page }) => {
    await open(page, '/wadmin/pages');
    const titles = (await page.locator('a.admin-list-title').allTextContents()).map((t) => t.trim());
    // Home (/) and the /wadmin Dashboard are created by DefaultContentSeeder, not the JSON seed.
    expect(titles.sort()).toEqual([...pages.map((p) => p.title), 'Home', 'Dashboard'].sort());
  });

  test('/wadmin/articlelists lists Test Articles, which lists both articles', async ({ page }) => {
    await open(page, '/wadmin/articlelists');
    const list = page.locator('a.admin-list-title', { hasText: 'Test Articles' });
    await expect(list).toHaveCount(1);
    await list.click();
    await expect(page).toHaveURL(/\/wadmin\/articles\?list=[0-9a-f-]{36}$/i);
    await expect(page.locator('h1')).toHaveText('Test Articles — Articles');
    const titles = (await page.locator('a.admin-list-title').allTextContents()).map((t) => t.trim());
    expect(titles.sort()).toEqual(['First Seeded Article', 'Second Seeded Article']);
  });

  test('the faq-test draft preview reads drafts for that request only', async ({ page }) => {
    const draft = faq('draft-unpublished');
    const nodeId = await pageNodeId(page, 'FAQ Test');

    const response = await open(page, `/_preview/${nodeId}`);
    expect(response.headers()['x-frame-options']).toBe('SAMEORIGIN');
    expect(response.headers()['cache-control']).toContain('no-store');

    await expect(page.locator('main').getByTestId('faq-count')).toHaveAttribute('data-count', '7');
    // "Draft FAQ" sorts third by title, inside the list's MaxItems of 5.
    const questions = page.getByTestId('faq-list').locator('.accordion-button');
    await expect(questions).toHaveText([
      faq('general-account-page').question,
      faq('general-contact-support').question,
      draft.question,
      faq('billing-refunds').question,
      faq('general-reset-password').question,
    ]);

    // There is no preview cookie: the live page in the same session is still published-only.
    await open(page, '/faq-test');
    await expect(page.locator('main').getByTestId('faq-count')).toHaveAttribute('data-count', '6');
    await expect(page.locator('body')).not.toContainText(draft.question);
  });
});
