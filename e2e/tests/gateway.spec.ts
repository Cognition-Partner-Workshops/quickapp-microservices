import { test, expect } from '@playwright/test';
import { gateway } from '../helpers/urls';

test.describe('gateway routing: list endpoints', () => {
  for (const [name, url] of Object.entries(gateway)) {
    test(`GET ${name} list through the gateway returns 200 JSON`, async ({ request }) => {
      const res = await request.get(url);
      expect(res.status()).toBe(200);
      expect(res.headers()['content-type']).toContain('application/json');
      const body = await res.json();
      expect(body).not.toBeNull();
      expect(typeof body).toBe('object');
    });
  }

  test('gateway returns 404 for an unrouted path', async ({ request }) => {
    const res = await request.get('/api/does-not-exist');
    expect(res.status()).toBe(404);
  });
});
