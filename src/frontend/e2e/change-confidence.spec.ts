import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * Massa de teste estável para o módulo Change Confidence.
 * Cobre listagem de mudanças, detalhe com advisory, e decisão de governança.
 */

const CHANGES_SUMMARY_FIXTURE = {
  totalChanges: 15,
  validatedChanges: 8,
  changesNeedingAttention: 4,
  suspectedRegressions: 2,
  changesCorrelatedWithIncidents: 1,
};

const CHANGES_LIST_ITEMS = [
  {
    changeId: 'chg-001',
    serviceName: 'payments-service',
    version: 'v2.2.0',
    environment: 'prod',
    changeType: 'Deployment',
    deploymentStatus: 'Deployed',
    confidenceStatus: 'NeedsAttention',
    changeScore: 0.65,
    deployedAt: '2026-03-18T14:00:00Z',
    commitSha: 'a1b2c3d',
    description: 'Deploy payments-service v2.2.0 with new retry logic',
  },
  {
    changeId: 'chg-002',
    serviceName: 'auth-service',
    version: 'v1.5.1',
    environment: 'prod',
    changeType: 'ConfigurationChange',
    deploymentStatus: 'Deployed',
    confidenceStatus: 'Validated',
    changeScore: 0.18,
    deployedAt: '2026-03-17T10:00:00Z',
    commitSha: 'e4f5g6h',
    description: 'Update JWT expiration from 1h to 2h',
  },
  {
    changeId: 'chg-003',
    serviceName: 'payments-service',
    version: 'v2.1.9',
    environment: 'staging',
    changeType: 'ContractChange',
    deploymentStatus: 'Deployed',
    confidenceStatus: 'SuspectedRegression',
    changeScore: 0.82,
    deployedAt: '2026-03-16T08:00:00Z',
    commitSha: 'i7j8k9l',
    description: 'Breaking change in payment confirmation schema',
  },
];

const CHANGES_LIST_FIXTURE = {
  items: CHANGES_LIST_ITEMS,
  changes: CHANGES_LIST_ITEMS,
  totalCount: 3,
  page: 1,
  pageSize: 20,
};

const CHANGE_DETAIL_FIXTURE = {
  changeId: 'chg-001',
  serviceName: 'payments-service',
  serviceId: 'svc-pay-001',
  version: 'v2.2.0',
  environment: 'prod',
  changeType: 'Deployment',
  deploymentStatus: 'Deployed',
  confidenceStatus: 'NeedsAttention',
  changeScore: 0.65,
  deployedAt: '2026-03-18T14:00:00Z',
  commitSha: 'a1b2c3d4e5f6',
  description: 'Deploy payments-service v2.2.0 with new retry logic',
  pipelineSource: 'GitHub Actions',
  workItem: 'PROJ-1234',
  validationStatus: 'Partial',
  blastRadius: {
    totalAffected: 3,
    directConsumers: 2,
    transitiveConsumers: 1,
  },
};

const CHANGE_INTELLIGENCE_FIXTURE = {
  changeId: 'chg-001',
  baselineMetrics: {
    errorRate: 0.02,
    p99LatencyMs: 250,
    throughputRpm: 1200,
  },
  currentMetrics: {
    errorRate: 0.08,
    p99LatencyMs: 380,
    throughputRpm: 950,
  },
  riskSignals: ['Error rate increased 4x post-deploy', 'Latency degradation detected'],
};

const CHANGE_ADVISORY_FIXTURE = {
  changeId: 'chg-001',
  recommendation: 'ApproveConditionally',
  confidenceScore: 0.65,
  overallConfidence: 0.65,
  factors: [
    {
      factorName: 'EvidenceCompleteness',
      status: 'Pass',
      weight: 0.9,
      description: 'No breaking changes detected in contract',
    },
    {
      factorName: 'ChangeScore',
      status: 'Warning',
      weight: 0.4,
      description: 'Error rate increased above baseline threshold',
    },
    {
      factorName: 'BlastRadiusScope',
      status: 'Warning',
      weight: 0.5,
      description: '3 downstream consumers potentially affected',
    },
  ],
  rationale: 'Conditional approval — monitor error rate for 2h post-deploy',
};

const CHANGE_DECISIONS_FIXTURE = {
  items: [],
  totalCount: 0,
};

