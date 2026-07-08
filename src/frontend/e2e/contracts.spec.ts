import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * Massa de teste estável para Contract Governance.
 * Cobre os principais fluxos: listagem, criação de draft, ciclo de vida e detalhe.
 */

const CONTRACTS_SUMMARY_FIXTURE = {
  totalCount: 3,
  totalVersions: 3,
  byProtocol: { OpenApi: 2, AsyncApi: 1 },
  approvedCount: 1,
  lockedCount: 0,
  draftCount: 1,
  inReviewCount: 1,
  deprecatedCount: 0,
};

const CONTRACTS_LIST_FIXTURE = {
  items: [
    {
      id: 'cv-pay-001',
      versionId: 'cv-pay-001',
      apiAssetId: 'api-pay-001',
      name: 'Payments API',
      semVer: '2.1.0',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
      origin: 'HumanCreated',
      domain: 'Finance',
      teamName: 'Payments Team',
      createdAt: '2025-02-01T10:00:00Z',
      updatedAt: '2025-03-01T14:00:00Z',
    },
    {
      id: 'cv-auth-001',
      versionId: 'cv-auth-001',
      apiAssetId: 'api-auth-001',
      name: 'Auth API',
      semVer: '1.0.0',
      protocol: 'OpenApi',
      lifecycleState: 'Draft',
      origin: 'HumanCreated',
      domain: 'Platform',
      teamName: 'Identity Team',
      createdAt: '2025-03-10T09:00:00Z',
      updatedAt: '2025-03-10T09:00:00Z',
    },
    {
      id: 'cv-events-001',
      versionId: 'cv-events-001',
      apiAssetId: 'api-events-001',
      name: 'Events API',
      semVer: '3.0.0',
      protocol: 'AsyncApi',
      lifecycleState: 'InReview',
      origin: 'AiGenerated',
      domain: 'Platform',
      teamName: 'Platform Team',
      createdAt: '2025-03-05T12:00:00Z',
      updatedAt: '2025-03-12T10:00:00Z',
    },
  ],
  totalCount: 3,
  page: 1,
  pageSize: 20,
};

const CONTRACT_DETAIL_FIXTURE = {
  id: 'cv-pay-001',
  apiAssetId: 'api-pay-001',
  name: 'Payments API',
  semVer: '2.1.0',
  protocol: 'OpenApi',
  format: 'json',
  lifecycleState: 'Approved',
  origin: 'HumanCreated',
  importedFrom: 'upload',
  createdAt: '2025-02-01T10:00:00Z',
  updatedAt: '2025-03-01T14:00:00Z',
  specContent: '{"openapi":"3.0.0","info":{"title":"Payments API","version":"2.1.0"}}',
  isSigned: false,
  signedBy: null,
  signedAt: null,
  fingerprint: 'sha256:abc123def456',
  violations: [],
  domain: 'Finance',
  teamName: 'Payments Team',
  ownerEmail: 'payments@acme.com',
};

// ─── Contract Catalog — Listagem ──────────────────────────────────────────────

