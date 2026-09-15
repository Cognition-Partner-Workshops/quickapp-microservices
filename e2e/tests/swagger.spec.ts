import { test, expect } from '@playwright/test';
import { services } from '../helpers/urls';

test.describe('Swagger UI', () => {
  test('Notification service Swagger UI loads and lists its endpoints', async ({ page }) => {
    await page.goto(`${services.notification}/swagger`);
    await expect(page).toHaveTitle(/Swagger UI/);
    await expect(page.locator('.swagger-ui .info .title')).toContainText('Notification.API');
    await expect(page.locator('#operations-Notification-get_api_Notification')).toBeVisible();
    await expect(
      page.locator('#operations-Notification-post_api_Notification_events_order_placed'),
    ).toBeVisible();
  });

  test('Customer service Swagger UI loads', async ({ page }) => {
    await page.goto(`${services.customer}/swagger`);
    await expect(page).toHaveTitle(/Swagger UI/);
    await expect(page.locator('.swagger-ui .info .title')).toContainText('Customer.API');
    await expect(page.locator('.opblock-tag-section')).not.toHaveCount(0);
  });

  test('swagger.json is served for every service', async ({ request }) => {
    for (const [name, url] of Object.entries(services)) {
      const res = await request.get(`${url}/swagger/v1/swagger.json`);
      expect(res.status(), `${name} swagger.json`).toBe(200);
      const doc = await res.json();
      expect(doc.openapi, `${name} openapi version`).toMatch(/^3\./);
      expect(Object.keys(doc.paths).length, `${name} paths`).toBeGreaterThan(0);
    }
  });
});
