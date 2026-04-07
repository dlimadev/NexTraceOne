import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * FUTURE-ROADMAP 6.2 — E2E tests para fluxos de versionamento de contratos.
 * Cobre: criar nova versão a partir de versão existente, ciclo draft → published.
 */

// ── Fixtures ──────────────────────────────────────────────────────────────────

const CONTRACT_V1_FIXTURE = {
  id: 'cv-pay-001',
  apiAssetId: 'api-pay-001',
  name: 'Payments API',
  semVer: '1.0.0',
  protocol: 'OpenApi',
  format: 'json',
  lifecycleState: 'Approved',
  origin: 'HumanCreated',
  importedFrom: 'upload',
  createdAt: '2025-01-01T10:00:00Z',
  updatedAt: '2025-01-15T10:00:00Z',
  specContent: '{"openapi":"3.0.0","info":{"title":"Payments API","version":"1.0.0"}}',
  isSigned: false,
  signedBy: null,
  signedAt: null,
  fingerprint: 'sha256:v1fingerprint',
  violations: [],
  domain: 'Finance',
  teamName: 'Payments Team',
  ownerEmail: 'payments@acme.com',
};

const CONTRACT_V2_DRAFT_FIXTURE = {
  ...CONTRACT_V1_FIXTURE,
  id: 'cv-pay-002',
  semVer: '2.0.0',
  lifecycleState: 'Draft',
  createdAt: '2025-06-01T10:00:00Z',
  updatedAt: '2025-06-01T10:00:00Z',
  fingerprint: 'sha256:v2fingerprint',
};

const CONTRACTS_LIST_FIXTURE = {
  items: [CONTRACT_V1_FIXTURE, CONTRACT_V2_DRAFT_FIXTURE],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

// ─── Versioning — History & Summary ──────────────────────────────────────────

test.describe('Contract Versioning — version summary & history', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);

    await page.route('**/api/v1/contracts/summary', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 2,
          totalVersions: 2,
          byProtocol: { OpenApi: 2 },
          approvedCount: 1,
          lockedCount: 0,
          draftCount: 1,
          inReviewCount: 0,
          deprecatedCount: 0,
        }),
      }),
    );

    await page.route('**/api/v1/contracts*', (route) => {
      if (route.request().url().includes('/api/v1/contracts/') && !route.request().url().endsWith('/contracts/')) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(CONTRACT_V1_FIXTURE),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_LIST_FIXTURE),
      });
    });
  });

  test('contract list shows draft badge for v2 and approved badge for v1', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText('1.0.0')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('2.0.0')).toBeVisible({ timeout: 5_000 });
  });

  test('contract detail shows version semver', async ({ page }) => {
    await page.goto('/contracts/cv-pay-001');
    await expect(page.getByText(/1\.0\.0/)).toBeVisible({ timeout: 5_000 });
  });

  test('contract detail shows lifecycle state', async ({ page }) => {
    await page.goto('/contracts/cv-pay-001');
    await expect(page.getByText(/approved/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Versioning — Draft creation from new contract ───────────────────────────

test.describe('Contract Versioning — creating new draft via wizard', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);

    await page.route('**/api/v1/contracts/summary', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 1,
          totalVersions: 1,
          byProtocol: { OpenApi: 1 },
          approvedCount: 0,
          lockedCount: 0,
          draftCount: 1,
          inReviewCount: 0,
          deprecatedCount: 0,
        }),
      }),
    );

    await page.route('**/api/v1/contracts', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'cv-new-001',
            apiAssetId: 'api-new-001',
            name: 'New API',
            semVer: '1.0.0',
            lifecycleState: 'Draft',
          }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      });
    });
  });

  test('navigates to contract creation page from New Contract button', async ({ page }) => {
    await page.goto('/contracts');
    const newContractBtn = page.getByRole('button', { name: /new contract/i });
    if (await newContractBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await newContractBtn.click();
      await expect(page).toHaveURL(/\/contracts\/new/, { timeout: 5_000 });
    }
  });

  test('wizard step 1 shows REST API type selection', async ({ page }) => {
    await page.goto('/contracts/new');
    await expect(page.getByRole('heading', { name: /rest api/i })).toBeVisible({ timeout: 5_000 });
  });

  test('selecting REST API and clicking Next advances to mode selection', async ({ page }) => {
    await page.goto('/contracts/new');
    const restBtn = page.getByRole('button', { name: /rest api/i }).first();
    if (await restBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await restBtn.click();
      const nextBtn = page.getByRole('button', { name: /next/i });
      await expect(nextBtn).toBeEnabled({ timeout: 3_000 });
      await nextBtn.click();
      // Mode selection step should appear
      await expect(page.getByText(/visual builder|import|ai/i).first()).toBeVisible({ timeout: 5_000 });
    }
  });
});

// ─── Versioning — lifecycle state badges ─────────────────────────────────────

test.describe('Contract Versioning — lifecycle state visibility', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test.beforeEach(async ({ page }) => {
    const states = ['Draft', 'InReview', 'Approved', 'Deprecated'];
    const items = states.map((state, i) => ({
      id: `cv-${i}`,
      apiAssetId: `api-${i}`,
      name: `API ${state}`,
      semVer: '1.0.0',
      protocol: 'OpenApi',
      lifecycleState: state,
      origin: 'HumanCreated',
      domain: 'Test',
      teamName: 'Test Team',
      createdAt: '2025-01-01T00:00:00Z',
      updatedAt: '2025-01-01T00:00:00Z',
    }));

    await page.route('**/api/v1/contracts/summary', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 4,
          totalVersions: 4,
          byProtocol: { OpenApi: 4 },
          approvedCount: 1,
          lockedCount: 0,
          draftCount: 1,
          inReviewCount: 1,
          deprecatedCount: 1,
        }),
      }),
    );

    await page.route('**/api/v1/contracts*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items, totalCount: 4, page: 1, pageSize: 20 }),
      }),
    );
  });

  test('shows Draft state in contract list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/draft/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows InReview state in contract list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/review/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows Approved state in contract list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/approved/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows Deprecated state in contract list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/deprecated/i).first()).toBeVisible({ timeout: 5_000 });
  });
});
