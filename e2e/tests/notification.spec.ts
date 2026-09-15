import { test, expect, type APIRequestContext } from '@playwright/test';
import { gateway, services } from '../helpers/urls';

/** OrderPlacedEvent.TotalAmount is transmitted in cents (see NotificationRenderer.FormatCurrency). */
const orderPlaced = (totalAmountCents: number) => ({
  orderId: crypto.randomUUID(),
  customerId: crypto.randomUUID(),
  totalAmount: totalAmountCents,
  placedAt: new Date().toISOString(),
});

async function postOrderPlaced(request: APIRequestContext, totalAmountCents: number) {
  const event = orderPlaced(totalAmountCents);
  const res = await request.post(`${gateway.notifications}/events/order-placed`, { data: event });
  expect(res.status()).toBe(201);
  const body = (await res.json()) as { id: string; previewUrl: string };
  expect(body.id).toMatch(/^[0-9a-f-]{36}$/);
  expect(body.previewUrl).toBe(`/api/notification/${body.id}/preview`);
  return { event, ...body };
}

test.describe('Notification flow', () => {
  test('order-placed event creates a rendered notification retrievable by id', async ({ request }) => {
    const { event, id } = await postOrderPlaced(request, 14999);

    const res = await request.get(`${gateway.notifications}/${id}`);
    expect(res.status()).toBe(200);
    const notification = await res.json();
    expect(notification).toMatchObject({
      id,
      orderId: event.orderId,
      customerId: event.customerId,
      orderTotal: 14999,
      customerName: 'Valued Customer',
      customerEmail: 'customer@example.com',
      type: 0, // OrderConfirmation
      status: 1, // Rendered
      renderedSubject: 'Order Confirmed — $149.99',
      sentAt: null,
    });

    const list = await request.get(gateway.notifications);
    expect(list.status()).toBe(200);
    expect((await list.json()).map((n: { id: string }) => n.id)).toContain(id);
  });

  test('GET unknown notification and preview return 404', async ({ request }) => {
    const id = crypto.randomUUID();
    expect((await request.get(`${gateway.notifications}/${id}`)).status()).toBe(404);
    expect((await request.get(`${gateway.notifications}/${id}/preview`)).status()).toBe(404);
  });

  test('preview renders the order summary card with name and currency-formatted total', async ({
    page,
    request,
  }) => {
    const { event, id } = await postOrderPlaced(request, 123456);

    const response = await page.goto(`${services.notification}/api/notification/${id}/preview`);
    expect(response?.status()).toBe(200);
    expect(response?.headers()['content-type']).toContain('text/html');

    await expect(page.getByRole('heading', { name: 'Order Confirmed' })).toBeVisible();
    await expect(page.getByText('Hi Valued Customer,')).toBeVisible();

    // The summary card is a presentational table, so query by text rather than cell role.
    const card = page.locator('table').filter({ hasText: 'Order Number' }).last();
    await expect(card).toBeVisible();
    await expect(card.getByText('Order Number')).toBeVisible();
    await expect(card.getByText(event.orderId.slice(0, 8).toUpperCase(), { exact: true })).toBeVisible();
    await expect(card.getByText('Total', { exact: true })).toBeVisible();
    await expect(card.getByText('$1,234.56', { exact: true })).toBeVisible();
    await expect(card).not.toContainText('¤');

    await expect(page.getByText('This email was sent to')).toContainText('customer@example.com');
  });

  test.describe('currency formatting edge cases in the preview', () => {
    for (const [cents, expected] of [
      [5, '$0.05'],
      [100, '$1.00'],
      [1000000, '$10,000.00'],
    ] as const) {
      test(`${cents} cents renders as ${expected}`, async ({ page, request }) => {
        const { id } = await postOrderPlaced(request, cents);
        await page.goto(`${services.notification}/api/notification/${id}/preview`);
        await expect(page.getByText(expected, { exact: true })).toBeVisible();
      });
    }
  });
});
