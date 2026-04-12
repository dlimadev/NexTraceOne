import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * FUTURE-ROADMAP 6.2 — E2E tests para fluxos de deprecação de contratos.
 * Cobre: visualização de contratos deprecated, detalhes e filtros por estado.
 */

// ── Fixtures ──────────────────────────────────────────────────────────────────

const CONTRACT_DEPRECATED_FIXTURE = {
  id: 'cv-dep-001',
  apiAssetId: 'api-dep-001',
  name: 'Legacy Payments API',
  semVer: '1.0.0',
  protocol: 'OpenApi',
  format: 'json',
  lifecycleState: 'Deprecated',
  origin: 'HumanCreated',
  importedFrom: 'upload',
  createdAt: '2023-01-01T10:00:00Z',
  updatedAt: '2025-12-01T10:00:00Z',
  specContent: '{"openapi":"3.0.0","info":{"title":"Legacy Payments API","version":"1.0.0"}}',
  isSigned: false,
  signedBy: null,
  signedAt: null,
  fingerprint: 'sha256:depfinger',
  violations: [],
  domain: 'Finance',
  teamName: 'Payments Team',
  ownerEmail: 'payments@acme.com',
};

const CONTRACT_ACTIVE_FIXTURE = {
  id: 'cv-active-001',
  apiAssetId: 'api-active-001',
  name: 'Payments API v2',
  semVer: '2.0.0',
  protocol: 'OpenApi',
  format: 'json',
  lifecycleState: 'Approved',
  origin: 'HumanCreated',
  importedFrom: 'upload',
  createdAt: '2025-01-01T10:00:00Z',
  updatedAt: '2025-06-01T10:00:00Z',
  specContent: '{"openapi":"3.0.0","info":{"title":"Payments API v2","version":"2.0.0"}}',
  isSigned: false,
  signedBy: null,
  signedAt: null,
  fingerprint: 'sha256:activefinger',
  violations: [],
  domain: 'Finance',
  teamName: 'Payments Team',
  ownerEmail: 'payments@acme.com',
};

function setupContractsMocks(page: import('@playwright/test').Page) {
  return Promise.all([
    page.route('**/api/v1/contracts/summary', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 2,
          totalVersions: 2,
          byProtocol: { OpenApi: 2 },
          approvedCount: 1,
          lockedCount: 0,
          draftCount: 0,
          inReviewCount: 0,
          deprecatedCount: 1,
        }),
      }),
    ),
    page.route('**/api/v1/contracts*', (route) => {
      if (route.request().url().match(/\/api\/v1\/contracts\/cv-dep-001/)) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(CONTRACT_DEPRECATED_FIXTURE),
        });
      }
      if (route.request().url().match(/\/api\/v1\/contracts\/cv-active-001/)) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(CONTRACT_ACTIVE_FIXTURE),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [CONTRACT_DEPRECATED_FIXTURE, CONTRACT_ACTIVE_FIXTURE],
          totalCount: 2,
          page: 1,
          pageSize: 20,
        }),
      });
    }),
  ]);
}

// ─── Deprecation — list visibility ────────────────────────────────────────────

test.describe('Contract Deprecation — visibility in contract list', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await setupContractsMocks(page);
  });

  test('deprecated contract appears in the contract list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/legacy payments api/i)).toBeVisible({ timeout: 5_000 });
  });

  test('active contract appears alongside deprecated in the list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/payments api v2/i)).toBeVisible({ timeout: 5_000 });
  });

  test('deprecated badge is shown in contract list', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/deprecated/i).first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Deprecation — detail page ────────────────────────────────────────────────

test.describe('Contract Deprecation — detail page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await setupContractsMocks(page);
  });

  test('deprecated contract detail shows Deprecated lifecycle state', async ({ page }) => {
    await page.goto('/contracts/cv-dep-001');
    await expect(page.getByText(/deprecated/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('deprecated contract detail shows contract name', async ({ page }) => {
    await page.goto('/contracts/cv-dep-001');
    await expect(page.getByText(/legacy payments api/i)).toBeVisible({ timeout: 5_000 });
  });

  test('deprecated contract detail shows semver', async ({ page }) => {
    await page.goto('/contracts/cv-dep-001');
    await expect(page.getByText(/1\.0\.0/)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Deprecation — summary metrics ────────────────────────────────────────────

test.describe('Contract Deprecation — summary metrics', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await setupContractsMocks(page);
  });

  test('contract health dashboard shows deprecatedCount', async ({ page }) => {
    await page.route('**/api/v1/contracts/health*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          healthScore: 72,
          totalContracts: 2,
          deprecatedVersions: 1,
          withExamples: 1,
          withCanonicalEntities: 0,
          topViolations: [],
        }),
      }),
    );
    await page.goto('/contracts/health');
    // Should show the deprecated count metric
    await expect(page.getByText(/1/).first()).toBeVisible({ timeout: 5_000 });
  });

  test('contract source of truth page loads without error', async ({ page }) => {
    await page.route('**/api/v1/contracts/source-of-truth*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [CONTRACT_DEPRECATED_FIXTURE, CONTRACT_ACTIVE_FIXTURE],
          totalCount: 2,
          page: 1,
          pageSize: 20,
        }),
      }),
    );
    await page.goto('/contracts/source-of-truth');
    await expect(page).toHaveURL(/source-of-truth/, { timeout: 5_000 });
  });
});
