import { test, expect, type Page } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * E2E dos fluxos de versionamento de contratos (v5).
 * Cobre: badges de estado na listagem, semver/lifecycle no workspace de detalhe,
 * e entrada no wizard de criação (service-first).
 *
 * Endpoints v5: GET /contracts/summary + /contracts/list (catálogo),
 * GET /contracts/:id/detail (workspace), GET /catalog/services (wizard).
 */

// ── Fixtures ──────────────────────────────────────────────────────────────────

const CONTRACT_V1_DETAIL = {
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

function listItem(id: string, name: string, semVer: string, lifecycleState: string) {
  return {
    id,
    versionId: id,
    apiAssetId: `api-${id}`,
    name,
    semVer,
    protocol: 'OpenApi',
    lifecycleState,
    origin: 'HumanCreated',
    domain: 'Finance',
    teamName: 'Payments Team',
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  };
}

const SUMMARY = {
  totalCount: 2,
  totalVersions: 2,
  byProtocol: { OpenApi: 2 },
  approvedCount: 1,
  lockedCount: 0,
  draftCount: 1,
  inReviewCount: 0,
  deprecatedCount: 0,
};

async function mockSummary(page: Page, summary: unknown = SUMMARY) {
  await page.route('**/api/v1/contracts/summary**', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(summary) }),
  );
}

async function mockList(page: Page, items: unknown[]) {
  await page.route('**/api/v1/contracts/list**', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items, totalCount: items.length, page: 1, pageSize: 20 }),
    }),
  );
}

// ─── Versionamento — badges de estado na listagem ────────────────────────────

test.describe('Contract Versioning — badges de estado na listagem', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await mockSummary(page);
    await mockList(page, [
      listItem('cv-pay-001', 'Payments API', '1.0.0', 'Approved'),
      listItem('cv-pay-002', 'Payments API', '2.0.0', 'Draft'),
    ]);
  });

  test('a listagem mostra badge Approved (v1) e Draft (v2)', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/approved/i).first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/draft/i).first()).toBeVisible();
  });
});

// ─── Versionamento — semver + lifecycle no workspace de detalhe ───────────────

test.describe('Contract Versioning — detalhe (workspace)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/cv-pay-001/detail**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CONTRACT_V1_DETAIL) }),
    );
    await page.route('**/api/v1/contracts/history/api-pay-001**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ apiAssetId: 'api-pay-001', versions: [CONTRACT_V1_DETAIL] }),
      }),
    );
  });

  test('o detalhe mostra o semver da versão', async ({ page }) => {
    await page.goto('/contracts/cv-pay-001');
    await expect(page.getByText(/1\.0\.0/).first()).toBeVisible({ timeout: 5_000 });
  });

  test('o detalhe mostra o estado de ciclo de vida', async ({ page }) => {
    await page.goto('/contracts/cv-pay-001');
    await expect(page.getByText(/approved/i).first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Versionamento — entrada no wizard de criação ────────────────────────────

test.describe('Contract Versioning — wizard de criação (service-first)', () => {
  const WIZARD_SERVICE = {
    items: [
      {
        serviceId: 'svc-pay-001',
        name: 'payments-service',
        displayName: 'Payments Service',
        serviceType: 'RestApi',
        domain: 'Finance',
        teamName: 'Payments Team',
        criticality: 'High',
        lifecycleStatus: 'Active',
        exposureType: 'External',
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  };

  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('navega para /contracts/new a partir do botão New Contract', async ({ page }) => {
    await mockSummary(page);
    await mockList(page, [listItem('cv-pay-001', 'Payments API', '1.0.0', 'Approved')]);
    await page.goto('/contracts');
    await page.getByRole('button', { name: /new contract/i }).click();
    await expect(page).toHaveURL(/\/contracts\/new/, { timeout: 5_000 });
  });

  test('passo 1 do wizard lista serviços e o passo 2 mostra o tipo REST API', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(WIZARD_SERVICE) }),
    );
    await page.goto('/contracts/new');
    await page.getByRole('button', { name: /Payments Service/i }).click();
    await page.getByRole('button', { name: /next/i }).click();
    await expect(page.getByRole('button', { name: /rest api/i }).first()).toBeVisible({ timeout: 5_000 });
  });

  test('selecionar REST API revela a secção de modo de criação', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(WIZARD_SERVICE) }),
    );
    await page.goto('/contracts/new');
    await page.getByRole('button', { name: /Payments Service/i }).click();
    await page.getByRole('button', { name: /next/i }).click();
    await page.getByRole('button', { name: /rest api/i }).first().click();
    await expect(page.getByText(/how do you want to create it/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Versionamento — visibilidade dos estados na listagem ────────────────────

test.describe('Contract Versioning — visibilidade dos estados', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await mockSummary(page, {
      totalCount: 4,
      totalVersions: 4,
      byProtocol: { OpenApi: 4 },
      approvedCount: 1,
      lockedCount: 0,
      draftCount: 1,
      inReviewCount: 1,
      deprecatedCount: 1,
    });
    await mockList(page, [
      listItem('cv-0', 'API Draft', '1.0.0', 'Draft'),
      listItem('cv-1', 'API InReview', '1.0.0', 'InReview'),
      listItem('cv-2', 'API Approved', '1.0.0', 'Approved'),
      listItem('cv-3', 'API Deprecated', '1.0.0', 'Deprecated'),
    ]);
  });

  test('mostra o estado Draft na listagem', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/draft/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('mostra o estado In Review na listagem', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/review/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('mostra o estado Approved na listagem', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/approved/i).first()).toBeVisible({ timeout: 5_000 });
  });

  test('mostra o estado Deprecated na listagem', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText(/deprecated/i).first()).toBeVisible({ timeout: 5_000 });
  });
});