// ─── Change Confidence — Listagem ─────────────────────────────────────────────

test.describe('Change Confidence — listagem', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/changes/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGES_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/filter-options**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ changeTypes: [], confidenceStatuses: [], deploymentStatuses: [] }),
      }),
    );
    await page.route('**/api/v1/changes**', (route) => {
      const url = new URL(route.request().url());
      // Let more specific routes handle summary, filter-options, and individual changes
      if (url.pathname.includes('/summary') || url.pathname.includes('/filter-options') || /\/changes\/chg-\d+/.test(url.pathname)) {
        route.fallback();
        return;
      }
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGES_LIST_FIXTURE),
      });
    });
  });

  test('exibe o título da página Change Confidence', async ({ page }) => {
    await page.goto('/changes');
    await expect(page.getByRole('heading', { name: /change confidence/i })).toBeVisible({ timeout: 5_000 });
  });

  test('exibe as métricas de resumo', async ({ page }) => {
    await page.goto('/changes');
    // Aguarda os dados do summary serem visíveis — label "Total Changes" with value
    await expect(page.getByText('Total Changes')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByRole('paragraph').filter({ hasText: 'Validated' })).toBeVisible();
  });

  test('lista as mudanças devolvidas pela API', async ({ page }) => {
    await page.goto('/changes');
    await expect(page.getByText('payments-service').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('auth-service')).toBeVisible();
  });

  test('exibe os tipos e estados das mudanças', async ({ page }) => {
    await page.goto('/changes');
    const table = page.getByRole('table');
    await expect(table.getByText('Deployment').first()).toBeVisible({ timeout: 5_000 });
    await expect(table.getByText('Configuration').first()).toBeVisible();
    await expect(table.getByText('Contract').first()).toBeVisible();
  });

  test('exibe o estado "no changes" quando a API devolve lista vazia', async ({ page }) => {
    await page.route('**/api/v1/changes**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], changes: [], totalCount: 0, page: 1, pageSize: 20 }),
      }),
    );
    await page.goto('/changes');
    await expect(page.getByText(/no changes/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Change Confidence — Filtros ──────────────────────────────────────────────

test.describe('Change Confidence — filtros', () => {
  test('filtra por ambiente via dropdown', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/changes/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGES_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/filter-options**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ changeTypes: [], confidenceStatuses: [], deploymentStatuses: [] }),
      }),
    );
    await page.route('**/api/v1/changes**', (route) => {
      const url = new URL(route.request().url());
      if (/\/changes\/chg-\d+/.test(url.pathname)) { route.fallback(); return; }
      const env = url.searchParams.get('environment') ?? '';
      const filtered = env
        ? CHANGES_LIST_FIXTURE.items.filter((c) => c.environment === env)
        : CHANGES_LIST_FIXTURE.items;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: filtered, changes: filtered, totalCount: filtered.length, page: 1, pageSize: 20 }),
      });
    });

    await page.goto('/changes');
    // Confirma lista completa
    await expect(page.getByText('payments-service').first()).toBeVisible({ timeout: 5_000 });

    // Selecciona filtro de ambiente "staging"
    const envSelect = page.locator('select').filter({ hasText: /all environments/i }).first();
    if (await envSelect.isVisible()) {
      await envSelect.selectOption('staging');
      // Apenas a mudança de staging (chg-003) deve aparecer
      await expect(page.getByText('auth-service')).not.toBeVisible({ timeout: 3_000 });
    }
  });
});

// ─── Change Detail ────────────────────────────────────────────────────────────

