import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — o playground pré-carrega pelo contractVersionId do URL e volta ao portal. */
test.describe('Contract consumer journey', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/cv-1/detail**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ contractVersionId: 'cv-1', apiAssetId: 'a-1', apiName: 'orders-api', protocol: 'OpenApi', semVer: '1.0.0', lifecycleState: 'Approved', spec: '{"paths":{}}' }),
      }));
  });

  test('playground auto-loads from the contractVersionId query param', async ({ page }) => {
    await page.goto('/contracts/playground?contractVersionId=cv-1');
    await expect(page.getByRole('link', { name: /back to portal/i })).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/v1\.0\.0/)).toBeVisible({ timeout: 5_000 });
  });
});
