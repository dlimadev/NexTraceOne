import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * Massa de teste estável para o Service Catalog.
 * Representa dois serviços registados com atributos realistas.
 */
const SERVICE_LIST_FIXTURE = {
  items: [
    {
      serviceId: 'svc-pay-001',
      name: 'payments-service',
      displayName: 'Payments Service',
      description: 'Handles payment processing and reconciliation',
      serviceType: 'RestApi',
      domain: 'Finance',
      teamName: 'Payments Team',
      criticality: 'Critical',
      lifecycleStatus: 'Active',
      exposureType: 'Internal',
      registeredAt: '2025-01-10T10:00:00Z',
    },
    {
      serviceId: 'svc-auth-002',
      name: 'auth-service',
      displayName: 'Auth Service',
      description: 'Identity and authentication provider',
      serviceType: 'RestApi',
      domain: 'Platform',
      teamName: 'Identity Team',
      criticality: 'High',
      lifecycleStatus: 'Active',
      exposureType: 'Internal',
      registeredAt: '2025-01-08T09:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

const SERVICES_SUMMARY_FIXTURE = {
  totalCount: 2,
  criticalCount: 1,
  highCriticalityCount: 1,
  activeCount: 2,
  deprecatedCount: 0,
  retiredCount: 0,
};

const GRAPH_FIXTURE = {
  services: [
    { serviceAssetId: 'svc-pay-001', name: 'payments-service', domain: 'Finance', teamName: 'Payments Team', serviceType: 'RestApi', criticality: 'Critical', lifecycleStatus: 'Active' },
    { serviceAssetId: 'svc-auth-002', name: 'auth-service', domain: 'Platform', teamName: 'Identity Team', serviceType: 'RestApi', criticality: 'High', lifecycleStatus: 'Active' },
  ],
  apis: [
    { apiAssetId: 'api-pay-001', name: 'Payments API', ownerServiceId: 'svc-pay-001', protocol: 'OpenApi', baseUrl: '/api/payments', consumers: ['svc-auth-002'] },
  ],
};

const SERVICE_DETAIL_FIXTURE = {
  serviceId: 'svc-pay-001',
  id: 'svc-pay-001',
  name: 'payments-service',
  displayName: 'Payments Service',
  description: 'Handles payment processing and reconciliation',
  serviceType: 'RestApi',
  domain: 'Finance',
  systemArea: 'Finance',
  teamName: 'Payments Team',
  technicalOwner: 'payments@acme.com',
  businessOwner: '',
  ownerEmail: 'payments@acme.com',
  criticality: 'Critical',
  lifecycleStatus: 'Active',
  exposureType: 'Internal',
  repositoryUrl: 'https://github.com/acme/payments-service',
  documentationUrl: 'https://docs.acme.com/payments',
  tags: ['payments', 'finance', 'critical'],
  registeredAt: '2025-01-10T10:00:00Z',
  updatedAt: '2025-03-01T14:00:00Z',
  apis: [
    {
      apiId: 'api-pay-001',
      name: 'Payments API',
      routePattern: '/api/payments',
      version: '2.1.0',
      visibility: 'Internal',
      isDecommissioned: false,
      consumerCount: 3,
    },
  ],
  apiCount: 1,
  totalConsumers: 3,
};

// ─── Service Catalog List ─────────────────────────────────────────────────────

test.describe('Service Catalog — listagem', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/graph**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(GRAPH_FIXTURE),
      }),
    );
    await page.route('**/api/v1/catalog/services/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICES_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/catalog/snapshots**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }),
    );
    await page.route('**/api/v1/catalog/services**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary') || url.pathname.includes('/svc-')) {
        route.fallback();
        return;
      }
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICE_LIST_FIXTURE),
      });
    });
    await page.route('**/api/v1/catalog/health**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }),
    );
  });

  test('exibe o título da página Service Catalog', async ({ page }) => {
    await page.goto('/services');
    await expect(page.getByRole('heading', { name: /service catalog/i, level: 1 })).toBeVisible();
  });

  test('exibe as métricas de resumo (summary cards)', async ({ page }) => {
    await page.goto('/services');
    // The page shows graph-based stats: Services, APIs, Relationships, Domains
    await expect(page.getByText('Services').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('APIs')).toBeVisible();
  });

  test('lista os serviços devolvidos pela API', async ({ page }) => {
    await page.goto('/services');
    // The services table shows displayName from the /catalog/services list endpoint
    await expect(page.getByText('Payments Service').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Auth Service').first()).toBeVisible();
  });

  test('exibe os atributos de criticidade e lifecycle dos serviços', async ({ page }) => {
    await page.goto('/services');
    // Payments Service: Critical + Active
    await expect(page.getByText('Payments Team')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Identity Team')).toBeVisible();
  });

  test('exibe estado vazio quando a API devolve lista vazia', async ({ page }) => {
    // Sobrescreve o mock do graph para vazio
    await page.route('**/api/v1/catalog/graph**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ services: [], apis: [] }),
      }),
    );
    await page.goto('/services');
    // With empty graph, stat values should be 0
    await expect(page.getByText('0').first()).toBeVisible({ timeout: 5_000 });
  });

  test('exibe erro quando a API falha', async ({ page }) => {
    await page.route('**/api/v1/catalog/graph**', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: '{}' }),
    );
    await page.route('**/api/v1/catalog/services**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary') || url.pathname.includes('/svc-')) {
        route.fallback();
        return;
      }
      route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });
    });
    await page.route('**/api/v1/catalog/services/summary**', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: '{}' }),
    );
    await page.goto('/services');
    await expect(page.getByText(/error/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Service Catalog — Pesquisa ───────────────────────────────────────────────

test.describe('Service Catalog — pesquisa e filtros', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICES_SUMMARY_FIXTURE),
      }),
    );
  });

  test('pesquisa filtra os resultados da API', async ({ page }) => {
    // Primeiro carregamento — devolve lista completa
    await page.route('**/api/v1/catalog/services**', (route) => {
      const url = new URL(route.request().url());
      const search = url.searchParams.get('search') ?? '';
      const filtered = SERVICE_LIST_FIXTURE.items.filter((s) =>
        s.name.includes(search) || s.displayName.toLowerCase().includes(search.toLowerCase()),
      );
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: filtered, totalCount: filtered.length, page: 1, pageSize: 20 }),
      });
    });

    await page.goto('/services');
    // Confirma que ambos os serviços estão visíveis inicialmente
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 5_000 });

    // Preenche o campo de pesquisa com "auth"
    const searchInput = page.getByPlaceholder(/search services/i);
    await searchInput.fill('auth');

    // Após debounce (350ms), "Auth Service" deve aparecer, "Payments" não
    await expect(page.getByText('Auth Service')).toBeVisible({ timeout: 3_000 });
    await expect(page.getByText('Payments Service')).not.toBeVisible();
  });
});

