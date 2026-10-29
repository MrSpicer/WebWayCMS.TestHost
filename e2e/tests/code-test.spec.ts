import { expect, test } from '@playwright/test';

import { open } from '../support/helpers';

// CodeTestController's [CmsRoute] attributes: code-registered routes dispatched by the CMS route
// transformer, exercising literals, parameters, inline constraints, optional and catch-all segments.

test.describe('[CmsRoute] code routes', () => {
  test('/code-test renders the index', async ({ page }) => {
    await open(page, '/code-test');
    await expect(page).toHaveTitle('CmsRoute Tests - Integration-Host');
    await expect(page.locator('main h1')).toHaveText('CmsRoute Attribute Tests');
  });

  test('a literal route', async ({ page }) => {
    await open(page, '/code-test/simple');
    await expect(page).toHaveTitle('Simple Route - Integration-Host');
    await expect(page.locator('main h1')).toHaveText('Simple Route — Loaded!');
  });

  test('a {name} parameter', async ({ page }) => {
    // Lowercase on purpose: the CMS route matcher currently case-folds captured values
    // (/hello/World renders "Hello, world!").
    await open(page, '/code-test/hello/world');
    await expect(page.locator('main h1')).toHaveText('Hello, world!');
  });

  test('an {id:int} parameter', async ({ page }) => {
    await open(page, '/code-test/product/42');
    await expect(page.locator('main h1')).toHaveText('Product #42');
  });

  test('an {id:int} parameter rejects a non-integer', async ({ page }) => {
    await open(page, '/code-test/product/abc', 404);
  });

  test('an optional {query?} parameter, absent and present', async ({ page }) => {
    await open(page, '/code-test/search');
    await expect(page.locator('main h1')).toHaveText('Search Results');
    await expect(page.locator('main td.fw-bold')).toHaveText('(null)');

    await open(page, '/code-test/search/widgets');
    await expect(page.locator('main td.fw-bold')).toHaveText('widgets');
  });

  test('an {id:guid} parameter', async ({ page }) => {
    const id = '3f2504e0-4f89-11d3-9a0c-0305e82c3301';
    await open(page, `/code-test/item/${id}`);
    await expect(page.locator('main h1')).toHaveText('Item ID');
    await expect(page.locator('main td.fw-bold')).toHaveText(id);
  });

  test('an {id:guid} parameter rejects a non-guid', async ({ page }) => {
    await open(page, '/code-test/item/not-a-guid', 404);
  });

  test('a parameter inside a segment (page-{slug})', async ({ page }) => {
    await open(page, '/code-test/page-about');
    await expect(page.locator('main h1')).toHaveText('Page: about');
  });

  test('a {**path} catch-all captures slashes', async ({ page }) => {
    await open(page, '/code-test/docs/api/v2/users');
    await expect(page.locator('main h1')).toHaveText('Documentation Path');
    await expect(page.locator('main td.fw-bold')).toHaveText('api/v2/users');
  });

  test('two routes on one controller (multi/a, multi/b)', async ({ page }) => {
    await open(page, '/code-test/multi/a');
    await expect(page).toHaveTitle('Multi Route A - Integration-Host');
    await expect(page.locator('main h1')).toHaveText('Multi Route A');
    await open(page, '/code-test/multi/b');
    await expect(page).toHaveTitle('Multi Route B - Integration-Host');
    await expect(page.locator('main h1')).toHaveText('Multi Route B');
  });

  test('an explicit Action name', async ({ page }) => {
    await open(page, '/code-test/custom-action');
    await expect(page).toHaveTitle('Custom Action - Integration-Host');
    await expect(page.locator('main h1')).toHaveText('Custom Action — Loaded!');
  });

  test('a regex constraint accepts a match and rejects a miss', async ({ page }) => {
    await open(page, '/code-test/constraints/regex/abc');
    await expect(page.locator('main h1')).toHaveText('Regex Slug: abc');
    await open(page, '/code-test/constraints/regex/ABC1', 404);
  });
});
