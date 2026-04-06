import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * Testes E2E para o Contract Health Dashboard.
 * Cobre os principais fluxos: visualização do score, métricas, violações e estados de erro.
 */

const HEALTH_DASHBOARD_FIXTURE = {
  totalContractVersions: 30,
  distinctContracts: 15,
  deprecatedVersions: 4,
  filteredCount: 15,
  percentWithExamples: 73,
  percentWithCanonicalEntities: 53,
  topViolations: [
    {
      contractVersionId: 'cv-001',
      semVer: '1.0.0',
      violationCount: 7,
      topRuleIds: ['OperationIdRequired', 'ResponseRequired', 'TitleRequired'],
    },
    {
      contractVersionId: 'cv-002',
      semVer: '2.1.0',
      violationCount: 3,
      topRuleIds: ['VersionRequired'],
    },
  ],
  healthScore: 78,
};

const HEALTH_DASHBOARD_NO_VIOLATIONS = {
  ...HEALTH_DASHBOARD_FIXTURE,
  topViolations: [],
  healthScore: 96,
};

// ─── Health Dashboard — título e estrutura ────────────────────────────────────

test.describe('Contract Health Dashboard — estrutura', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      }),
    );
  });

  test('navega para /contracts/health e exibe o título', async ({ page }) => {
    await page.goto('/contracts/health');
    await expect(page.getByText(/contract health dashboard/i)).toBeVisible({ timeout: 5_000 });
  });

  test('exibe o subtítulo do dashboard', async ({ page }) => {
    await page.goto('/contracts/health');
    await expect(page.getByText(/aggregated quality/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Health Dashboard — health score ─────────────────────────────────────────

test.describe('Contract Health Dashboard — health score', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('exibe o health score quando os dados são carregados', async ({ page }) => {
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      }),
    );
    await page.goto('/contracts/health');
    await expect(page.getByText('78')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe indicador /100 junto ao score', async ({ page }) => {
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      }),
    );
    await page.goto('/contracts/health');
    await expect(page.getByText('/100')).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Health Dashboard — metric cards ─────────────────────────────────────────

test.describe('Contract Health Dashboard — metric cards', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      }),
    );
  });

  test('exibe o total de contratos distintos', async ({ page }) => {
    await page.goto('/contracts/health');
    await expect(page.getByText('15')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe a percentagem de contratos com exemplos', async ({ page }) => {
    await page.goto('/contracts/health');
    await expect(page.getByText('73%')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe a percentagem de contratos com entidades canónicas', async ({ page }) => {
    await page.goto('/contracts/health');
    await expect(page.getByText('53%')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe a contagem de versões deprecated', async ({ page }) => {
    await page.goto('/contracts/health');
    await expect(page.getByText('4')).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Health Dashboard — top violations ───────────────────────────────────────

test.describe('Contract Health Dashboard — violations', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('exibe a lista de top violations quando existem', async ({ page }) => {
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      }),
    );
    await page.goto('/contracts/health');
    await expect(page.getByText('1.0.0')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('2.1.0')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe contagem de violations por contrato', async ({ page }) => {
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      }),
    );
    await page.goto('/contracts/health');
    await expect(page.getByText(/7 violations/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/3 violations/i)).toBeVisible({ timeout: 5_000 });
  });

  test('exibe mensagem de sem violations quando lista está vazia', async ({ page }) => {
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_NO_VIOLATIONS),
      }),
    );
    await page.goto('/contracts/health');
    await expect(page.getByText(/no rule violations/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Health Dashboard — loading state ────────────────────────────────────────

test.describe('Contract Health Dashboard — loading', () => {
  test('exibe estado de loading antes dos dados', async ({ page }) => {
    await mockAuthSession(page);

    let resolveRequest: (value: unknown) => void;
    const pendingRequest = new Promise((resolve) => {
      resolveRequest = resolve;
    });

    await page.route('**/api/v1/catalog/contracts/health-dashboard**', async (route) => {
      await pendingRequest;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(HEALTH_DASHBOARD_FIXTURE),
      });
    });

    await page.goto('/contracts/health');
    await expect(page.getByText(/loading/i)).toBeVisible({ timeout: 5_000 });

    resolveRequest!(null);
  });
});

// ─── Health Dashboard — error state ──────────────────────────────────────────

test.describe('Contract Health Dashboard — erro', () => {
  test('exibe estado de erro quando a API falha', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/contracts/health-dashboard**', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal Server Error' }),
      }),
    );
    await page.goto('/contracts/health');
    await expect(page.getByText(/error/i)).toBeVisible({ timeout: 5_000 });
  });
});
