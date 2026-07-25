/**
 * Handlers MSW do módulo Operations (Inteligência Operacional).
 *
 * O módulo é grande e quase nada estava stubado — as telas caíam no catch-all
 * (`[]`) e ficavam vazias. Este ficheiro dá dados de demo aos fluxos flagship,
 * começando pelos Incidentes (lista + resumo) e Runbooks.
 *
 * NOTA de ordem: rotas literais (summary, timeline) TÊM de vir ANTES da
 * paramétrica /incidents/:id. Registar ANTES do catch-all.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';
const daysAgo = (d: number) => new Date(Date.now() - d * 86400000).toISOString();
const hoursAgo = (h: number) => new Date(Date.now() - h * 3600000).toISOString();

// ── Incidentes (lista) ────────────────────────────────────────────────
const incidents = [
  {
    incidentId: 'inc-1', reference: 'INC-2041', title: 'Latência elevada no processamento de pagamentos',
    incidentType: 'ServiceDegradation', severity: 'Critical', status: 'Investigating', serviceId: 'svc-payments-api',
    serviceDisplayName: 'Payments API', ownerTeam: 'Payments', environment: 'production', createdAt: hoursAgo(3),
    hasCorrelatedChanges: true, correlationConfidence: 'High', mitigationStatus: 'Available',
  },
  {
    incidentId: 'inc-2', reference: 'INC-2040', title: 'Erros 500 esporádicos no checkout',
    incidentType: 'AvailabilityIssue', severity: 'Major', status: 'Mitigating', serviceId: 'svc-orders-api',
    serviceDisplayName: 'Orders API', ownerTeam: 'Orders', environment: 'production', createdAt: hoursAgo(8),
    hasCorrelatedChanges: true, correlationConfidence: 'Medium', mitigationStatus: 'InProgress',
  },
  {
    incidentId: 'inc-3', reference: 'INC-2039', title: 'Fila de eventos de inventário acumulada',
    incidentType: 'MessagingIssue', severity: 'Minor', status: 'Monitoring', serviceId: 'svc-inventory-graphql',
    serviceDisplayName: 'Inventory GraphQL', ownerTeam: 'Inventory', environment: 'staging', createdAt: daysAgo(1),
    hasCorrelatedChanges: false, correlationConfidence: 'Low', mitigationStatus: 'NotAvailable',
  },
  {
    incidentId: 'inc-4', reference: 'INC-2035', title: 'Timeout no gateway de notificações',
    incidentType: 'DependencyFailure', severity: 'Warning', status: 'Resolved', serviceId: 'svc-notifications-worker',
    serviceDisplayName: 'Notifications Worker', ownerTeam: 'Platform', environment: 'production', createdAt: daysAgo(3),
    hasCorrelatedChanges: false, correlationConfidence: 'Low', mitigationStatus: 'Completed',
  },
];

/** Gera uma série temporal de N pontos horários com um valor base + ruído determinístico. */
const series = (base: number, n = 24) =>
  Array.from({ length: n }, (_, i) => ({
    timestamp: hoursAgo(n - i),
    value: Math.round(base * (0.8 + 0.4 * Math.abs(Math.sin(i / 3)))),
  }));

const sreTimeSeries = () => ({
  requests: series(52000),
  requestLatency: series(180),
  requestErrors: series(9),
  queries: series(35000),
  queryLatency: series(13),
  queryErrors: series(2),
});

