# End-to-end tests (Playwright)

Playwright suite that exercises the whole docker compose stack: health checks, Swagger UI,
gateway routing, Customer/Product CRUD through the gateway, and the Notification
order-placed → preview flow (rendered in a real Chromium browser).

## Prerequisites

- Docker with `docker compose`
- Node.js 20+

## Run locally

```bash
cd e2e
npm ci
npx playwright install --with-deps chromium
npm run test:e2e
```

`npm run test:e2e` uses Playwright's `webServer` hook to run `scripts/start-stack.mjs`, which
does `docker compose -f ../src/docker-compose.yml up -d --build` and waits until every
`/healthz` (gateway + five services) returns 200. If the stack is already up it is reused.

Tear down afterwards with `npm run stack:down` (runs `docker compose down -v`).

## Useful environment variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `BASE_URL` / `PLAYWRIGHT_BASE_URL` | `http://localhost:5000` | API gateway base URL |
| `SERVICES_HOST` | `localhost` | Host used for the direct service URLs (ports 5001–5005) |
| `IDENTITY_URL`, `CUSTOMER_URL`, `ORDER_URL`, `PRODUCT_URL`, `NOTIFICATION_URL` | `http://<SERVICES_HOST>:500x` | Override individual service URLs |
| `SKIP_WEBSERVER=1` | unset | Don't start docker compose; assume the stack is already running (CI does this) |
| `COMPOSE_NO_BUILD=1` | unset | `docker compose up` without `--build` |
| `POSTGRES_HOST_PORT` | `5432` | Host port for the compose Postgres (set e.g. `55432` if you already run Postgres locally) |

## Layout

- `playwright.config.ts` – config, `webServer` hook, `BASE_URL` handling
- `helpers/urls.ts` – gateway and service URL helpers
- `scripts/start-stack.mjs` – start compose and wait for health
- `tests/*.spec.ts` – the tests

The HTML report is written to `e2e/playwright-report/` (open with `npx playwright show-report`).
