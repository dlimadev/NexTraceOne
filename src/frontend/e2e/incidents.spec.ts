import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/**
 * Massa de teste estável para o módulo Incidents.
 * Cobre listagem, criação, detalhe com correlação e mitigação.
 */

const INCIDENT_SUMMARY_FIXTURE = {
  totalOpen: 4,
  criticalIncidents: 1,
  withCorrelatedChanges: 2,
  withMitigationAvailable: 3,
  servicesImpacted: 5,
};

const INCIDENTS_LIST_FIXTURE = {
  items: [
    {
      incidentId: 'inc-001',
      reference: 'INC-2026-001',
      title: 'Payment processing degradation',
      incidentType: 'ServiceDegradation',
      severity: 'Critical',
      status: 'Investigating',
      serviceId: 'svc-pay-001',
      serviceDisplayName: 'Payments Service',
      ownerTeam: 'Payments Team',
      environment: 'Production',
      createdAt: '2026-03-18T14:30:00Z',
      hasCorrelatedChanges: true,
      correlationConfidence: 'High',
      mitigationStatus: 'Pending',
    },
    {
      incidentId: 'inc-002',
      reference: 'INC-2026-002',
      title: 'Auth service elevated latency',
      incidentType: 'OperationalRegression',
      severity: 'Major',
      status: 'Monitoring',
      serviceId: 'svc-auth-002',
      serviceDisplayName: 'Auth Service',
      ownerTeam: 'Identity Team',
      environment: 'Production',
      createdAt: '2026-03-17T09:00:00Z',
      hasCorrelatedChanges: false,
      correlationConfidence: 'Low',
      mitigationStatus: 'Mitigated',
    },
    {
      incidentId: 'inc-003',
      reference: 'INC-2026-003',
      title: 'Kafka consumer lag spike',
      incidentType: 'MessagingIssue',
      severity: 'Minor',
      status: 'Open',
      serviceId: 'svc-events-003',
      serviceDisplayName: 'Events Service',
      ownerTeam: 'Platform Team',
      environment: 'Staging',
      createdAt: '2026-03-16T11:00:00Z',
      hasCorrelatedChanges: true,
      correlationConfidence: 'Medium',
      mitigationStatus: 'NotStarted',
    },
  ],
  totalCount: 3,
  page: 1,
  pageSize: 20,
};

const INCIDENT_DETAIL_FIXTURE = {
  identity: {
    incidentId: 'inc-001',
    reference: 'INC-2026-001',
    title: 'Payment processing degradation',
    summary: 'Payment gateway error rate rose to 8% — 4x above baseline',
    incidentType: 'ServiceDegradation',
    severity: 'Critical',
    status: 'Investigating',
    createdAt: '2026-03-18T14:30:00Z',
    updatedAt: '2026-03-18T15:00:00Z',
  },
  linkedServices: [
    {
      serviceId: 'svc-pay-001',
      displayName: 'Payments Service',
      serviceType: 'RestApi',
      criticality: 'Critical',
    },
  ],
  ownerTeam: 'Payments Team',
  impactedDomain: 'Finance',
  impactedEnvironment: 'Production',
  timeline: [
    { timestamp: '2026-03-18T14:30:00Z', description: 'Incident opened — error rate spiked to 8%' },
    { timestamp: '2026-03-18T14:35:00Z', description: 'Team alerted via PagerDuty' },
  ],
  correlation: {
    confidence: 'High',
    reason: 'Deployment of payments-service v2.2.0 at 14:00 correlates with error rate spike',
    relatedChanges: [
      {
        changeId: 'chg-001',
        description: 'Deploy payments-service v2.2.0',
        changeType: 'Deployment',
        confidenceStatus: 'CorrelatedWithIncident',
        deployedAt: '2026-03-18T14:00:00Z',
      },
    ],
    relatedServices: [
      {
        serviceId: 'svc-pay-001',
        displayName: 'Payments Service',
        impactDescription: 'Primary affected service',
      },
    ],
  },
  evidence: {
    operationalSignalsSummary: 'Error rate 8% (baseline: 2%), P99 latency 380ms (baseline: 250ms)',
    degradationSummary: 'Significant performance regression post-deploy',
    observations: [
      { title: 'Error Rate', description: 'Error rate increased from 2% to 8% at 14:00' },
      { title: 'Latency', description: 'P99 latency increased from 250ms to 380ms' },
    ],
  },
  relatedContracts: [
    {
      contractVersionId: 'cv-pay-001',
      name: 'Payments API',
      version: '2.2.0',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
    },
  ],
  runbooks: [
    { title: 'Payment Gateway Recovery', url: 'https://docs.acme.com/runbooks/payments' },
  ],
  mitigation: {
    status: 'Pending',
    actions: [
      { description: 'Rollback to v2.1.9', status: 'Pending', completed: false },
      { description: 'Notify downstream consumers', status: 'Pending', completed: false },
    ],
    rollbackGuidance: 'Rollback to v2.1.9 using the deployment pipeline',
    rollbackRelevant: true,
    escalationGuidance: 'Escalate to platform team if rollback does not resolve within 15 minutes',
  },
};

