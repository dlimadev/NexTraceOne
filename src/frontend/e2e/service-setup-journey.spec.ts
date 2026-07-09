import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — checklist de setup no detalhe de um serviço Planning + hub self-service. */
test.describe('Service setup journey', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services/svc-1/maturity**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ level: 'Bronze', dimensions: [] }) }));
    await page.route('**/api/v1/catalog/services/svc-1**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
        id: 'svc-1', name: 'orders-api', displayName: 'orders-api', domain: 'Commerce', serviceType: 'RestApi',
        criticality: 'Medium', exposureType: 'Internal', lifecycleStatus: 'Planning', teamName: 'Orders',
        technicalOwner: '', apis: [], apiCount: 0,
      }) }));
    await page.route('**/api/v1/contracts/by-service/**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ contracts: [], totalCount: 0 }) }));
  });

  test('service detail shows the setup checklist and the contract CTA navigates', async ({ page }) => {
    await page.goto('/services/svc-1');
    await expect(page.getByText(/setup checklist/i)).toBeVisible({ timeout: 5_000 });
    await page.getByTestId('setup-cta-contract').click();
    await expect(page).toHaveURL(/\/contracts\/new\?serviceId=svc-1/, { timeout: 5_000 });
  });

  test('self-service hub leads with onboarding golden path', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }));
    await page.goto('/catalog/self-service');
    await expect(page.getByRole('link', { name: /onboard a service/i })).toBeVisible({ timeout: 5_000 });
  });
});
