import { expect, test, type Page } from '@playwright/test';

import { ADMIN_STATE } from '../support/admin';
import { open } from '../support/helpers';
import { GUID } from '../support/seed';

// The one spec that writes. It runs in the `writes` project, after every read-only spec has passed
// (playwright.config.ts), one test at a time, and everything it creates is titled "E2E …" and swept
// before and after the run — so the suite stays re-runnable against the same stack.
test.use({ storageState: ADMIN_STATE });
test.describe.configure({ mode: 'serial' });

const PREFIX = 'E2E ';

/** Deletes every row on `listUrl` whose delete trigger names an "E2E …" item, until none is left. */
async function sweep(page: Page, listUrl: string): Promise<void> {
  for (let i = 0; i < 20; i++) {
    await open(page, listUrl);
    const trigger = page.locator(`[data-delete-trigger][data-delete-name^="${PREFIX}"]`).first();
    if ((await trigger.count()) === 0) return;
    await confirmDelete(page, (await trigger.getAttribute('data-delete-name'))!);
  }
  throw new Error(`Could not sweep ${listUrl}`);
}

/** Opens the shared delete confirm for `name` (the trigger may sit in a closed row menu) and confirms it. */
async function confirmDelete(page: Page, name: string): Promise<string> {
  await page.locator(`[data-delete-trigger][data-delete-name="${name}"]`).evaluate((el: HTMLElement) => el.click());
  const message = (await page.locator('#deleteModalMessage').textContent()) ?? '';
  await Promise.all([page.waitForURL(/\/wadmin\//), page.locator('#deleteModalConfirm').click()]);
  return message;
}

async function pageId(page: Page, title: string): Promise<string> {
  await open(page, '/wadmin/pages');
  const href = await page.locator('a.admin-list-title', { hasText: new RegExp(`^\\s*${title}\\s*$`) }).getAttribute('href');
  const id = href?.split('/').pop() ?? '';
  expect(id, `node id of page '${title}'`).toMatch(GUID);
  return id;
}

/** Picks `value` in an entity-picker select once its options have loaded from the list endpoint. */
async function pick(page: Page, name: string, value: string): Promise<void> {
  const select = page.locator(`select[name="${name}"]`);
  await expect(select.locator(`option[value="${value}"]`)).toHaveCount(1);
  await select.selectOption(value);
}

async function createPage(page: Page, title: string, slug: string, parentId?: string): Promise<string> {
  await open(page, '/wadmin/pages/create');
  await page.locator('#Title').fill(title);
  await page.locator('#Slug').fill(slug);
  await page.locator('#ControllerName').selectOption('GenericPage');
  if (parentId) await pick(page, 'ParentNodeId', parentId);
  await Promise.all([page.waitForURL(/\/wadmin\/pages$/), page.getByRole('button', { name: 'Publish', exact: true }).click()]);
  await expect(page.locator('.notification.is-danger')).toHaveCount(0);
  return pageId(page, title);
}

/** Publishes from the Pages list (the path that reports the child-URL rewrite) and returns the flash. */
async function publishFromList(page: Page, title: string): Promise<string> {
  await open(page, '/wadmin/pages');
  const row = page.locator('tr', { has: page.locator('a.admin-list-title', { hasText: new RegExp(`^\\s*${title}\\s*$`) }) });
  await Promise.all([page.waitForURL(/\/wadmin\/pages$/), row.getByRole('button', { name: 'Publish', exact: true }).click()]);
  await expect(page.locator('.notification.is-danger')).toHaveCount(0);
  return (await page.locator('.notification.is-success').allTextContents()).join(' ').trim();
}

async function expectStatus(page: Page, url: string, status: number): Promise<void> {
  const response = await page.request.get(url, { maxRedirects: 0 });
  expect(response.status(), `status for ${url}`).toBe(status);
}

test.beforeAll(async ({ browser }) => {
  const page = await browser.newPage({ storageState: ADMIN_STATE });
  await sweep(page, '/wadmin/pages');
  await sweep(page, '/wadmin/articlelists');
  await page.close();
});

test.afterAll(async ({ browser }) => {
  const page = await browser.newPage({ storageState: ADMIN_STATE });
  await sweep(page, '/wadmin/pages');
  await sweep(page, '/wadmin/articlelists');
  await page.close();
});

test('reference modal: New Article List from the Article form, validation inside the modal, then picked', async ({ page }) => {
  await open(page, '/wadmin/articles/create');
  const field = page.locator('.field', { has: page.locator('select[name="ArticleListId"]') });
  await field.locator('.entity-picker-new').click();

  const modal = page.locator('[data-relation-modal]');
  await expect(modal).toHaveCount(1);
  const frame = page.frameLocator('iframe.relation-modal-frame');

  // An empty title fails validation; the error re-renders inside the modal, which stays open.
  await frame.getByRole('button', { name: 'Save as Draft' }).click();
  await expect(frame.locator('.validation-summary-errors, [data-valmsg-for="Title"]:not(:empty)').first()).toBeVisible();
  await expect(modal).toHaveCount(1);

  await frame.locator('#Title').fill(`${PREFIX}Modal List`);
  await frame.getByRole('button', { name: 'Save as Draft' }).click();

  await expect(modal).toHaveCount(0);
  const select = page.locator('select[name="ArticleListId"]');
  await expect(select).toHaveValue(GUID);
  await expect(select.locator('option:checked')).toHaveText(`${PREFIX}Modal List`);
  // Only the list was saved: the Article form itself is still an unsaved create form.
  await expect(page).toHaveURL(/\/wadmin\/articles\/create$/);

  // Edit is now enabled and opens the saved list in the modal.
  await field.locator('.entity-picker-edit').click();
  await expect(page.frameLocator('iframe.relation-modal-frame').locator('#Title')).toHaveValue(`${PREFIX}Modal List`);
});

test('modal publish: a New list created through the picker modal goes live, not stranded as a draft', async ({ page }) => {
  await open(page, '/wadmin/articles/create');
  const field = page.locator('.field', { has: page.locator('select[name="ArticleListId"]') });
  await field.locator('.entity-picker-new').click();

  const frame = page.frameLocator('iframe.relation-modal-frame');
  await frame.locator('#Title').fill(`${PREFIX}Live List`);
  await frame.getByRole('button', { name: 'Publish', exact: true }).click();

  await expect(page.locator('[data-relation-modal]')).toHaveCount(0);
  const select = page.locator('select[name="ArticleListId"]');
  await expect(select).toHaveValue(GUID);

  // The Publish click inside the modal must publish: the list is Live right away.
  await open(page, '/wadmin/articlelists');
  const row = page.locator('tr', { has: page.locator('a.admin-list-title', { hasText: new RegExp(`^\\s*${PREFIX}Live List\\s*$`) }) });
  await expect(row).toContainText('Live');
});

test('routes: a modal-added Explicit route, a parent rename rewriting the child URL, a move, and a subtree delete', async ({ page }) => {
  // 1. A published page with its Derived route.
  const docsId = await createPage(page, `${PREFIX}Docs`, 'e2e-docs');
  await expectStatus(page, '/e2e-docs', 200);

  // 2. An Explicit route added through the routes picker's New modal; live once the page publishes.
  await open(page, `/wadmin/pages/edit/${docsId}`);
  const routes = page.locator('#RouteIds');
  await expect(routes.locator('.entity-picker-values > li')).toHaveCount(1);
  await routes.locator('.entity-picker-new').click();
  const frame = page.frameLocator('iframe.relation-modal-frame');
  await frame.locator('#Mode').selectOption('Explicit');
  await frame.locator('#Pattern').fill('/e2e-documentation');
  await frame.getByRole('button', { name: 'Save', exact: true }).click();
  await expect(page.locator('[data-relation-modal]')).toHaveCount(0);
  await expect(routes.locator('.entity-picker-values > li')).toHaveCount(2);
  // A live page's form offers Unpublish, not Publish: save the draft, then publish from the list.
  await Promise.all([page.waitForURL(/\/wadmin\/pages$/), page.getByRole('button', { name: 'Save as Draft' }).click()]);
  await expectStatus(page, '/e2e-documentation', 404); // a draft until the page publishes
  await publishFromList(page, `${PREFIX}Docs`);
  await expectStatus(page, '/e2e-docs', 200);
  await expectStatus(page, '/e2e-documentation', 200);

  // 3. A child page under it.
  const introId = await createPage(page, `${PREFIX}Intro`, 'intro', docsId);
  await expectStatus(page, '/e2e-docs/intro', 200);

  // 4. Rename the parent: its publish re-projects the live child's derived URL; the Explicit route stays.
  await open(page, `/wadmin/pages/edit/${docsId}`);
  await page.locator('#Slug').fill('e2e-guides');
  await Promise.all([page.waitForURL(/\/wadmin\/pages$/), page.getByRole('button', { name: 'Save as Draft' }).click()]);
  await expectStatus(page, '/e2e-docs/intro', 200); // unchanged until publish
  expect(await publishFromList(page, `${PREFIX}Docs`)).toContain('1 child URL(s) updated.');
  await expectStatus(page, '/e2e-guides', 200);
  await expectStatus(page, '/e2e-guides/intro', 200);
  await expectStatus(page, '/e2e-docs/intro', 404);
  await expectStatus(page, '/e2e-documentation', 200);

  // 5. Move the child to another parent with the Parent Page picker: a draft until it publishes.
  const otherId = await createPage(page, `${PREFIX}Other`, 'e2e-other');
  await open(page, `/wadmin/pages/edit/${introId}`);
  await pick(page, 'ParentNodeId', otherId);
  await Promise.all([page.waitForURL(/\/wadmin\/pages$/), page.getByRole('button', { name: 'Save as Draft' }).click()]);
  await expectStatus(page, '/e2e-guides/intro', 200);
  await expectStatus(page, '/e2e-other/intro', 404);
  await publishFromList(page, `${PREFIX}Intro`);
  await expectStatus(page, '/e2e-other/intro', 200);
  await expectStatus(page, '/e2e-guides/intro', 404);

  // 6. A page can't be moved under its own child: the picker excludes the edited page's own
  //    subtree, so the child option is never offered; the server guard is still exercised with a
  //    forged value (simulating a stale/tampered client).
  await open(page, `/wadmin/pages/edit/${otherId}`);
  await expect(page.locator(`select[name="ParentNodeId"] option[value="${introId}"]`)).toHaveCount(0);
  await page.locator('select[name="ParentNodeId"]').evaluate((el: HTMLSelectElement, id: string) => {
    const forged = document.createElement('option');
    forged.value = id;
    forged.textContent = 'E2E Intro';
    el.add(forged);
    el.value = id;
  }, introId);
  await page.getByRole('button', { name: 'Save as Draft' }).click();
  await expect(page.locator('[data-valmsg-for="ParentNodeId"]')).toContainText('under itself or one of its descendants');

  // 7. Subtree delete: two levels under "E2E Other"; the confirm states the count, and every URL goes.
  await createPage(page, `${PREFIX}Deep`, 'deep', introId);
  await expectStatus(page, '/e2e-other/intro/deep', 200);
  await open(page, '/wadmin/pages');
  const message = await confirmDelete(page, `${PREFIX}Other`);
  expect(message).toBe(`Delete "${PREFIX}Other" and its 2 child pages? This cannot be undone.`);
  await expect(page.locator('.notification.is-success')).toContainText('2 item(s)');
  for (const url of ['/e2e-other', '/e2e-other/intro', '/e2e-other/intro/deep']) await expectStatus(page, url, 404);
  await open(page, '/wadmin/pages');
  for (const title of ['Other', 'Intro', 'Deep']) {
    await expect(page.locator('a.admin-list-title', { hasText: new RegExp(`^\\s*${PREFIX}${title}\\s*$`) })).toHaveCount(0);
  }
});