// ─── Incidents — Listagem ─────────────────────────────────────────────────────

test.describe('Incidents — listagem', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/incidents/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENT_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/incidents**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary') || /\/incidents\/inc-\d+/.test(url.pathname)) {
        route.fallback();
        return;
      }
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENTS_LIST_FIXTURE),
      });
    });
  });

  test('exibe o título da página Incidents', async ({ page }) => {
    await page.goto('/operations/incidents');
    await expect(page.getByRole('heading', { name: /incidents/i, level: 1 })).toBeVisible({ timeout: 5_000 });
  });

  test('exibe os stat cards com métricas de resumo', async ({ page }) => {
    await page.goto('/operations/incidents');
    await expect(page.getByText('Open Incidents')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Critical').first()).toBeVisible();
    await expect(page.getByText('With Correlated Changes')).toBeVisible();
  });

  test('lista os incidentes devolvidos pela API', async ({ page }) => {
    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment processing degradation')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Auth service elevated latency')).toBeVisible();
    await expect(page.getByText('Kafka consumer lag spike')).toBeVisible();
  });

  test('exibe os badges de severidade e status', async ({ page }) => {
    await page.goto('/operations/incidents');
    await expect(page.getByText('Investigating').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Monitoring')).toBeVisible();
    await expect(page.getByText('Open').first()).toBeVisible();
  });

  test('exibe o indicador de correlação nos incidentes com changes correlacionados', async ({ page }) => {
    await page.goto('/operations/incidents');
    // Incidentes com hasCorrelatedChanges=true exibem um indicador visual
    await expect(page.getByText(/correlated/i)).toBeVisible({ timeout: 5_000 });
  });

  test('exibe estado vazio quando não há incidentes', async ({ page }) => {
    await page.route('**/api/v1/incidents**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      }),
    );
    await page.goto('/operations/incidents');
    await expect(page.getByText(/no incidents/i)).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Incidents — Criar incidente ──────────────────────────────────────────────

test.describe('Incidents — criar incidente', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/incidents/summary**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENT_SUMMARY_FIXTURE),
      }),
    );
    await page.route('**/api/v1/incidents**', (route) => {
      const url = new URL(route.request().url());
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ incidentId: 'inc-new-001', reference: 'INC-2026-004' }),
        });
        return;
      }
      if (url.pathname.includes('/summary') || /\/incidents\/inc-\d+/.test(url.pathname)) { route.fallback(); return; }
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENTS_LIST_FIXTURE),
      });
    });
  });

  test('exibe o botão de criar incidente', async ({ page }) => {
    await page.goto('/operations/incidents');
    await expect(page.getByRole('button', { name: /create incident/i })).toBeVisible({ timeout: 5_000 });
  });

  test('abre o formulário ao clicar em "Create Incident"', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();
    await expect(page.getByPlaceholder(/incident title/i)).toBeVisible({ timeout: 3_000 });
  });

  test('preenche e submete o formulário de criação', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    await page.getByPlaceholder(/incident title/i).fill('Database connection pool exhausted');
    await page.getByPlaceholder(/service id/i).fill('svc-db-001');
    await page.getByPlaceholder(/service display name/i).fill('Database Service');
    await page.getByPlaceholder(/owner team/i).fill('Platform Team');

    // Submete o formulário
    await page.getByRole('button', { name: /^create$/i }).click();

    // Após criação com sucesso, o formulário deve fechar (ou aparecer a referência)
    await expect(page.getByPlaceholder(/incident title/i)).not.toBeVisible({ timeout: 5_000 });
  });

  test('desabilita o botão de submissão quando o título está vazio', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();

    // Com título vazio, o botão Create deve estar desabilitado
    const createBtn = page.getByRole('button', { name: /^create$/i });
    await expect(createBtn).toBeDisabled({ timeout: 3_000 });
  });

  test('cancela a criação ao clicar em Cancel', async ({ page }) => {
    await page.goto('/operations/incidents');
    await page.getByRole('button', { name: /create incident/i }).click();
    await expect(page.getByPlaceholder(/incident title/i)).toBeVisible({ timeout: 3_000 });

    await page.getByRole('button', { name: /cancel/i }).click();
    await expect(page.getByPlaceholder(/incident title/i)).not.toBeVisible({ timeout: 3_000 });
  });
});

