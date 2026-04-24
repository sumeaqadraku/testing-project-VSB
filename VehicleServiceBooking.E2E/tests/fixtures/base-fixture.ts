import { test as base, expect, type Page } from '@playwright/test';

// Kredencialet e seeded nga DbInitializer + të krijuara gjatë setup
const TEST_USERS = {
  manager:  { email: 'manager@vehicleservice.com', password: 'Manager@123' },
  mechanic: { email: 'mechanic@test.com',          password: 'Mechanic@123!' },
  client:   { email: 'client@test.com',            password: 'Client@123!' },
  client2:  { email: 'client2@test.com',           password: 'Client@123!' },
} as const;

type Role = keyof typeof TEST_USERS;

type AuthFixtures = {
  loginAs: (role: Role) => Promise<void>;
  apiLogin: (role: Role) => Promise<string>; // kthen JWT token
};

async function loginViaUI(page: Page, role: Role): Promise<void> {
  const { email, password } = TEST_USERS[role];
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(email);
  await page.getByLabel(/password|fjalëkalim/i).fill(password);
  await page.getByRole('button', { name: /login|kyçu|sign in/i }).click();
  await page.waitForURL(/\/(manager|mechanic|client)/, { timeout: 10_000 });
}

async function loginViaApi(page: Page, role: Role): Promise<string> {
  const { email, password } = TEST_USERS[role];
  const response = await page.request.post('http://localhost:5294/api/auth/login', {
    data: { email, password },
  });
  const body = await response.json();
  return body.token as string;
}

export const test = base.extend<AuthFixtures>({
  loginAs: async ({ page }, use) => {
    await use((role) => loginViaUI(page, role));
  },

  apiLogin: async ({ page }, use) => {
    await use((role) => loginViaApi(page, role));
  },
});

export { expect };
export { TEST_USERS };
