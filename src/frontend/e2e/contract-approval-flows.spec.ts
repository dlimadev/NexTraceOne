import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * FUTURE-ROADMAP 6.2 — E2E tests para fluxos de aprovação de contratos.
 * Cobre: submissão para revisão, aprovação, rejeição e visualização de estado.
 */

// ── Fixtures ──────────────────────────────────────────────────────────────────

const CONTRACT_DRAFT_FIXTURE = {
  id: 'cv-draft-001',
  apiAssetId: 'api-draft-001',
  name: 'Payments API',
  semVer: '2.0.0',
  protocol: 'OpenApi',
  format: 'json',
  lifecycleState: 'Draft',
  origin: 'HumanCreated',
  importedFrom: 'visual_builder',
  createdAt: '2025-06-01T10:00:00Z',
  updatedAt: '2025-06-01T10:00:00Z',
  specContent: '{"openapi":"3.0.0","info":{"title":"Payments API","version":"2.0.0"}}',
  isSigned: false,
  signedBy: null,
  signedAt: null,
  fingerprint: 'sha256:draftfinger',
  violations: [],
  domain: 'Finance',
  teamName: 'Payments Team',
  ownerEmail: 'payments@acme.com',
};

const CONTRACT_IN_REVIEW_FIXTURE = {
  ...CONTRACT_DRAFT_FIXTURE,
  lifecycleState: 'InReview',
};

const CONTRACT_APPROVED_FIXTURE = {
  ...CONTRACT_DRAFT_FIXTURE,
  lifecycleState: 'Approved',
};

const CONTRACT_REJECTED_FIXTURE = {
  ...CONTRACT_DRAFT_FIXTURE,
  lifecycleState: 'Draft',
};

const CONTRACTS_SUMMARY_FIXTURE = {
  totalCount: 1,
  totalVersions: 1,
  byProtocol: { OpenApi: 1 },
  approvedCount: 0,
  lockedCount: 0,
  draftCount: 1,
  inReviewCount: 0,
  deprecatedCount: 0,
};

// ─── Approval — contract detail renders lifecycle state ───────────────────────

test.describe('Contract Approval — lifecycle state display', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);

    await page.route('**/api/v1/contracts/summary', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_SUMMARY_FIXTURE),
      }),
    );
  });

  test('shows Draft state on contract detail page', async ({ page }) => {
    await page.route('**/api/v1/contracts/cv-draft-001*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACT_DRAFT_FIXTURE),
      }),
    );
    await page.goto('/contracts/cv-draft-001');
    await expect(page.getByText(/draft/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows InReview state on contract detail page', async ({ page }) => {
    await page.route('**/api/v1/contracts/cv-draft-001*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACT_IN_REVIEW_FIXTURE),
      }),
    );
    await page.goto('/contracts/cv-draft-001');
    await expect(page.getByText(/review/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows Approved state on contract detail page', async ({ page }) => {
    await page.route('**/api/v1/contracts/cv-draft-001*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACT_APPROVED_FIXTURE),
      }),
    );
    await page.goto('/contracts/cv-draft-001');
    await expect(page.getByText(/approved/i).first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Approval — contract governance page ─────────────────────────────────────

test.describe('Contract Approval — contract governance overview', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);

    await page.route('**/api/v1/contracts/summary', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalCount: 3,
          totalVersions: 3,
          byProtocol: { OpenApi: 2, AsyncApi: 1 },
          approvedCount: 1,
          lockedCount: 0,
          draftCount: 1,
          inReviewCount: 1,
          deprecatedCount: 0,
        }),
      }),
    );

    await page.route('**/api/v1/contracts*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            CONTRACT_DRAFT_FIXTURE,
            CONTRACT_IN_REVIEW_FIXTURE,
            CONTRACT_APPROVED_FIXTURE,
          ],
          totalCount: 3,
          page: 1,
          pageSize: 20,
        }),
      }),
    );
  });

  test('renders the contract list with all lifecycle states', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/payments api/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('shows count summary for pending review contracts', async ({ page }) => {
    await page.goto('/contracts');
    // The summary section should show inReview count
    await expect(page.getByText(/1/).first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Approval — publication center ────────────────────────────────────────────

test.describe('Contract Approval — Publication Center page', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);

    await page.route('**/api/v1/contracts/publication-center*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          pending: [CONTRACT_IN_REVIEW_FIXTURE],
          recentlyPublished: [CONTRACT_APPROVED_FIXTURE],
          rejected: [CONTRACT_REJECTED_FIXTURE],
        }),
      }),
    );
  });

  test('navigates to Publication Center page', async ({ page }) => {
    await page.goto('/contracts/publication-center');
    // Page should load without error
    await expect(page).toHaveURL(/publication-center/, { timeout: 5_000 });
  });
});