// ─── Incidents — Detalhe ──────────────────────────────────────────────────────

test.describe('Incidents — detalhe', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/incidents/inc-001', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENT_DETAIL_FIXTURE),
      }),
    );
  });

  test('exibe o título e referência do incidente', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText('Payment processing degradation')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('INC-2026-001')).toBeVisible();
  });

  test('exibe a equipa responsável e o domínio', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText('Payments Team')).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Finance')).toBeVisible();
  });

  test('exibe a severidade e o status do incidente', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText('Critical').first()).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Investigating')).toBeVisible();
  });

  test('exibe a correlação com mudanças', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    // Secção de correlação deve ser visível
    await expect(page.getByText(/correlation/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Deploy payments-service v2.2.0')).toBeVisible();
  });

  test('exibe o resumo de evidências operacionais', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText(/evidence/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/error rate/i)).toBeVisible({ timeout: 5_000 });
  });

  test('exibe a secção de mitigação com acções sugeridas', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText(/mitigation/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText('Rollback to v2.1.9')).toBeVisible();
  });

  test('exibe a timeline do incidente', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText(/timeline/i)).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/incident opened/i)).toBeVisible();
  });

  test('exibe os runbooks relacionados', async ({ page }) => {
    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByText('Payment Gateway Recovery')).toBeVisible({ timeout: 5_000 });
  });
});

// ─── Incidents — Correlação: Refresh ─────────────────────────────────────────

test.describe('Incidents — correlação (refresh)', () => {
  test('exibe o botão de refresh de correlação', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/incidents/inc-001', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENT_DETAIL_FIXTURE),
      }),
    );
    await page.route('**/api/v1/incidents/inc-001/correlation/refresh**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          ...INCIDENT_DETAIL_FIXTURE.correlation,
          score: 82,
        }),
      }),
    );

    await page.goto('/operations/incidents/inc-001');
    await expect(page.getByRole('button', { name: /refresh correlation/i })).toBeVisible({ timeout: 5_000 });
  });

  test('executa refresh de correlação ao clicar no botão', async ({ page }) => {
    await mockAuthSession(page);

    let refreshCalled = false;

    await page.route('**/api/v1/incidents/inc-001', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(INCIDENT_DETAIL_FIXTURE),
      }),
    );
    await page.route('**/api/v1/incidents/inc-001/correlation/refresh**', (route) => {
      refreshCalled = true;
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...INCIDENT_DETAIL_FIXTURE.correlation, score: 87 }),
      });
    });

    await page.goto('/operations/incidents/inc-001');
    const refreshBtn = page.getByRole('button', { name: /refresh correlation/i });
    await expect(refreshBtn).toBeVisible({ timeout: 5_000 });
    await refreshBtn.click();

    // Aguarda a chamada de refresh ser feita
    await page.waitForTimeout(1_000);
    expect(refreshCalled).toBeTruthy();
  });
});

// ─── Navegação: listagem → detalhe ───────────────────────────────────────────

test.describe('Incidents — navegação lista → detalhe', () => {
  test('clica num incidente da listagem e navega para o detalhe', async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/incidents/summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENT_SUMMARY_FIXTURE) }),
    );
    await page.route('**/api/v1/incidents**', (route) => {
      const url = new URL(route.request().url());
      if (url.pathname.includes('/summary') || /\/incidents\/inc-\d+/.test(url.pathname)) { route.fallback(); return; }
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENTS_LIST_FIXTURE) });
    });
    await page.route('**/api/v1/incidents/inc-001**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(INCIDENT_DETAIL_FIXTURE) }),
    );

    await page.goto('/operations/incidents');
    await expect(page.getByText('Payment processing degradation')).toBeVisible({ timeout: 5_000 });

    // Clica no link do incidente
    const incidentLink = page.getByRole('link', { name: /payment processing degradation/i }).first();
    await expect(incidentLink).toBeVisible({ timeout: 5_000 });
    await incidentLink.click();

    await expect(page).toHaveURL(/\/operations\/incidents\/inc-001/, { timeout: 5_000 });
    await expect(page.getByText('INC-2026-001')).toBeVisible({ timeout: 5_000 });
  });
});