test.describe('Contract Governance — listagem', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/contracts/list**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_LIST_FIXTURE),
      }),
    );
  });

  test('exibe o título do catálogo de contratos', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByRole('heading', { name: /contract catalog/i })).toBeVisible({ timeout: 5_000 });
  });

  test('lista os contratos devolvidos pela API', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText('Payments API')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Auth API')).toBeVisible();
    await expect(page.getByText('Events API')).toBeVisible();
  });

  test('exibe os badges de protocolo e estado', async ({ page }) => {
    await page.goto('/contracts');
    await expect(page.getByText('Payments API').first()).toBeVisible({ timeout: 5_000 });
    // Ciclos de vida
    await expect(page.getByText('Approved').first()).toBeVisible();
    await expect(page.getByText('Draft').first()).toBeVisible();
    await expect(page.getByText('In Review').first()).toBeVisible();
  });

  test('exibe o botão de criar novo contrato', async ({ page }) => {
    await page.goto('/contracts');
    // v5: o CTA é um Button "New contract" (navega via onClick), não um <link>.
    await expect(page.getByRole('button', { name: /new contract/i })).toBeVisible({ timeout: 5_000 });
  });

  test('exibe estado vazio quando a API devolve lista vazia', async ({ page }) => {
    await page.route('**/api/v1/contracts/list**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      }),
    );
    await page.goto('/contracts');
    await expect(page.getByText(/no contracts/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Contract Detail ──────────────────────────────────────────────────────────

test.describe('Contract Governance — detalhe', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/cv-pay-001/detail**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACT_DETAIL_FIXTURE),
      }),
    );
    // Histórico de versões
    await page.route('**/api/v1/contracts/history/api-pay-001**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          apiAssetId: 'api-pay-001',
          versions: [CONTRACT_DETAIL_FIXTURE],
        }),
      }),
    );
  });

  test('exibe o nome e versão do contrato no detalhe', async ({ page }) => {
    await page.goto('/contracts/cv-pay-001');
    // The contract detail heading shows the apiAssetId (appears multiple times, use heading)
    await expect(page.getByRole('heading', { name: 'api-pay-001' }).first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('2.1.0').first()).toBeVisible();
  });

  test('exibe o protocolo e estado de ciclo de vida', async ({ page }) => {
    await page.goto('/contracts/cv-pay-001');
    await expect(page.getByText('OpenApi').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Approved').first()).toBeVisible();
  });

  test('exibe o fingerprint (hash) do contrato', async ({ page }) => {
    // v5: /contracts/:id é o ContractWorkspacePage. A Validação está na secção
    // "Validation" do grupo "Governance" (WorkspaceTabs: grupo → secção).
    await page.route('**/api/v1/contracts/cv-pay-001/validation-summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalIssues: 0,
          errorCount: 0,
          warningCount: 0,
          infoCount: 0,
          hintCount: 0,
          blockedCount: 0,
          isPublishReady: true,
          isReviewReady: true,
          sources: [],
          validatedAt: '2026-01-01T00:00:00Z',
          fingerprint: 'sha256:abc123def456',
          overallStatus: 'Valid',
        }),
      }),
    );
    await page.goto('/contracts/cv-pay-001');

    // Selecionar o grupo "Governance" (tab superior) e depois a secção "Validation".
    await page.getByRole('tab', { name: /governance/i }).click();
    await page.getByRole('button', { name: /^validation$/i }).click();

    // O fingerprint é exibido na secção de validação.
    await expect(page.getByText(/sha256/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Criação de Contrato (draft) ──────────────────────────────────────────────

test.describe('Contract Governance — criar novo contrato', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('navega para a página de criação ao clicar em "New Contract"', async ({ page }) => {
    await page.route('**/api/v1/contracts/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/contracts/list**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_LIST_FIXTURE),
      }),
    );

    await page.goto('/contracts');
    // v5: CTA é um Button "New contract" que navega para /contracts/new via onClick.
    const createBtn = page.getByRole('button', { name: /new contract/i });
    await expect(createBtn).toBeVisible({ timeout: 5_000 });
    await createBtn.click();
    await expect(page).toHaveURL('/contracts/new');
  });

  // v5: /contracts/new é um wizard de 4 passos (service → typeMode → details → confirm).
  // O passo 1 lista serviços (GET /catalog/services); o tipo de contrato é o passo 2.
  const SERVICES_FIXTURE = {
    items: [
      {
        serviceId: 'svc-pay-001',
        name: 'payments-service',
        displayName: 'Payments Service',
        serviceType: 'RestApi',
        domain: 'Payments',
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

  test('passo 1 do wizard lista os serviços disponíveis', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICES_FIXTURE),
      }),
    );
    await page.goto('/contracts/new');
    // Passo de seleção de serviço — cada serviço é um botão com o displayName.
    await expect(page.getByRole('button', { name: /Payments Service/i })).toBeVisible({ timeout: 5_000 });
  });

  test('selecionar serviço e avançar mostra o tipo REST API e o passo de modo', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICES_FIXTURE),
      }),
    );
    await page.goto('/contracts/new');

    // Passo 1: escolher o serviço.
    await page.getByRole('button', { name: /Payments Service/i }).click();
    // Avançar para o passo typeMode.
    await page.getByRole('button', { name: /next/i }).click();

    // Passo 2: o tipo REST API está disponível (label i18n "REST API").
    const restOption = page.getByRole('button', { name: /rest api/i }).first();
    await expect(restOption).toBeVisible({ timeout: 5_000 });
    await restOption.click();

    // Selecionar o tipo revela a secção de modo de criação (mesmo passo).
    await expect(page.getByText(/how do you want to create it/i)).toBeVisible({ timeout: 3_000 });
  });
});

// ─── Navegação: catálogo → detalhe ───────────────────────────────────────────

test.describe('Contract Governance — navegação catálogo → detalhe', () => {
  test('clica num contrato da listagem e navega para o detalhe', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/contracts/list**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACTS_LIST_FIXTURE),
      }),
    );
    await page.route('**/api/v1/contracts/cv-pay-001/detail**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CONTRACT_DETAIL_FIXTURE),
      }),
    );
    await page.route('**/api/v1/contracts/history/api-pay-001**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ apiAssetId: 'api-pay-001', versions: [CONTRACT_DETAIL_FIXTURE] }),
      }),
    );

    await page.goto('/contracts');
    await expect(page.getByText('Payments API')).toBeVisible({ timeout: 5_000 });

    // Click the "Payments API" row (rows are clickable via onClick, not links)
    await page.getByText('Payments API').first().click();

    await expect(page).toHaveURL(/\/contracts\/cv-pay-001/);
    await expect(page.getByText('2.1.0')).toBeVisible({ timeout: 5_000 });
  });
});
