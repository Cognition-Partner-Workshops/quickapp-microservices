// Starts the docker compose stack and blocks until every /healthz answers 200.
// Used as the Playwright `webServer` command locally and as a CI step.
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const composeFile = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../src/docker-compose.yml');
const host = process.env.SERVICES_HOST ?? 'localhost';
const gateway = process.env.PLAYWRIGHT_BASE_URL ?? process.env.BASE_URL ?? `http://${host}:5000`;
const targets = {
  gateway,
  identity: process.env.IDENTITY_URL ?? `http://${host}:5001`,
  customer: process.env.CUSTOMER_URL ?? `http://${host}:5002`,
  order: process.env.ORDER_URL ?? `http://${host}:5003`,
  product: process.env.PRODUCT_URL ?? `http://${host}:5004`,
  notification: process.env.NOTIFICATION_URL ?? `http://${host}:5005`,
};
const timeoutMs = Number(process.env.STACK_TIMEOUT_MS ?? 10 * 60 * 1000);

if (process.env.SKIP_COMPOSE_UP !== '1') {
  const args = ['compose', '-f', composeFile, 'up', '-d'];
  if (process.env.COMPOSE_NO_BUILD !== '1') args.push('--build');
  console.log(`> docker ${args.join(' ')}`);
  const result = spawnSync('docker', args, { stdio: 'inherit' });
  if (result.status !== 0) {
    console.error('docker compose up failed');
    process.exit(result.status ?? 1);
  }
}

const healthy = async (url) => {
  try {
    const res = await fetch(`${url}/healthz`, { signal: AbortSignal.timeout(3000) });
    return res.status === 200;
  } catch {
    return false;
  }
};

const deadline = Date.now() + timeoutMs;
const pending = new Set(Object.keys(targets));
while (pending.size > 0) {
  for (const name of [...pending]) {
    if (await healthy(targets[name])) {
      console.log(`healthy: ${name} (${targets[name]})`);
      pending.delete(name);
    }
  }
  if (pending.size === 0) break;
  if (Date.now() > deadline) {
    console.error(`Timed out waiting for /healthz on: ${[...pending].join(', ')}`);
    spawnSync('docker', ['compose', '-f', composeFile, 'ps'], { stdio: 'inherit' });
    spawnSync('docker', ['compose', '-f', composeFile, 'logs', '--tail=50'], { stdio: 'inherit' });
    process.exit(1);
  }
  await new Promise((r) => setTimeout(r, 2000));
}
console.log('All services healthy.');
