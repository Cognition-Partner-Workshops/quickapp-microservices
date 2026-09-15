import { test, expect } from '@playwright/test';
import { gateway } from '../helpers/urls';

test.describe('Customer CRUD through the gateway', () => {
  test('create → get → update → delete round-trip', async ({ request }) => {
    const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const create = { name: `E2E Customer ${suffix}`, email: `e2e-${suffix}@example.com` };

    const created = await request.post(gateway.customers, { data: create });
    expect(created.status()).toBe(201);
    const customer = await created.json();
    expect(customer).toMatchObject(create);
    expect(typeof customer.id).toBe('number');
    expect(new Date(customer.createdAt).getTime()).not.toBeNaN();
    expect(created.headers()['location'].toLowerCase()).toContain(`/api/customer/${customer.id}`);

    const fetched = await request.get(`${gateway.customers}/${customer.id}`);
    expect(fetched.status()).toBe(200);
    expect(await fetched.json()).toMatchObject({ id: customer.id, ...create });

    const list = await request.get(gateway.customers);
    expect(list.status()).toBe(200);
    expect((await list.json()).map((c: { id: number }) => c.id)).toContain(customer.id);

    const update = { name: `${create.name} (updated)`, email: `updated-${suffix}@example.com` };
    const updated = await request.put(`${gateway.customers}/${customer.id}`, { data: update });
    expect(updated.status()).toBe(204);
    expect(await (await request.get(`${gateway.customers}/${customer.id}`)).json()).toMatchObject({
      id: customer.id,
      ...update,
    });

    const deleted = await request.delete(`${gateway.customers}/${customer.id}`);
    expect(deleted.status()).toBe(204);
    expect((await request.get(`${gateway.customers}/${customer.id}`)).status()).toBe(404);
    expect((await request.delete(`${gateway.customers}/${customer.id}`)).status()).toBe(404);
  });

  test('GET unknown customer returns 404', async ({ request }) => {
    const res = await request.get(`${gateway.customers}/2147483647`);
    expect(res.status()).toBe(404);
  });

  test('POST with invalid body returns 400', async ({ request }) => {
    const res = await request.post(gateway.customers, {
      headers: { 'content-type': 'application/json' },
      data: 'not json',
    });
    expect(res.status()).toBe(400);
  });
});
