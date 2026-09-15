import { test, expect } from '@playwright/test';
import { gateway } from '../helpers/urls';

test.describe('Product CRUD through the gateway', () => {
  test('create → get → update → delete round-trip', async ({ request }) => {
    const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const create = { name: `E2E Product ${suffix}`, sku: `E2E-${suffix}`, price: 19.99, stockQuantity: 5 };

    const created = await request.post(gateway.products, { data: create });
    expect(created.status()).toBe(201);
    const product = await created.json();
    expect(product).toMatchObject(create);
    expect(typeof product.id).toBe('number');
    expect(created.headers()['location'].toLowerCase()).toContain(`/api/product/${product.id}`);

    const fetched = await request.get(`${gateway.products}/${product.id}`);
    expect(fetched.status()).toBe(200);
    expect(await fetched.json()).toMatchObject({ id: product.id, ...create });

    const list = await request.get(gateway.products);
    expect(list.status()).toBe(200);
    expect((await list.json()).map((p: { id: number }) => p.id)).toContain(product.id);

    const update = { ...create, name: `${create.name} (updated)`, price: 24.5, stockQuantity: 0 };
    const updated = await request.put(`${gateway.products}/${product.id}`, { data: update });
    expect(updated.status()).toBe(204);
    expect(await (await request.get(`${gateway.products}/${product.id}`)).json()).toMatchObject({
      id: product.id,
      ...update,
    });

    const deleted = await request.delete(`${gateway.products}/${product.id}`);
    expect(deleted.status()).toBe(204);
    expect((await request.get(`${gateway.products}/${product.id}`)).status()).toBe(404);
    expect((await request.put(`${gateway.products}/${product.id}`, { data: update })).status()).toBe(404);
  });

  test('GET unknown product returns 404', async ({ request }) => {
    const res = await request.get(`${gateway.products}/2147483647`);
    expect(res.status()).toBe(404);
  });
});
