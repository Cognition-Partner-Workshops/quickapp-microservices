import { test, expect } from '@playwright/test';
import { healthTargets } from '../helpers/urls';

test.describe('health checks', () => {
  for (const [name, url] of Object.entries(healthTargets)) {
    test(`${name} responds 200 Healthy on /healthz`, async ({ request }) => {
      const res = await request.get(`${url}/healthz`);
      expect(res.status()).toBe(200);
      expect(await res.text()).toBe('Healthy');
    });
  }
});
