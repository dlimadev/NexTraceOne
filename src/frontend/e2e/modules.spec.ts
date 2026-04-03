import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

// ─── Workflow Page ────────────────────────────────────────────────────────────

test.describe('Workflow Page (autenticado)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('exibe o título Workflow & Approvals', async ({ page }) => {
    await page.route('**/api/v1/workflow/templates', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
    );
    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/workflow');
    await expect(page.getByRole('heading', { name: 'Workflow & Approvals' })).toBeVisible();
  });

  test('exibe templates carregados da API', async ({ page }) => {
    await page.route('**/api/v1/workflow/templates', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'tpl-1',
            name: 'Standard Release',
            changeLevel: 1,
            stages: [{ id: 's1', name: 'Review', order: 1, approvers: [], requiredApprovals: 1 }],
            createdAt: '2024-01-01T00:00:00Z',
          },
        ]),
      })
    );
    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/workflow');
    await expect(page.getByText('Standard Release')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe mensagem "No pending approvals" quando não há instâncias', async ({ page }) => {
    await page.route('**/api/v1/workflow/templates', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
    );
    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/workflow');
    await expect(page.getByText(/no pending approvals/i)).toBeVisible({ timeout: 5_000 });
  });

  test('exibe botões de Approve e Reject para instâncias pendentes', async ({ page }) => {
    await page.route('**/api/v1/workflow/templates', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
    );
    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'inst-1',
              releaseId: 'rel-001',
              templateId: 'tpl-1',
              status: 'InProgress',
              currentStage: 'stg-1',
              createdAt: '2024-01-15T10:00:00Z',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        }),
      })
    );
    await page.goto('/workflow');
    await expect(page.getByRole('button', { name: /approve/i })).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('button', { name: /reject/i })).toBeVisible();
  });

  test('exibe formulário de rejeição ao clicar em Reject', async ({ page }) => {
    await page.route('**/api/v1/workflow/templates', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) })
    );
    await page.route('**/api/v1/workflow/instances**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'inst-1',
              releaseId: 'rel-001',
              templateId: 'tpl-1',
              status: 'InProgress',
              currentStage: 'stg-1',
              createdAt: '2024-01-15T10:00:00Z',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        }),
      })
    );
    await page.goto('/workflow');
    await page.getByRole('button', { name: /reject/i }).click();
    await expect(page.getByPlaceholder(/reason for rejection/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /confirm reject/i })).toBeDisabled();
  });
});

// ─── Audit Page ───────────────────────────────────────────────────────────────

test.describe('Audit Page (autenticado)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('exibe o título Audit Log', async ({ page }) => {
    await page.route('**/api/v1/audit/events**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/audit');
    await expect(page.getByRole('heading', { name: /audit log/i })).toBeVisible();
  });

  test('exibe o botão Verify Integrity', async ({ page }) => {
    await page.route('**/api/v1/audit/events**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/audit');
    await expect(page.getByRole('button', { name: /verify integrity/i })).toBeVisible();
  });

  test('exibe eventos de auditoria carregados da API', async ({ page }) => {
    await page.route('**/api/v1/audit/events**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'evt-1',
              eventType: 'ReleaseCreated',
              aggregateId: 'rel-001',
              aggregateType: 'Release',
              actorId: 'usr-001',
              actorEmail: 'admin@acme.com',
              payload: {},
              hash: 'abc123abc123abc123abc123abc123abc123abc123abc123',
              occurredAt: '2024-01-15T10:00:00Z',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        }),
      })
    );
    await page.goto('/audit');
    await expect(page.getByText('ReleaseCreated')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('admin@acme.com')).toBeVisible();
  });

  test('exibe resultado de verificação de integridade', async ({ page }) => {
    await page.route('**/api/v1/audit/events**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.route('**/api/v1/audit/verify', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ valid: true, message: 'Hash chain is valid. All events verified.' }),
      })
    );
    await page.goto('/audit');
    await page.getByRole('button', { name: /verify integrity/i }).click();
    await expect(page.getByText(/hash chain is valid/i)).toBeVisible({ timeout: 5_000 });
  });

  test('exibe mensagem de erro quando API falha', async ({ page }) => {
    await page.route('**/api/v1/audit/events**', (route) =>
      route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({}) })
    );
    await page.goto('/audit');
    await expect(page.getByText(/failed to load audit events/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Promotion Page ───────────────────────────────────────────────────────────

test.describe('Promotion Page (autenticado)', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
  });

  test('exibe o título Promotion', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/promotion');
    await expect(page.getByRole('heading', { name: 'Promotion', level: 1 })).toBeVisible();
  });

  test('exibe o pipeline de ambientes', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/promotion');
    await expect(page.getByText('development')).toBeVisible();
    await expect(page.getByText('staging')).toBeVisible();
    await expect(page.getByText('production')).toBeVisible();
  });

  test('exibe o botão de nova requisição', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/promotion');
    await expect(page.getByRole('button', { name: /new promotion request/i })).toBeVisible();
  });

  test('exibe formulário ao clicar em New Promotion Request', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/promotion');
    await page.getByRole('button', { name: /new promotion request/i }).click();
    await expect(page.getByPlaceholder(/uuid of the release/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /create request/i })).toBeVisible();
  });

  test('exibe requisições carregadas da API', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'pr-1',
              releaseId: '00000000-0000-0000-0000-000000000001',
              sourceEnvironment: 'staging',
              targetEnvironment: 'production',
              status: 'Approved',
              gateResults: [{ gateName: 'Linting Passed', passed: true }],
              createdAt: '2024-01-15T10:00:00Z',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        }),
      })
    );
    await page.goto('/promotion');
    await expect(page.getByText('Linting Passed')).toBeVisible({ timeout: 5_000 });
  });

  test('exibe mensagem quando não há requisições', async ({ page }) => {
    await page.route('**/api/v1/promotion/requests**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
      })
    );
    await page.goto('/promotion');
    await expect(page.getByText(/no promotion requests yet/i)).toBeVisible({ timeout: 5_000 });
  });
});
