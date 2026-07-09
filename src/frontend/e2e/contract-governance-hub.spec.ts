import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — hub de governança lança as ferramentas da jornada. */
test.describe('Contract governance hub', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ distinctContracts: 0, draftCount: 0, inReviewCount: 0, approvedCount: 0, byProtocol: [], byLifecycle: [] }),
      }));
    await page.route('**/api/v1/contracts/list**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }));
  });

  test('hub launches the playground tool', async ({ page }) => {
    await page.goto('/contracts/governance');
    await expect(page.getByText(/governance tools/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('link', { name: /playground/i }).click();
    await expect(page).toHaveURL(/\/contracts\/playground/, { timeout: 5_000 });
  });
});
