import { expect, type Locator, type Page } from '@playwright/test';

/** The `.content-zone` wrapper the CMS renders for a zone with items (an empty zone renders nothing). */
export function zone(scope: Page | Locator, zoneName: string): Locator {
  return scope.locator(`.content-zone[data-zone-name="${zoneName}"]`);
}

/** The direct items of a zone, in render order. */
export function zoneItems(zoneLocator: Locator): Locator {
  return zoneLocator.locator(':scope > .zone-object');
}

/** Asserts every `data-*` attribute in `attrs` on `locator`. */
export async function expectData(locator: Locator, attrs: Record<string, string | RegExp>): Promise<void> {
  for (const [name, value] of Object.entries(attrs)) {
    await expect(locator, `data-${name}`).toHaveAttribute(`data-${name}`, value);
  }
}

/**
 * Records every JS dialog (alert/confirm/prompt) the page raises; assert the returned list is empty.
 * The seeded XSS probe uses `alert('xss')`, which must never run.
 */
export function watchDialogs(page: Page): string[] {
  const messages: string[] = [];
  page.on('dialog', async (dialog) => {
    messages.push(dialog.message());
    await dialog.dismiss();
  });
  return messages;
}

/** Navigates and asserts the response status (default 200). */
export async function open(page: Page, url: string, status = 200) {
  const response = await page.goto(url);
  expect(response, `response for ${url}`).not.toBeNull();
  expect(response!.status(), `status for ${url}`).toBe(status);
  return response!;
}
