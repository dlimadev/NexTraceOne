import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — a timeline pré-carrega pelo apiAssetId do URL, sem digitação manual. */
test.describe('Contract health experience', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/asset-1/health/timeline**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          apiAssetId: 'asset-1',
          points: [
            { semVer: '1.0.0', healthScore: 55, createdAt: '2026-01-01T00:00:00Z', lifecycleState: 'Approved', isBreakingChange: false },
            { semVer: '1.1.0', healthScore: 82, createdAt: '2026-02-01T00:00:00Z', lifecycleState: 'Approved', isBreakingChange: true },
          ],
        }),
      }));
  });

  test('timeline auto-loads from the apiAssetId query param', async ({ page }) => {
    await page.goto('/contracts/health/timeline?apiAssetId=asset-1');
    await expect(page.getByText('1.1.0')).toBeVisible({ timeout: 5_000 });
    await expect(page.locator('polyline')).toBeVisible({ timeout: 5_000 });
  });
});
