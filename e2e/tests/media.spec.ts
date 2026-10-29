import { expect, test } from '@playwright/test';

import { open } from '../support/helpers';
import { testPattern } from '../support/seed';

// The seeded Image (@media:media/test-pattern.png) is stored content-addressed in Postgres and served
// by MediaController at /media/{sha256}/{fileName} with immutable caching.

test.describe('media serving', () => {
  test('serves the seeded image bytes with immutable cache headers', async ({ page, request }) => {
    await open(page, '/zones-landing');
    const src = await page.locator('figure.cms-image img').getAttribute('src');
    expect(src).toContain(`/media/${testPattern.hash}/`);

    const response = await request.get(src!);
    expect(response.status()).toBe(200);
    expect(response.headers()['content-type']).toBe('image/png');
    expect(response.headers()['cache-control']).toBe('public, max-age=31536000, immutable');
    expect(response.headers()['etag']).toBe(`"${testPattern.hash}"`);
    expect(Buffer.compare(await response.body(), testPattern.bytes)).toBe(0);
  });

  test('the file name segment is cosmetic: the hash alone addresses the blob', async ({ request }) => {
    const response = await request.get(`/media/${testPattern.hash}/any-name.png`);
    expect(response.status()).toBe(200);
    expect(Buffer.compare(await response.body(), testPattern.bytes)).toBe(0);
  });

  test('a matching If-None-Match answers 304', async ({ request }) => {
    const response = await request.get(`/media/${testPattern.hash}/test-pattern.png`, {
      headers: { 'If-None-Match': `"${testPattern.hash}"` },
    });
    expect(response.status()).toBe(304);
    expect(response.headers()['etag']).toBe(`"${testPattern.hash}"`);
  });

  test('an unknown hash is a 404', async ({ request }) => {
    const response = await request.get(`/media/${'0'.repeat(64)}/x.png`);
    expect(response.status()).toBe(404);
  });
});