export const operationsHandlers = [
  // ── Rotas literais primeiro ─────────────────────────────────────────
  http.get(`${API}/incidents/summary`, () =>
    HttpResponse.json({
      totalOpen: 3, criticalIncidents: 1, withCorrelatedChanges: 2, withMitigationAvailable: 1,
      servicesImpacted: 4,
      severityBreakdown: { critical: 1, major: 1, minor: 1, warning: 1 },
      statusBreakdown: { open: 0, investigating: 1, mitigating: 1, monitoring: 1, resolved: 1, closed: 0 },
    }),
  ),
  http.get(`${API}/incidents/timeline`, () =>
    HttpResponse.json({
      entries: incidents.map((i) => ({
        id: i.incidentId, entryType: 'Incident', title: i.title, reference: i.reference,
        severity: i.severity, status: i.status, serviceDisplayName: i.serviceDisplayName,
        occurredAt: i.createdAt,
      })),
      totalCount: incidents.length,
    }),
  ),

  // Lista de incidentes
  http.get(`${API}/incidents`, () =>
    HttpResponse.json({ items: incidents, totalCount: incidents.length, page: 1, pageSize: 20 }),
  ),

  // ── Incidente: correlação / evidência / mitigação (sub-rotas) ───────
  http.get(`${API}/incidents/:incidentId/correlation`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId), confidence: 'High', score: 0.86,
      reason: 'Deploy chg-9001 há 4h coincide com o início da degradação de latência.',
      relatedChanges: [{ changeId: 'chg-9001', description: 'Deploy v2.14.0 — novo motor de reconciliação', changeType: 'Deployment', confidenceStatus: 'Watch', deployedAt: hoursAgo(4) }],
      relatedServices: [{ serviceId: 'svc-ledger-db', displayName: 'Ledger DB', impactDescription: 'Latência de escrita aumentou 3x.' }],
      relatedDependencies: [{ serviceId: 'svc-ledger-db', displayName: 'Ledger DB', relationship: 'Downstream' }],
      possibleImpactedContracts: [{ contractVersionId: 'cv-1', name: 'Payments REST', version: '2', protocol: 'REST' }],
    }),
  ),
  http.post(`${API}/incidents/:incidentId/correlation/refresh`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId), confidence: 'High', score: 0.88,
      reason: 'Correlação recalculada: deploy chg-9001 confirmado como causa provável.',
      relatedChanges: [{ changeId: 'chg-9001', description: 'Deploy v2.14.0 — novo motor de reconciliação', changeType: 'Deployment', confidenceStatus: 'Watch', deployedAt: hoursAgo(4) }],
      relatedServices: [{ serviceId: 'svc-ledger-db', displayName: 'Ledger DB', impactDescription: 'Latência de escrita aumentou 3x.' }],
      relatedDependencies: [{ serviceId: 'svc-ledger-db', displayName: 'Ledger DB', relationship: 'Downstream' }],
      possibleImpactedContracts: [{ contractVersionId: 'cv-1', name: 'Payments REST', version: '2', protocol: 'REST' }],
    }),
  ),
  http.get(`${API}/incidents/:incidentId/correlated-changes`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId), totalCorrelations: 1,
      correlations: [{ changeId: 'chg-9001', serviceId: 'svc-payments-api', serviceName: 'Payments API', description: 'Deploy v2.14.0 — novo motor de reconciliação', environment: 'production', occurredAt: hoursAgo(4), confidenceLevel: 'High', matchType: 'ServiceAndTimeWindow', timeWindowHours: 6, correlatedAt: hoursAgo(2) }],
    }),
  ),
  http.post(`${API}/incidents/:incidentId/correlate`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId), timeWindowHours: 6, totalCandidates: 3, newCorrelations: 1,
      correlations: [{ changeId: 'chg-9001', serviceName: 'Payments API', description: 'Deploy v2.14.0', environment: 'production', occurredAt: hoursAgo(4), confidenceLevel: 'High', matchType: 'ServiceAndTimeWindow', isDuplicate: false }],
    }),
  ),
  http.get(`${API}/incidents/:incidentId/evidence`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId),
      operationalSignalsSummary: 'Latência P99 412ms (baseline 180ms); taxa de erro 500 a 0.34%.',
      degradationSummary: 'Degradação iniciada há 3 horas, logo após o deploy v2.14.0.',
      observations: [
        { title: 'Pico de latência', description: 'P99 subiu de 180ms para 412ms em 12 minutos.' },
        { title: 'Erros 500', description: 'Taxa de erro 500 subiu para 0.34% após o deploy.' },
      ],
      anomalySummary: 'Anomalia de latência correlacionada com o deploy chg-9001.',
    }),
  ),
  http.get(`${API}/incidents/:incidentId/mitigation/recommendations`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId),
      recommendations: [
        { recommendationId: 'rec-1', title: 'Reverter deploy v2.14.0', summary: 'Rollback do deploy correlacionado restaura a latência ao baseline.', recommendedActionType: 'Rollback', rationaleSummary: 'Deploy chg-9001 é a causa provável (confiança alta).', evidenceSummary: 'Latência subiu imediatamente após o deploy.', requiresApproval: true, riskLevel: 'Medium', linkedRunbookIds: ['rb-3'], suggestedValidationSteps: ['Confirmar P99 < 200ms', 'Confirmar erro 500 < 0.1%'] },
      ],
    }),
  ),
  http.get(`${API}/incidents/:incidentId/mitigation/history`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId),
      entries: [
        { entryId: 'aud-1', action: 'MitigationStarted', performedBy: 'ana.silva@nextraceone.dev', performedAt: hoursAgo(1), notes: 'Rollback do connection pool iniciado.', linkedEvidence: [] },
        { entryId: 'aud-2', action: 'IncidentAcknowledged', performedBy: 'ana.silva@nextraceone.dev', performedAt: hoursAgo(2), linkedEvidence: [] },
      ],
    }),
  ),
  http.get(`${API}/incidents/:incidentId/mitigation`, ({ params }) =>
    HttpResponse.json({
      incidentId: String(params.incidentId), mitigationStatus: 'InProgress',
      suggestedActions: [
        { description: 'Reverter connection pool para configuração anterior', status: 'InProgress', completed: false },
        { description: 'Escalar réplicas de leitura do Ledger DB', status: 'Pending', completed: false },
        { description: 'Notificar equipa de pagamentos', status: 'Done', completed: true },
      ],
      recommendedRunbooks: [{ title: 'Rollback de deploy com erros 500', url: '/operations/runbooks/rb-3/edit', description: 'Procedimento de rollback seguro.' }],
      rollbackGuidance: 'Reverter o deploy chg-9001 via pipeline de release.',
      rollbackRelevant: true,
      escalationGuidance: 'Escalar para o on-call de plataforma se a latência persistir acima de 400ms por 15min.',
    }),
  ),
  http.post(`${API}/incidents/:incidentId/resolve`, ({ params }) =>
    HttpResponse.json({ incidentId: String(params.incidentId), status: 'Resolved', resolvedAt: new Date().toISOString(), resolutionNote: 'Resolvido via rollback.' }),
  ),

  // ── Incidente: detalhe ──────────────────────────────────────────────
  http.get(`${API}/incidents/:incidentId`, ({ params }) => {
    const inc = incidents.find((i) => i.incidentId === String(params.incidentId)) ?? incidents[0];
    return HttpResponse.json({
      identity: {
        incidentId: inc.incidentId, reference: inc.reference, title: inc.title,
        summary: 'Latência de escrita elevada na base de dados de pagamentos após o deploy v2.14.0; taxa de erro 500 a subir.',
        incidentType: inc.incidentType, severity: inc.severity, status: inc.status,
        createdAt: inc.createdAt, updatedAt: hoursAgo(1), resolvedAt: null,
        acknowledgedAt: hoursAgo(2), acknowledgedBy: 'ana.silva@nextraceone.dev',
      },
      linkedServices: [{ serviceId: inc.serviceId, displayName: inc.serviceDisplayName, serviceType: 'RestApi', criticality: 'Critical' }],
      ownerTeam: inc.ownerTeam, impactedDomain: 'Billing', impactedEnvironment: inc.environment,
      timeline: [
        { timestamp: hoursAgo(3), description: 'Incidente aberto automaticamente por alerta de latência.' },
        { timestamp: hoursAgo(2.5), description: 'Reconhecido por ana.silva; equipa de pagamentos notificada.' },
        { timestamp: hoursAgo(2), description: 'Correlação identificou o deploy chg-9001 como causa provável.' },
        { timestamp: hoursAgo(1), description: 'Mitigação em curso: rollback do connection pool.' },
      ],
      correlation: {
        confidence: 'High', reason: 'Deploy chg-9001 há 4h coincide com o início da degradação.',
        relatedChanges: [{ changeId: 'chg-9001', description: 'Deploy v2.14.0 — novo motor de reconciliação', changeType: 'Deployment', confidenceStatus: 'Watch', deployedAt: hoursAgo(4) }],
        relatedServices: [{ serviceId: 'svc-ledger-db', displayName: 'Ledger DB', impactDescription: 'Latência de escrita aumentou 3x.' }],
      },
      evidence: {
        operationalSignalsSummary: 'Latência P99 412ms (baseline 180ms); erro 500 a 0.34%.',
        degradationSummary: 'Degradação iniciada há 3 horas, após o deploy v2.14.0.',
        observations: [
          { title: 'Pico de latência', description: 'P99 subiu de 180ms para 412ms em 12 minutos.' },
          { title: 'Erros 500', description: 'Taxa de erro 500 subiu para 0.34% após o deploy.' },
        ],
      },
      relatedContracts: [{ contractVersionId: 'cv-1', name: 'Payments REST', version: '2', protocol: 'REST', lifecycleState: 'Published' }],
      runbooks: [{ title: 'Mitigar latência de pagamentos', url: '/operations/runbooks/rb-1/edit' }],
      mitigation: {
        status: 'InProgress',
        actions: [
          { description: 'Reverter connection pool para configuração anterior', status: 'InProgress', completed: false },
          { description: 'Escalar réplicas de leitura do Ledger DB', status: 'Pending', completed: false },
          { description: 'Notificar equipa de pagamentos', status: 'Done', completed: true },
        ],
        rollbackGuidance: 'Reverter o deploy chg-9001 via pipeline de release.',
        rollbackRelevant: true,
        escalationGuidance: 'Escalar para o on-call de plataforma se a latência persistir.',
      },
    });
  }),

  // ── Runbooks (lista) ────────────────────────────────────────────────
  http.get(`${API}/runbooks`, () =>
    HttpResponse.json({
      items: [
        { runbookId: 'rb-1', title: 'Mitigar latência de pagamentos', serviceId: 'svc-payments-api', serviceDisplayName: 'Payments API', incidentType: 'Performance', stepCount: 6, lastUpdatedAt: daysAgo(12), updatedBy: 'ana.silva@nextraceone.dev' },
        { runbookId: 'rb-2', title: 'Recuperação de fila de eventos', serviceId: 'svc-inventory-graphql', serviceDisplayName: 'Inventory GraphQL', incidentType: 'Performance', stepCount: 4, lastUpdatedAt: daysAgo(20), updatedBy: 'joao.costa@nextraceone.dev' },
        { runbookId: 'rb-3', title: 'Rollback de deploy com erros 500', serviceId: 'svc-orders-api', serviceDisplayName: 'Orders API', incidentType: 'Availability', stepCount: 5, lastUpdatedAt: daysAgo(5), updatedBy: 'ana.silva@nextraceone.dev' },
      ],
      totalCount: 3, page: 1, pageSize: 20,
    }),
  ),

  // ── Reliability (lista de serviços) ─────────────────────────────────
  http.get(`${API}/reliability/services`, () =>
    HttpResponse.json({
      items: [
        { serviceName: 'svc-payments-api', displayName: 'Payments API', serviceType: 'RestApi', domain: 'Billing', teamName: 'Payments', criticality: 'Critical', reliabilityStatus: 'NeedsAttention', operationalSummary: '1 incidente crítico ativo; SLO de latência sob pressão.', trend: 'Declining', activeFlags: 2, openIncidents: 1, recentChangeImpact: true, overallScore: 74, lastComputedAt: hoursAgo(1) },
        { serviceName: 'svc-orders-api', displayName: 'Orders API', serviceType: 'RestApi', domain: 'Commerce', teamName: 'Orders', criticality: 'High', reliabilityStatus: 'Healthy', operationalSummary: 'Dentro dos SLOs; sem incidentes abertos.', trend: 'Stable', activeFlags: 0, openIncidents: 1, recentChangeImpact: true, overallScore: 88, lastComputedAt: hoursAgo(1) },
        { serviceName: 'svc-inventory-graphql', displayName: 'Inventory GraphQL', serviceType: 'GraphqlApi', domain: 'Commerce', teamName: 'Inventory', criticality: 'Medium', reliabilityStatus: 'Degraded', operationalSummary: 'Fila de eventos acumulada; latência acima do baseline.', trend: 'Declining', activeFlags: 1, openIncidents: 1, recentChangeImpact: false, overallScore: 63, lastComputedAt: hoursAgo(2) },
        { serviceName: 'svc-notifications-worker', displayName: 'Notifications Worker', serviceType: 'BackgroundService', domain: 'Platform', teamName: 'Platform', criticality: 'Low', reliabilityStatus: 'Healthy', operationalSummary: 'Estável após resolução do timeout.', trend: 'Improving', activeFlags: 0, openIncidents: 0, recentChangeImpact: false, overallScore: 95, lastComputedAt: hoursAgo(3) },
      ],
      totalCount: 4, page: 1, pageSize: 20,
    }),
  ),

  // ── SRE Dashboard (telemetria agregada) ─────────────────────────────
  http.get(`${API}/telemetry/sre/summary`, () =>
    HttpResponse.json({
      problems: { open: 3, total: 12 },
      slo: { errorCompliancePct: 99.4, latencyCompliancePct: 98.1 },
      traffic: { requestCount: 1284000, queryCount: 842000 },
      latency: { requestAvgMs: 182, queryAvgMs: 14 },
      errors: { http5xx: 214, http4xx: 1830, queryErrors: 42, logErrors: 96 },
    }),
  ),
  http.get(`${API}/telemetry/sre/timeseries`, () => HttpResponse.json(sreTimeSeries())),
  http.get(`${API}/telemetry/sre/top-requests`, () =>
    HttpResponse.json([
      { service: 'Payments API', request: 'POST /payments', count: 184000, avgLatencyMs: 210, errors: 120 },
      { service: 'Orders API', request: 'GET /orders/{id}', count: 142000, avgLatencyMs: 96, errors: 38 },
      { service: 'Inventory GraphQL', request: 'POST /graphql', count: 98000, avgLatencyMs: 156, errors: 21 },
    ]),
  ),
  http.get(`${API}/telemetry/sre/top-queries`, () =>
    HttpResponse.json([
      { database: 'payments-db', query: 'SELECT * FROM payments WHERE status=$1', count: 96000, avgLatencyMs: 12 },
      { database: 'orders-db', query: 'UPDATE orders SET state=$1 WHERE id=$2', count: 74000, avgLatencyMs: 9 },
    ]),
  ),

  // ── Reliability: SLOs de um serviço ─────────────────────────────────
  http.get(`${API}/reliability/services/:serviceId/slos`, ({ params }) =>
    HttpResponse.json({
      serviceId: String(params.serviceId),
      items: [
        { id: 'slo-1', name: 'Disponibilidade API', serviceId: String(params.serviceId), environment: 'production', type: 'Availability', targetPercent: 99.9, alertThresholdPercent: 99.5, windowDays: 30, isActive: true },
        { id: 'slo-2', name: 'Latência P99', serviceId: String(params.serviceId), environment: 'production', type: 'Latency', targetPercent: 99.0, alertThresholdPercent: 98.0, windowDays: 30, isActive: true },
        { id: 'slo-3', name: 'Taxa de Erro', serviceId: String(params.serviceId), environment: 'production', type: 'ErrorRate', targetPercent: 99.5, alertThresholdPercent: null, windowDays: 7, isActive: false },
      ],
    }),
  ),

  // ── Reliability: detalhe de um serviço ──────────────────────────────
  http.get(`${API}/reliability/services/:serviceId`, ({ params }) =>
    HttpResponse.json({
      identity: { serviceId: String(params.serviceId), displayName: 'Payments API', serviceType: 'RestApi', domain: 'Billing', teamName: 'Payments', criticality: 'Critical' },
      status: 'NeedsAttention',
      operationalSummary: '1 incidente crítico ativo; SLO de latência sob pressão nas últimas 3 horas.',
      trend: { direction: 'Declining', timeframe: '24h', summary: 'Latência P99 subiu 18% após o último deploy.' },
      metrics: { availabilityPercent: 99.62, latencyP99Ms: 412, errorRatePercent: 0.34, requestsPerSecond: 1450, queueLag: null, processingDelay: null },
      activeFlags: 2,
      recentChanges: [
        { changeId: 'chg-9001', description: 'Deploy v2.14.0 — novo motor de reconciliação', changeType: 'Deployment', confidenceStatus: 'Watch', deployedAt: hoursAgo(4) },
        { changeId: 'chg-8994', description: 'Ajuste de connection pool', changeType: 'Configuration', confidenceStatus: 'Healthy', deployedAt: daysAgo(2) },
      ],
      linkedIncidents: [
        { incidentId: 'inc-1', reference: 'INC-2041', title: 'Latência elevada no processamento de pagamentos', status: 'Investigating', reportedAt: hoursAgo(3) },
      ],
      dependencies: [
        { serviceId: 'svc-orders-api', displayName: 'Orders API', status: 'Healthy' },
        { serviceId: 'svc-ledger-db', displayName: 'Ledger DB', status: 'Degraded' },
      ],
      linkedContracts: [
        { contractVersionId: 'cv-1', name: 'Payments REST', version: '2', protocol: 'REST', lifecycleState: 'Published' },
      ],
      runbooks: [{ title: 'Mitigar latência de pagamentos', url: '/operations/runbooks/rb-1/edit' }],
      anomalySummary: 'Deteção de anomalia: pico de latência correlacionado com o deploy chg-9001.',
      coverage: { hasOperationalSignals: true, hasRunbook: true, hasOwner: true, hasDependenciesMapped: true, hasRecentChangeContext: true, hasIncidentLinkage: true },
    }),
  ),

  // ── Reliability: resumo de equipa ───────────────────────────────────
  http.get(`${API}/reliability/teams/:teamId/summary`, ({ params }) =>
    HttpResponse.json({
      teamId: String(params.teamId), totalServices: 8, healthyServices: 5, degradedServices: 2,
      unavailableServices: 0, needsAttentionServices: 1, criticalServicesImpacted: 1,
      openIncidents: 2, overallScore: 82, trend: 'Stable',
    }),
  ),

  // ── Reliability: SLO error-budget / burn-rate / SLAs ────────────────
  http.get(`${API}/reliability/slos/:sloId/error-budget`, ({ params }) =>
    HttpResponse.json({
      sloDefinitionId: String(params.sloId), sloName: 'Latência P99', serviceId: 'svc-payments-api', environment: 'production',
      targetPercent: 99.0, windowDays: 30, totalBudgetMinutes: 432, consumedBudgetMinutes: 261,
      remainingBudgetMinutes: 171, consumedPercent: 60.4, status: 'AtRisk', computedAt: hoursAgo(1),
    }),
  ),
  http.get(`${API}/reliability/slos/:sloId/burn-rate`, ({ params }) =>
    HttpResponse.json({
      sloDefinitionId: String(params.sloId), sloName: 'Latência P99', serviceId: 'svc-payments-api', environment: 'production',
      window: 'SixHours', burnRate: 1.8, observedErrorRate: 0.9, toleratedErrorRate: 0.5, status: 'AtRisk', computedAt: hoursAgo(1),
    }),
  ),
  http.get(`${API}/reliability/slos/:sloId/slas`, ({ params }) =>
    HttpResponse.json({
      sloDefinitionId: String(params.sloId), sloName: 'Latência P99',
      items: [
        { id: 'sla-1', name: 'Contrato Enterprise — Pagamentos', contractualTargetPercent: 99.5, status: 'Meeting', effectiveFrom: daysAgo(120), effectiveTo: null, hasPenaltyClauses: true, isActive: true },
      ],
    }),
  ),
  http.post(`${API}/reliability/slos/:sloId/compute-error-budget`, ({ params }) =>
    HttpResponse.json({
      sloDefinitionId: String(params.sloId), sloName: 'Latência P99', serviceId: 'svc-payments-api', environment: 'production',
      targetPercent: 99.0, windowDays: 30, totalBudgetMinutes: 432, consumedBudgetMinutes: 261,
      remainingBudgetMinutes: 171, consumedPercent: 60.4, status: 'AtRisk', computedAt: new Date().toISOString(),
    }),
  ),
  http.post(`${API}/reliability/slos/:sloId/compute-burn-rate`, ({ params }) =>
    HttpResponse.json({
      sloDefinitionId: String(params.sloId), sloName: 'Latência P99', serviceId: 'svc-payments-api', environment: 'production',
      observedErrorRate: 0.9, toleratedErrorRate: 0.5,
      snapshots: [
        { window: 'OneHour', burnRate: 2.4, status: 'Violated' },
        { window: 'SixHours', burnRate: 1.8, status: 'AtRisk' },
        { window: 'TwentyFourHours', burnRate: 1.1, status: 'AtRisk' },
        { window: 'SevenDays', burnRate: 0.7, status: 'Healthy' },
      ],
      computedAt: new Date().toISOString(),
    }),
  ),
  http.post(`${API}/reliability/slos`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as {
      name?: string; serviceId?: string; environment?: string; type?: string; targetPercent?: number; windowDays?: number;
    };
    return HttpResponse.json({
      id: 'slo-new', name: body.name ?? 'Novo SLO', serviceId: body.serviceId ?? 'svc-payments-api',
      environment: body.environment ?? 'production', type: body.type ?? 'Availability',
      targetPercent: body.targetPercent ?? 99.9, windowDays: body.windowDays ?? 30,
    }, { status: 201 });
  }),
];