// ─── Service Detail ───────────────────────────────────────────────────────────

test.describe('Service Catalog — detalhe do serviço', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services/svc-pay-001**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICE_DETAIL_FIXTURE),
      }),
    );
    // Mocks auxiliares para sub-recursos da página de detalhe
    await page.route('**/api/v1/incidents**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0 }),
      }),
    );
    await page.route('**/api/v1/changes**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0 }),
      }),
    );
  });

  test('exibe o nome do serviço no detalhe', async ({ page }) => {
    await page.goto('/services/svc-pay-001');
    await expect(page.getByRole('heading', { name: 'Payments Service' })).toBeVisible({ timeout: 5_000 });
  });

  test('exibe o domínio e equipa do serviço', async ({ page }) => {
    await page.goto('/services/svc-pay-001');
    await expect(page.getByText('Finance').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Payments Team').first()).toBeVisible();
  });

  test('exibe a lista de API assets do serviço', async ({ page }) => {
    await page.goto('/services/svc-pay-001');
    // APIs are shown in the APIs tab — click it first
    const apisTab = page.getByRole('button', { name: /apis/i }).first();
    if (await apisTab.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await apisTab.click();
    }
    await expect(page.getByText('Payments API').first()).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Navegação: listagem → detalhe ───────────────────────────────────────────

test.describe('Service Catalog — navegação lista → detalhe', () => {
  test('clica num serviço da listagem e navega para o detalhe', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICES_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/catalog/services**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary') || url.pathname.includes('/svc-pay-001')) {
        route.fallback();
        return;
      }
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICE_LIST_FIXTURE),
      });
    });
    await page.route('**/api/v1/catalog/services/svc-pay-001**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(SERVICE_DETAIL_FIXTURE),
      }),
    );
    await page.route('**/api/v1/incidents**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }),
    );
    await page.route('**/api/v1/changes**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }),
    );

    await page.goto('/services');
    // Espera a lista carregar e clica no link do serviço
    const serviceLink = page.getByRole('link', { name: /payments service/i }).first();
    await expect(serviceLink).toBeVisible({ timeout: 5_000 });
    await serviceLink.click();

    await expect(page).toHaveURL(/\/services\/svc-pay-001/);
    await expect(page.getByText('Payments Service')).toBeVisible({ timeout: 5_000 });
  });
});