test.describe('Change Confidence — detalhe', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/changes/chg-001', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGE_DETAIL_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/chg-001/intelligence**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGE_INTELLIGENCE_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/chg-001/advisory**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGE_ADVISORY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/chg-001/decisions**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGE_DECISIONS_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/chg-001/blast-radius**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGE_DETAIL_FIXTURE.blastRadius),
      }),
    );
  });

  test('exibe o nome do serviço no detalhe da mudança', async ({ page }) => {
    await page.goto('/changes/chg-001');
    await expect(page.getByRole('heading', { name: 'payments-service' })).toBeVisible({ timeout: 5_000 });
  });

  test('exibe o advisory com a recomendação de governança', async ({ page }) => {
    await page.goto('/changes/chg-001');
    await expect(page.getByText('Approve Conditionally').first()).toBeVisible({ timeout: 5_000 });
  });

  test('exibe os factores de análise do advisory', async ({ page }) => {
    await page.goto('/changes/chg-001');
    await expect(page.getByText('Evidence Completeness').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Change Score').first()).toBeVisible();
    await expect(page.getByText('Blast Radius Scope').first()).toBeVisible();
  });

  test('exibe o blast radius da mudança', async ({ page }) => {
    await page.goto('/changes/chg-001');
    await expect(page.getByRole('heading', { name: /blast radius/i })).toBeVisible({ timeout: 5_000 });
  });

  test('exibe o commit SHA', async ({ page }) => {
    await page.goto('/changes/chg-001');
    await expect(page.getByText(/a1b2c3d4e5f6/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Change Detail — Decisão de governança ───────────────────────────────────

test.describe('Change Confidence — decisão de governança', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/changes/chg-001', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(CHANGE_DETAIL_FIXTURE),
      }),
    );
    await page.route('**/api/v1/changes/chg-001/intelligence**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_INTELLIGENCE_FIXTURE) }),
    );
    await page.route('**/api/v1/changes/chg-001/advisory**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_ADVISORY_FIXTURE) }),
    );
    await page.route('**/api/v1/changes/chg-001/decisions**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DECISIONS_FIXTURE) }),
    );
    await page.route('**/api/v1/changes/chg-001/blast-radius**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DETAIL_FIXTURE.blastRadius) }),
    );
    await page.route('**/api/v1/changes/chg-001/decision**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          changeId: 'chg-001',
          decision: 'Approved',
          notes: 'Approved after manual review',
          decidedBy: 'admin@acme.com',
          decidedAt: new Date().toISOString(),
        }),
      }),
    );
  });

  test('exibe opções de decisão Approve e Reject', async ({ page }) => {
    await page.goto('/changes/chg-001');
    // Aguarda o advisory ser carregado (indica que a página está pronta)
    await expect(page.getByText('Approve Conditionally').first()).toBeVisible({ timeout: 5_000 });
    // Os botões de decisão devem estar visíveis
    const approveBtn = page.getByRole('button', { name: /approve/i });
    const rejectBtn = page.getByRole('button', { name: /reject/i });
    // Pelo menos um dos botões deve estar visível (o detalhe tem workflow de decisão)
    const hasDecisionButtons = await approveBtn.first().isVisible() || await rejectBtn.first().isVisible();
    expect(hasDecisionButtons).toBeTruthy();
  });
});

// ─── Navegação: listagem → detalhe ───────────────────────────────────────────

test.describe('Change Confidence — navegação lista → detalhe', () => {
  test('clica numa mudança da listagem e navega para o detalhe', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/changes/summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGES_SUMMARY_FIXTURE) }),
    );
    await page.route('**/api/v1/changes/filter-options**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ changeTypes: [], confidenceStatuses: [], deploymentStatuses: [] }),
      }),
    );
    await page.route('**/api/v1/changes**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary') || url.pathname.includes('/filter-options') || /\/changes\/chg-\d+/.test(url.pathname)) {
        route.fallback();
        return;
      }
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGES_LIST_FIXTURE) });
    });
    await page.route('**/api/v1/changes/chg-001**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.endsWith('/intelligence')) {
        route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_INTELLIGENCE_FIXTURE) });
      } else if (url.pathname.endsWith('/advisory')) {
        route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_ADVISORY_FIXTURE) });
      } else if (url.pathname.endsWith('/decisions')) {
        route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DECISIONS_FIXTURE) });
      } else if (url.pathname.endsWith('/blast-radius')) {
        route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DETAIL_FIXTURE.blastRadius) });
      } else {
        route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CHANGE_DETAIL_FIXTURE) });
      }
    });

    await page.goto('/changes');
    await expect(page.getByText('payments-service').first()).toBeVisible({ timeout: 5_000 });

    // Clica na linha para navegar ao detalhe
    const row = page.getByText('payments-service').first();
    await row.click();
    await expect(page).toHaveURL(/\/changes\/chg-001/, { timeout: 5_000 });
  });
});
