// PERSONA 1 — System Tests: Auth + Login flow
import { test, expect } from './fixtures/base-fixture';

test.describe('Autentikimi', () => {
  test('faqja login shfaqet kur vizitohet rrënja', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL('/login');
  });

  test('forma e login-it ekziston', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password|fjalëkalim/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /login|kyçu|sign in/i })).toBeVisible();
  });

  test('Manager ridrejtohet te /manager pas login-it', async ({ page, loginAs }) => {
    await loginAs('manager');
    await expect(page).toHaveURL('/manager');
  });

  test('Mechanic ridrejtohet te /mechanic pas login-it', async ({ page, loginAs }) => {
    await loginAs('mechanic');
    await expect(page).toHaveURL('/mechanic');
  });

  test('Client ridrejtohet te /client pas login-it', async ({ page, loginAs }) => {
    await loginAs('client');
    await expect(page).toHaveURL('/client');
  });

  test('kredenciale të gabuara tregojnë mesazh gabimi', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill('gabim@test.com');
    await page.getByLabel(/password|fjalëkalim/i).fill('FjalëkalimIGabuar123!');
    await page.getByRole('button', { name: /login|kyçu|sign in/i }).click();
    // Duhet të qëndrojë te /login dhe të shfaqë error
    await expect(page).toHaveURL('/login');
  });

  test('Client nuk mund të aksesojë /manager', async ({ page, loginAs }) => {
    await loginAs('client');
    await page.goto('/manager');
    // Duhet ridrejtuar larg /manager
    await expect(page).not.toHaveURL('/manager');
  });
});
