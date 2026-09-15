const host = process.env.SERVICES_HOST ?? 'localhost';

/** API gateway (YARP). Overridable via BASE_URL / PLAYWRIGHT_BASE_URL. */
export const gatewayUrl =
  process.env.PLAYWRIGHT_BASE_URL ?? process.env.BASE_URL ?? `http://${host}:5000`;

/** Direct service URLs. Each can be overridden individually (e.g. IDENTITY_URL). */
export const services = {
  identity: process.env.IDENTITY_URL ?? `http://${host}:5001`,
  customer: process.env.CUSTOMER_URL ?? `http://${host}:5002`,
  order: process.env.ORDER_URL ?? `http://${host}:5003`,
  product: process.env.PRODUCT_URL ?? `http://${host}:5004`,
  notification: process.env.NOTIFICATION_URL ?? `http://${host}:5005`,
} as const;

export type ServiceName = keyof typeof services;

/** Every URL that must answer /healthz before the suite runs. */
export const healthTargets: Record<string, string> = { gateway: gatewayUrl, ...services };

/**
 * Gateway routes. YARP matches `/api/<plural>/{**catch-all}` and only strips
 * that prefix, so the downstream controller route (`/api/<controller>`) must be
 * repeated after it.
 */
export const gateway = {
  identity: `${gatewayUrl}/api/identity/api/identity`,
  customers: `${gatewayUrl}/api/customers/api/customer`,
  orders: `${gatewayUrl}/api/orders/api/order`,
  products: `${gatewayUrl}/api/products/api/product`,
  notifications: `${gatewayUrl}/api/notifications/api/notification`,
} as const;
