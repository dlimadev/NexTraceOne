import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E — jornada de onboarding de serviço (/services/onboard).
 * Cobre: criação do serviço no passo 1, saltar interface e contrato, concluir.
 */
test.describe('Service onboarding journey', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ id: 'svc-e2e-1' }) });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) });
    });
    await page.route('**/api/v1/catalog/services/**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ id: 'svc-e2e-1', name: 'orders-api', displayName: 'orders-api', domain: 'Commerce', serviceType: 'RestApi', lifecycleStatus: 'Planning', apis: [] }) }),
    );
  });

  test('creates a service then skips interface and contract to finish', async ({ page }) => {
    await page.goto('/services/onboard');

    // Passo 1: identidade
    await page.getByLabel(/service name/i).fill('orders-api');
    await page.getByLabel(/domain/i).fill('Commerce');
    await page.getByLabel(/team/i).fill('Orders');
    await page.getByRole('button', { name: /next/i }).click();

    // Passo 2: interface (saltar)
    await expect(page.getByText(/expose an interface/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: /skip/i }).click();

    // Passo 3: contrato (saltar)
    await expect(page.getByText(/define a contract/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: /skip/i }).click();

    // Passo 4: revisão → concluir
    await expect(page.getByText(/review & create/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: /finish/i }).click();

    await expect(page).toHaveURL(/\/services\/svc-e2e-1/, { timeout: 5_000 });
  });

  test('dead-CTA repair: /services/new redirects to onboard', async ({ page }) => {
    await page.goto('/services/new');
    await expect(page).toHaveURL(/\/services\/onboard/, { timeout: 5_000 });
    await expect(page.getByLabel(/service name/i)).toBeVisible({ timeout: 5_000 });
  });
});
