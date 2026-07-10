import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — a Publication liga cada entrada ao contrato. */
test.describe('Contract enforcement hardening', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/publication-center**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{ publicationEntryId: 'pe-1', contractVersionId: 'cv-1', apiAssetId: 'a-1', contractTitle: 'orders-api', semVer: '1.0.0', status: 'Published', visibility: 'Public', publishedBy: 'me' }],
          totalCount: 1,
        }),
      }));
  });

  test('publication entry title links to the contract', async ({ page }) => {
    await page.goto('/contracts/publication');
    const link = page.getByRole('link', { name: 'orders-api' });
    await expect(link).toBeVisible({ timeout: 5_000 });
    await expect(link).toHaveAttribute('href', '/contracts/cv-1');
  });
});
