/**
 * Handlers MSW do módulo Change Governance (Confiança em Mudanças).
 *
 * Sem estes handlers, os endpoints /changes, /releases, /workflow, /promotion e
 * /freeze-windows caem no catch-all (`[]`) e as telas ficam vazias/em erro.
 * Este ficheiro dá dados de demo realistas ao núcleo Change Confidence + DORA.
 *
 * NOTA de ordem: rotas literais (summary, filter-options, dora-metrics) TÊM de
 * vir ANTES da paramétrica /changes/:changeId, senão o :changeId captura-as.
 * Registar ANTES do catch-all.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';
const nowIso = () => new Date().toISOString();
const daysAgo = (d: number) => new Date(Date.now() - d * 86400000).toISOString();

// ── Mudanças (lista + detalhe) ────────────────────────────────────────
const changes = [
  {
    id: 'chg-1', changeId: 'chg-1', serviceName: 'Payments API', teamName: 'Payments', domain: 'Billing',
    version: '2.3.0', environment: 'production', changeType: 'Deployment',
    description: 'Introduz reconciliação assíncrona e novo endpoint de reembolsos.',
    commitSha: 'a1b2c3d4e5f6', pipelineSource: 'github-actions', workItemReference: 'PAY-1420',
    changeScore: 0.72, changeLevel: 3, confidenceStatus: 'NeedsAttention',
    deploymentStatus: 'Deployed', validationStatus: 'InProgress', createdAt: daysAgo(1),
  },
  {
    id: 'chg-2', changeId: 'chg-2', serviceName: 'Orders API', teamName: 'Orders', domain: 'Commerce',
    version: '1.8.2', environment: 'production', changeType: 'Deployment',
    description: 'Correção de idempotência na criação de encomendas.',
    commitSha: 'f6e5d4c3b2a1', pipelineSource: 'github-actions', workItemReference: 'ORD-882',
    changeScore: 0.18, changeLevel: 1, confidenceStatus: 'Validated',
    deploymentStatus: 'Deployed', validationStatus: 'Validated', createdAt: daysAgo(2),
  },
  {
    id: 'chg-3', changeId: 'chg-3', serviceName: 'Inventory GraphQL', teamName: 'Inventory', domain: 'Commerce',
    version: '3.1.0', environment: 'staging', changeType: 'Deployment',
    description: 'Migração de schema de stock com nova federação GraphQL.',
    commitSha: '9a8b7c6d5e4f', pipelineSource: 'gitlab-ci', workItemReference: 'INV-311',
    changeScore: 0.88, changeLevel: 4, confidenceStatus: 'SuspectedRegression',
    deploymentStatus: 'Deployed', validationStatus: 'Failed', createdAt: daysAgo(3),
  },
];

const intelligenceByChange: Record<string, unknown> = {
  'chg-1': {
    blastRadius: {
      totalAffected: 6, directConsumers: ['checkout-web', 'billing-worker'],
      transitiveConsumers: ['orders-api', 'notifications-worker', 'ledger-service', 'reporting-api'],
    },
    timeline: [
      { timestamp: daysAgo(1), eventType: 'Deployed', description: 'Release 2.3.0 promovida para produção.' },
      { timestamp: daysAgo(1), eventType: 'CanaryStarted', description: 'Canary a 10% do tráfego.' },
      { timestamp: nowIso(), eventType: 'ValidationPending', description: 'A aguardar sinais de validação.' },
    ],
    validation: { baselineMetrics: 'latency_p95=180ms, error_rate=0.4%', reviewStatus: 'InProgress' },
  },
};

const advisoryByChange: Record<string, unknown> = {
  'chg-1': {
    recommendation: 'ApproveConditionally', confidenceScore: 0.72, overallConfidence: 0.72,
    rationale: 'Blast radius moderado e canary saudável, mas validação ainda em curso.',
    factors: [
      { factorName: 'BlastRadiusScope', status: 'Warning', explanation: '6 consumidores afetados.', description: '6 consumidores afetados (2 diretos).', weight: 0.3 },
      { factorName: 'EvidenceCompleteness', status: 'Pass', explanation: 'Evidências recolhidas.', description: 'Canary saudável e testes recolhidos.', weight: 0.3 },
      { factorName: 'ChangeScore', status: 'Pass', explanation: 'Score dentro do limiar.', description: 'Score de risco abaixo do limiar de bloqueio.', weight: 0.2 },
      { factorName: 'RollbackReadiness', status: 'Warning', explanation: 'Rollback parcialmente pronto.', description: 'Plano de rollback definido; validação em curso.', weight: 0.2 },
    ],
  },
};

const decisionsByChange: Record<string, unknown> = {
  'chg-1': {
    decisions: [
      {
        decisionId: 'dec-1', eventId: 'evt-1', decision: 'ApprovedConditionally',
        rationale: 'Aprovado com condição de monitorizar reembolsos por 24h.',
        conditions: 'Monitorizar taxa de reembolso; rollback se erro > 1%.',
        decidedBy: 'ana.silva@nextraceone.dev', decidedAt: daysAgo(1),
        description: 'Decisão de governança registada.', eventType: 'DecisionRecorded',
        source: 'ChangeAdvisory', occurredAt: daysAgo(1),
      },
    ],
  },
};

const doraMetrics = {
  deploymentFrequency: { deploysPerDay: 3.4, totalDeploys: 102, classification: 'Elite' },
  leadTimeForChanges: { averageHours: 14.2, classification: 'High' },
  changeFailureRate: { failurePercentage: 8.5, failedDeploys: 6, rolledBackDeploys: 3, totalDeploys: 102, classification: 'High' },
  timeToRestoreService: { averageHours: 1.8, classification: 'Elite' },
  overallClassification: 'High', periodDays: 30,
  serviceName: null, teamName: null, environment: null, generatedAt: nowIso(),
};

const findChange = (id: string) => changes.find((c) => c.id === id) ?? changes[0]!;

export const changeGovernanceHandlers = [
  // ── Rotas literais primeiro ─────────────────────────────────────────
  http.get(`${API}/changes/summary`, () =>
    HttpResponse.json({
      totalChanges: 3, changesNeedingAttention: 1, suspectedRegressions: 1,
      validatedChanges: 1, changesCorrelatedWithIncidents: 1,
    }),
  ),
  http.get(`${API}/changes/filter-options`, () =>
    HttpResponse.json({
      changeTypes: ['Deployment', 'Configuration', 'Rollback'],
      confidenceStatuses: ['Validated', 'NeedsAttention', 'SuspectedRegression'],
      deploymentStatuses: ['Deployed', 'Pending', 'RolledBack'],
    }),
  ),
  http.get(`${API}/changes/dora-metrics`, () => HttpResponse.json(doraMetrics)),
  http.get(`${API}/changes/by-service/:serviceName`, () =>
    HttpResponse.json({ items: changes, changes, totalCount: changes.length, page: 1, pageSize: 20 }),
  ),

  // Lista
  http.get(`${API}/changes`, () =>
    HttpResponse.json({ items: changes, changes, totalCount: changes.length, page: 1, pageSize: 20 }),
  ),

  // ── Sub-recursos de uma mudança (antes da paramétrica base) ─────────
  http.get(`${API}/changes/:changeId/blast-radius`, ({ params }) => {
    const intel = intelligenceByChange[String(params.changeId)] as { blastRadius?: unknown } | undefined;
    return HttpResponse.json(intel?.blastRadius ?? { totalAffected: 0, directConsumers: [], transitiveConsumers: [] });
  }),
  http.get(`${API}/changes/:changeId/intelligence`, ({ params }) =>
    HttpResponse.json(intelligenceByChange[String(params.changeId)] ?? {
      blastRadius: { totalAffected: 0, directConsumers: [], transitiveConsumers: [] },
      timeline: [], validation: null,
    }),
  ),
  http.get(`${API}/changes/:changeId/advisory`, ({ params }) =>
    HttpResponse.json(advisoryByChange[String(params.changeId)] ?? {
      recommendation: 'NeedsMoreEvidence', confidenceScore: 0.5, overallConfidence: 0.5,
      rationale: 'Evidência insuficiente para uma recomendação forte.', factors: [],
    }),
  ),
  http.get(`${API}/changes/:changeId/decisions`, ({ params }) =>
    HttpResponse.json(decisionsByChange[String(params.changeId)] ?? { decisions: [] }),
  ),
  http.get(`${API}/changes/:changeId/feature-flags`, ({ params }) =>
    HttpResponse.json({
      releaseId: String(params.changeId), hasData: true, activeFlagCount: 4, criticalFlagCount: 1,
      newFeatureFlagCount: 2, flagProvider: 'internal', riskLevel: 'Medium',
      riskRationale: '1 flag crítica ativa no fluxo de pagamentos.', recordedAt: daysAgo(1),
    }),
  ),
  http.get(`${API}/changes/:changeId/historical-pattern`, ({ params }) =>
    HttpResponse.json({
      releaseId: String(params.changeId), serviceName: 'Payments API', environment: 'production',
      changeLevel: 'Level3', lookbackDays: 90, windowStart: daysAgo(90), windowEnd: nowIso(),
      totalSamples: 24, successRate: 0.83, rollbackRate: 0.08, failureRate: 0.12, averageScore: 61,
      patternRisk: 'Moderate', patternRationale: 'Releases similares tiveram 8% de rollback nos últimos 90 dias.',
      generatedAt: nowIso(),
    }),
  ),
  http.post(`${API}/changes/:changeId/decision`, async () =>
    HttpResponse.json({ decisionId: `dec-${Math.random().toString(36).slice(2, 8)}`, recordedAt: nowIso() }, { status: 201 }),
  ),

  // Detalhe (paramétrica base — por último dentro de /changes)
  http.get(`${API}/changes/:changeId`, ({ params }) => HttpResponse.json(findChange(String(params.changeId)))),

  // ── Trace correlations (usado pelo detalhe da mudança) ──────────────
  http.get(`${API}/releases/:releaseId/traces`, () =>
    HttpResponse.json({
      correlationCount: 2,
      correlations: [
        { traceId: '7f3a9c1e20b4', description: 'POST /payments — pico de latência pós-deploy.', correlatedAt: daysAgo(1) },
        { traceId: 'b28d5e6f10a3', description: 'GET /payments/{id} — erro 500 esporádico.', correlatedAt: daysAgo(1) },
      ],
    }),
  ),
];
