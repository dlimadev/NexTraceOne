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

  // ── On-Call Intelligence (literal — antes de /incidents/:id) ────────
  http.get(`${API}/incidents/on-call-intelligence`, ({ request }) => {
    const periodDays = Number(new URL(request.url).searchParams.get('periodDays') ?? 30);
    return HttpResponse.json({
      periodDays, generatedAt: new Date().toISOString(),
      totalIncidentsInPeriod: 42, avgIncidentsPerWeek: 9.8, peakHour: 14, peakDayOfWeek: 'Tuesday',
      fatigueSeverity: 'Moderate',
      recommendations: [
        'Rever a rotação da equipa de Pagamentos — 3 chamadas noturnas na última semana.',
        'Considerar follow-the-sun para reduzir a fadiga fora de horas.',
      ],
      distribution: [
        { hour: 14, dayOfWeek: 'Tuesday', incidentCount: 6 },
        { hour: 9, dayOfWeek: 'Monday', incidentCount: 5 },
        { hour: 22, dayOfWeek: 'Friday', incidentCount: 4 },
        { hour: 3, dayOfWeek: 'Sunday', incidentCount: 2 },
      ],
      teamFatigue: [
        { teamName: 'Payments', incidentsLastWeek: 8, incidentsLastMonth: 24, avgResponseMinutes: 12, fatigueLevel: 'High' },
        { teamName: 'Orders', incidentsLastWeek: 3, incidentsLastMonth: 11, avgResponseMinutes: 18, fatigueLevel: 'Moderate' },
        { teamName: 'Platform', incidentsLastWeek: 1, incidentsLastMonth: 6, avgResponseMinutes: 22, fatigueLevel: 'Low' },
      ],
    });
  }),

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

  // ── Automação: catálogo de ações ────────────────────────────────────
  http.get(`${API}/automation/actions`, () =>
    HttpResponse.json({
      items: [
        { actionId: 'act-1', name: 'restart_service', displayName: 'Reiniciar serviço', description: 'Reinicia as instâncias do serviço de forma controlada.', actionType: 'Remediation', riskLevel: 'Medium', requiresApproval: true, allowedPersonas: ['SRE', 'Platform'], allowedEnvironments: ['staging', 'production'], preconditionTypes: ['HealthCheck'], hasPostValidation: true },
        { actionId: 'act-2', name: 'scale_out', displayName: 'Escalar horizontalmente', description: 'Adiciona réplicas ao serviço para absorver carga.', actionType: 'Scaling', riskLevel: 'Low', requiresApproval: false, allowedPersonas: ['SRE'], allowedEnvironments: ['production'], preconditionTypes: ['CapacityCheck'], hasPostValidation: true },
        { actionId: 'act-3', name: 'rollback_deploy', displayName: 'Reverter deploy', description: 'Reverte para a versão anterior estável.', actionType: 'Rollback', riskLevel: 'High', requiresApproval: true, allowedPersonas: ['SRE', 'ReleaseManager'], allowedEnvironments: ['production'], preconditionTypes: ['ChangeCorrelation'], hasPostValidation: true },
      ],
    }),
  ),

  // ── Automação: trilha de auditoria ──────────────────────────────────
  http.get(`${API}/automation/audit`, () =>
    HttpResponse.json({
      entries: [
        { entryId: 'wa-1', workflowId: 'wf-1', action: 'WorkflowApproved', performedBy: 'ana.silva@nextraceone.dev', performedAt: hoursAgo(2), details: 'Rollback aprovado para Payments API.', serviceId: 'svc-payments-api', teamId: 'payments' },
        { entryId: 'wa-2', workflowId: 'wf-1', action: 'WorkflowCreated', performedBy: 'joao.costa@nextraceone.dev', performedAt: hoursAgo(3), details: null, serviceId: 'svc-payments-api', teamId: 'payments' },
        { entryId: 'wa-3', workflowId: 'wf-2', action: 'WorkflowExecuted', performedBy: 'sistema', performedAt: hoursAgo(20), details: 'Escalonamento concluído com sucesso.', serviceId: 'svc-orders-api', teamId: 'orders' },
      ],
    }),
  ),

  // ── Automação: workflow (detalhe) ───────────────────────────────────
  http.get(`${API}/automation/workflows/:workflowId`, ({ params }) =>
    HttpResponse.json({
      workflowId: String(params.workflowId), actionId: 'act-3', actionDisplayName: 'Reverter deploy',
      status: 'PendingApproval', riskLevel: 'High',
      rationale: 'Deploy chg-9001 correlacionado com o incidente INC-2041; rollback recomendado.',
      requestedBy: 'joao.costa@nextraceone.dev', approverInfo: null, scope: 'svc-payments-api',
      environment: 'production', serviceId: 'svc-payments-api', incidentId: 'inc-1', changeId: 'chg-9001',
      preconditions: [
        { type: 'ChangeCorrelation', description: 'Existe uma mudança correlacionada elegível para rollback.', status: 'Passed', evaluatedAt: hoursAgo(1) },
        { type: 'HealthCheck', description: 'Serviço acessível para operação de rollback.', status: 'Passed', evaluatedAt: hoursAgo(1) },
      ],
      executionSteps: [
        { stepOrder: 1, title: 'Validar versão-alvo do rollback', status: 'Pending', completedAt: null, completedBy: null },
        { stepOrder: 2, title: 'Executar rollback via pipeline', status: 'Pending', completedAt: null, completedBy: null },
        { stepOrder: 3, title: 'Validar sinais pós-mitigação', status: 'Pending', completedAt: null, completedBy: null },
      ],
      validationInfo: null,
      auditEntries: [
        { action: 'WorkflowCreated', performedBy: 'joao.costa@nextraceone.dev', performedAt: hoursAgo(3), details: null },
      ],
      createdAt: hoursAgo(3), updatedAt: hoursAgo(1),
    }),
  ),

  // ── Automação: workflows (lista) ────────────────────────────────────
  http.get(`${API}/automation/workflows`, () =>
    HttpResponse.json({
      items: [
        { workflowId: 'wf-1', actionId: 'act-3', actionDisplayName: 'Reverter deploy', status: 'PendingApproval', riskLevel: 'High', requestedBy: 'joao.costa@nextraceone.dev', serviceId: 'svc-payments-api', createdAt: hoursAgo(3) },
        { workflowId: 'wf-2', actionId: 'act-2', actionDisplayName: 'Escalar horizontalmente', status: 'Completed', riskLevel: 'Low', requestedBy: 'ana.silva@nextraceone.dev', serviceId: 'svc-orders-api', createdAt: daysAgo(1) },
        { workflowId: 'wf-3', actionId: 'act-1', actionDisplayName: 'Reiniciar serviço', status: 'InProgress', riskLevel: 'Medium', requestedBy: 'sre-oncall@nextraceone.dev', serviceId: 'svc-inventory-graphql', createdAt: hoursAgo(6) },
      ],
      totalCount: 3,
    }),
  ),

  // ── Platform Operations: health / jobs / queues / events / config ───
  http.get(`${API}/platform/health`, () =>
    HttpResponse.json({
      overallStatus: 'Degraded',
      subsystems: [
        { name: 'PostgreSQL', status: 'Healthy', description: 'Conexões dentro do limite; latência normal.', lastCheckedAt: hoursAgo(0.05) },
        { name: 'ClickHouse', status: 'Healthy', description: 'Ingestão de telemetria operacional.', lastCheckedAt: hoursAgo(0.05) },
        { name: 'Redis', status: 'Degraded', description: 'Latência de cache acima do baseline.', lastCheckedAt: hoursAgo(0.05) },
        { name: 'Outbox Processor', status: 'Healthy', description: 'Fila de outbox sem atrasos.', lastCheckedAt: hoursAgo(0.05) },
      ],
      uptimeSeconds: 1_284_500, version: '2.14.0', checkedAt: hoursAgo(0.05),
    }),
  ),
  http.get(`${API}/platform/jobs`, () =>
    HttpResponse.json({
      jobs: [
        { jobId: 'job-1', name: 'LicenseRecalculationJob', status: 'Completed', lastRunAt: hoursAgo(0.2), nextRunAt: hoursAgo(-0.05), executionCount: 4820, failureCount: 2, lastError: null },
        { jobId: 'job-2', name: 'AlertEvaluationJob', status: 'Running', lastRunAt: hoursAgo(0.02), nextRunAt: null, executionCount: 96210, failureCount: 14, lastError: null },
        { jobId: 'job-3', name: 'RetentionSweepJob', status: 'Scheduled', lastRunAt: daysAgo(1), nextRunAt: hoursAgo(-6), executionCount: 365, failureCount: 0, lastError: null },
        { jobId: 'job-4', name: 'OutboxDispatchJob', status: 'Failed', lastRunAt: hoursAgo(1), nextRunAt: hoursAgo(-0.1), executionCount: 15200, failureCount: 3, lastError: 'Timeout ao conectar ao broker.' },
      ],
      totalCount: 4, page: 1, pageSize: 20,
    }),
  ),
  http.get(`${API}/platform/queues`, () =>
    HttpResponse.json({
      queues: [
        { queueName: 'outbox.default', pendingCount: 42, processingCount: 3, failedCount: 1, deadLetterCount: 0, averageProcessingMs: 34, lastActivityAt: hoursAgo(0.02) },
        { queueName: 'notifications.email', pendingCount: 8, processingCount: 1, failedCount: 0, deadLetterCount: 0, averageProcessingMs: 120, lastActivityAt: hoursAgo(0.1) },
        { queueName: 'ingestion.telemetry', pendingCount: 310, processingCount: 12, failedCount: 4, deadLetterCount: 2, averageProcessingMs: 18, lastActivityAt: hoursAgo(0.01) },
      ],
      checkedAt: hoursAgo(0.02),
    }),
  ),
  http.get(`${API}/platform/events`, () =>
    HttpResponse.json({
      events: [
        { eventId: 'evt-1', timestamp: hoursAgo(0.5), severity: 'Warning', subsystem: 'Redis', message: 'Latência de cache acima de 5ms.', correlationId: 'corr-8891', resolved: false },
        { eventId: 'evt-2', timestamp: hoursAgo(1), severity: 'Error', subsystem: 'Outbox Processor', message: 'Falha ao despachar mensagem outbox (timeout do broker).', correlationId: 'corr-8890', resolved: false },
        { eventId: 'evt-3', timestamp: hoursAgo(4), severity: 'Info', subsystem: 'Scheduler', message: 'RetentionSweepJob agendado.', correlationId: null, resolved: true },
      ],
      totalCount: 3, page: 1, pageSize: 20,
    }),
  ),
  http.get(`${API}/platform/config`, () =>
    HttpResponse.json({
      environmentName: 'production', deploymentMode: 'Kubernetes',
      featureFlags: [
        { name: 'ai_governance', enabled: true, description: 'Governança de IA (registo de modelos, routing).' },
        { name: 'multi_region', enabled: false, description: 'Replicação multi-região.' },
      ],
      subsystems: [
        { name: 'Kafka', enabled: false, description: 'Streaming de eventos (opcional).' },
        { name: 'Redis', enabled: true, description: 'Cache distribuído.' },
      ],
      databases: [
        { name: 'NexTraceOne', provider: 'PostgreSQL', connected: true, statusDescription: 'Conectado.' },
        { name: 'Analytics', provider: 'ClickHouse', connected: true, statusDescription: 'Conectado.' },
      ],
      generatedAt: hoursAgo(0.05),
    }),
  ),

  // ── Telemetria: Log Explorer ────────────────────────────────────────
  http.get(`${API}/telemetry/logs`, () =>
    HttpResponse.json([
      { timestamp: hoursAgo(0.1), environment: 'production', serviceName: 'Payments API', applicationName: 'payments-api', moduleName: 'ReconciliationEngine', level: 'Error', message: 'Timeout ao gravar transação no Ledger DB', exception: 'System.TimeoutException: The operation has timed out.', traceId: 'trace-9a1', spanId: 'span-01', correlationId: 'corr-8891', hostName: 'pod-payments-7f9', containerName: 'payments-api' },
      { timestamp: hoursAgo(0.2), environment: 'production', serviceName: 'Payments API', level: 'Warning', message: 'Latência de escrita acima do baseline (412ms)', traceId: 'trace-9a2', spanId: 'span-02', hostName: 'pod-payments-7f9' },
      { timestamp: hoursAgo(0.3), environment: 'production', serviceName: 'Orders API', level: 'Information', message: 'Pedido processado com sucesso', traceId: 'trace-9b1', spanId: 'span-10', hostName: 'pod-orders-4a2' },
      { timestamp: hoursAgo(0.4), environment: 'production', serviceName: 'Inventory GraphQL', level: 'Warning', message: 'Fila de eventos acima de 500 mensagens', traceId: 'trace-9c1', spanId: 'span-20' },
    ]),
  ),

  // ── Telemetria: Trace Explorer ──────────────────────────────────────
  http.get(`${API}/telemetry/traces/:traceId`, ({ params }) => {
    const id = String(params.traceId);
    return HttpResponse.json({
      traceId: id, durationMs: 486, services: ['Payments API', 'Ledger DB'],
      spans: [
        { traceId: id, spanId: 'span-01', serviceName: 'Payments API', operationName: 'POST /payments', startTime: hoursAgo(0.1), endTime: hoursAgo(0.099), durationMs: 486, statusCode: 'Error', statusMessage: 'Timeout', environment: 'production', spanKind: 'Server', serviceKind: 'REST', events: [{ name: 'exception', timestamp: hoursAgo(0.099), attributes: { 'exception.type': 'TimeoutException' } }] },
        { traceId: id, spanId: 'span-02', parentSpanId: 'span-01', serviceName: 'Ledger DB', operationName: 'INSERT ledger_entries', startTime: hoursAgo(0.1), endTime: hoursAgo(0.099), durationMs: 402, statusCode: 'Error', environment: 'production', spanKind: 'Client', serviceKind: 'DB' },
      ],
    });
  }),
  http.get(`${API}/telemetry/traces`, () =>
    HttpResponse.json([
      { traceId: 'trace-9a1', serviceName: 'Payments API', operationName: 'POST /payments', startTime: hoursAgo(0.1), durationMs: 486, statusCode: 'Error', environment: 'production', spanCount: 6, hasErrors: true, rootServiceKind: 'REST' },
      { traceId: 'trace-9b1', serviceName: 'Orders API', operationName: 'GET /orders/{id}', startTime: hoursAgo(0.3), durationMs: 96, statusCode: 'Ok', environment: 'production', spanCount: 4, hasErrors: false, rootServiceKind: 'REST' },
      { traceId: 'trace-9c1', serviceName: 'Inventory GraphQL', operationName: 'POST /graphql', startTime: hoursAgo(0.4), durationMs: 156, statusCode: 'Ok', environment: 'production', spanCount: 8, hasErrors: false, rootServiceKind: 'REST' },
    ]),
  ),
  http.get(`${API}/telemetry/correlate/:traceId`, ({ params }) => {
    const id = String(params.traceId);
    return HttpResponse.json({
      traceId: id,
      logs: [{ timestamp: hoursAgo(0.1), environment: 'production', serviceName: 'Payments API', level: 'Error', message: 'Timeout ao gravar transação', traceId: id, spanId: 'span-01' }],
      spans: [{ traceId: id, spanId: 'span-01', serviceName: 'Payments API', operationName: 'POST /payments', startTime: hoursAgo(0.1), endTime: hoursAgo(0.099), durationMs: 486, statusCode: 'Error', environment: 'production', spanKind: 'Server', serviceKind: 'REST' }],
    });
  }),
  http.get(`${API}/telemetry/metrics`, () =>
    HttpResponse.json(series(180).map((p) => ({ timestamp: p.timestamp, metricName: 'latency_p99_ms', value: p.value, serviceName: 'Payments API', environment: 'production' }))),
  ),
  http.get(`${API}/telemetry/errors/top`, () =>
    HttpResponse.json([
      { errorMessage: 'System.TimeoutException: operation timed out', count: 214, serviceName: 'Payments API', lastSeen: hoursAgo(0.1), level: 'Error' },
      { errorMessage: 'Npgsql.PostgresException: deadlock detected', count: 58, serviceName: 'Ledger DB', lastSeen: hoursAgo(0.5), level: 'Error' },
    ]),
  ),
  http.get(`${API}/telemetry/latency/compare`, () =>
    HttpResponse.json({ serviceName: 'Payments API', environmentA: 'staging', environmentB: 'production', latencyP50MsA: 78, latencyP50MsB: 96, latencyP95MsA: 180, latencyP95MsB: 320, latencyP99MsA: 260, latencyP99MsB: 412, driftPercentP95: 77.8 }),
  ),
  http.get(`${API}/telemetry/health`, () => HttpResponse.json({ provider: 'ClickHouse', healthy: true })),

  // ── Telemetria: Request Explorer ────────────────────────────────────
  http.get(`${API}/telemetry/requests/facets`, () =>
    HttpResponse.json({
      services: ['Payments API', 'Orders API', 'Inventory GraphQL'],
      endpoints: ['POST /payments', 'GET /orders/{id}', 'POST /graphql'],
      processGroups: ['payments-api', 'orders-api'],
      k8sNamespaces: ['production', 'staging'],
      k8sWorkloads: ['payments-api', 'orders-api', 'inventory-graphql'],
    }),
  ),
  http.get(`${API}/telemetry/requests`, () =>
    HttpResponse.json({
      total: 3, page: 1, pageSize: 50,
      items: [
        { startTime: hoursAgo(0.1), endpoint: 'POST /payments', service: 'Payments API', durationMs: 486, requestStatus: 'Failure', httpCode: 500, processGroup: 'payments-api', k8sWorkload: 'payments-api', k8sNamespace: 'production', spanKind: 'server', spanStatus: 'Error', traceId: 'trace-9a1', spanId: 'span-01' },
        { startTime: hoursAgo(0.3), endpoint: 'GET /orders/{id}', service: 'Orders API', durationMs: 96, requestStatus: 'Success', httpCode: 200, processGroup: 'orders-api', k8sWorkload: 'orders-api', k8sNamespace: 'production', spanKind: 'server', spanStatus: 'Ok', traceId: 'trace-9b1', spanId: 'span-10' },
        { startTime: hoursAgo(0.4), endpoint: 'POST /graphql', service: 'Inventory GraphQL', durationMs: 156, requestStatus: 'Success', httpCode: 200, k8sNamespace: 'production', spanKind: 'server', spanStatus: 'Ok', traceId: 'trace-9c1', spanId: 'span-20' },
      ],
      histogram: [
        { durationLabel: '0-100ms', successCount: 1820, failureCount: 4 },
        { durationLabel: '100-300ms', successCount: 640, failureCount: 12 },
        { durationLabel: '300-500ms', successCount: 90, failureCount: 48 },
        { durationLabel: '>500ms', successCount: 12, failureCount: 30 },
      ],
    }),
  ),

  // ── Telemetria: Profiling / DB / Erros / Sintético ──────────────────
  http.get(`${API}/telemetry/profiling/sessions`, () =>
    HttpResponse.json([
      { id: 'prof-1', serviceName: 'Payments API', version: '2.14.0', environment: 'production', cpuPercent: 82, memoryMb: 1240, heapMb: 890, sampleCount: 15200, durationMs: 60000, deployCorrelated: true, deployId: 'chg-9001', capturedAt: hoursAgo(2), profileType: 'cpu' },
      { id: 'prof-2', serviceName: 'Orders API', version: '1.9.2', environment: 'production', cpuPercent: 34, memoryMb: 620, heapMb: 410, sampleCount: 9800, durationMs: 60000, deployCorrelated: false, capturedAt: hoursAgo(5), profileType: 'memory' },
    ]),
  ),
  http.get(`${API}/telemetry/db/slow-queries`, () =>
    HttpResponse.json([
      { id: 'sq-1', fingerprint: 'a1b2', database: 'payments-db', avgDurationMs: 412, maxDurationMs: 1820, executionCount: 96000, totalTimeMs: 39552000, lockWaitMs: 180, hasIndexMiss: true, indexMissCount: 34, recommendation: 'Adicionar índice em payments(status, created_at).', environment: 'production' },
      { id: 'sq-2', fingerprint: 'c3d4', database: 'orders-db', avgDurationMs: 88, maxDurationMs: 320, executionCount: 74000, totalTimeMs: 6512000, lockWaitMs: 12, hasIndexMiss: false, indexMissCount: 0, environment: 'production' },
    ]),
  ),
  http.get(`${API}/telemetry/errors/groups`, () =>
    HttpResponse.json([
      { id: 'eg-1', fingerprint: 'to-1', message: 'System.TimeoutException: operation timed out', serviceName: 'Payments API', count: 214, affectedUsers: 88, status: 'regressing', firstSeen: hoursAgo(4), lastSeen: hoursAgo(0.1), deployCorrelated: true, deployId: 'chg-9001', environment: 'production', stackTraceSummary: 'at ReconciliationEngine.WriteAsync()' },
      { id: 'eg-2', fingerprint: 'dl-1', message: 'Npgsql.PostgresException: deadlock detected', serviceName: 'Ledger DB', count: 58, affectedUsers: 21, status: 'new', firstSeen: hoursAgo(3), lastSeen: hoursAgo(0.5), deployCorrelated: false, environment: 'production' },
    ]),
  ),
  http.get(`${API}/telemetry/synthetic/probes`, () =>
    HttpResponse.json([
      { id: 'probe-1', name: 'Payments — checkout flow', type: 'httpMultiStep', target: 'https://api.nextraceone.dev/payments', status: 'degraded', uptimePercent: 98.4, lastCheck: hoursAgo(0.05), lastResult: 'Latência 412ms (> limiar 300ms)', schedule: '1m', contractValidation: 'pass', environment: 'production' },
      { id: 'probe-2', name: 'Orders — health', type: 'httpSingle', target: 'https://api.nextraceone.dev/orders/health', status: 'healthy', uptimePercent: 99.98, lastCheck: hoursAgo(0.05), lastResult: '200 OK', schedule: '30s', contractValidation: 'pass', environment: 'production' },
    ]),
  ),
  http.get(`${API}/telemetry/api/regressions`, () =>
    HttpResponse.json([
      { id: 'reg-1', endpoint: 'POST /payments', serviceName: 'Payments API', p50BaselineMs: 78, p50CurrentMs: 96, p95BaselineMs: 180, p95CurrentMs: 320, p99BaselineMs: 260, p99CurrentMs: 412, regressionPercent: 58, status: 'regressed', deployId: 'chg-9001', changeConfidence: 'high', environment: 'production' },
      { id: 'reg-2', endpoint: 'GET /orders/{id}', serviceName: 'Orders API', p50BaselineMs: 42, p50CurrentMs: 40, p95BaselineMs: 96, p95CurrentMs: 92, p99BaselineMs: 140, p99CurrentMs: 138, regressionPercent: -2, status: 'stable', changeConfidence: 'low', environment: 'production' },
    ]),
  ),

  // ── Operações: dependency-risk / load-tests / maturity / post-mortems / on-call ──
  http.get(`${API}/operations/dependency-risk`, () =>
    HttpResponse.json([
      { id: 'dr-1', serviceName: 'Ledger DB', riskScore: 82, riskLevel: 'critical', failureCount30d: 6, sloHealthPercent: 91, blastRadius: 12, deployFrequency: 3, dependentsCount: 8, trendDirection: 'up', environment: 'production' },
      { id: 'dr-2', serviceName: 'Payments API', riskScore: 64, riskLevel: 'high', failureCount30d: 3, sloHealthPercent: 96, blastRadius: 6, deployFrequency: 9, dependentsCount: 4, trendDirection: 'stable', environment: 'production' },
      { id: 'dr-3', serviceName: 'Notifications Worker', riskScore: 28, riskLevel: 'low', failureCount30d: 0, sloHealthPercent: 99, blastRadius: 2, deployFrequency: 2, dependentsCount: 1, trendDirection: 'down', environment: 'production' },
    ]),
  ),
  http.get(`${API}/operations/load-tests`, () =>
    HttpResponse.json([
      { id: 'lt-1', name: 'Payments — pico Black Friday', serviceName: 'Payments API', source: 'k6', status: 'passed', vus: 2000, durationMs: 600000, p95LatencyMs: 280, errorRate: 0.4, maxCapacityVus: 3200, maxRps: 4800, executedAt: daysAgo(2), environment: 'staging' },
      { id: 'lt-2', name: 'Orders — carga sustentada', serviceName: 'Orders API', source: 'gatling', status: 'failed', vus: 1500, durationMs: 300000, p95LatencyMs: 620, errorRate: 3.8, maxCapacityVus: 1200, maxRps: 2100, executedAt: daysAgo(1), environment: 'staging' },
    ]),
  ),
  http.get(`${API}/operations/service-maturity`, () =>
    HttpResponse.json([
      { id: 'sm-1', serviceName: 'Payments API', teamName: 'Payments', score: 88, maturityLevel: 'advanced', hasSlo: true, hasRunbook: true, hasOnCall: true, hasAlerts: true, hasProfiling: true, hasRecentPostMortem: true, environment: 'production' },
      { id: 'sm-2', serviceName: 'Inventory GraphQL', teamName: 'Inventory', score: 54, maturityLevel: 'basic', hasSlo: true, hasRunbook: false, hasOnCall: false, hasAlerts: true, hasProfiling: false, hasRecentPostMortem: false, environment: 'production' },
    ]),
  ),
  http.get(`${API}/operations/post-mortems`, () =>
    HttpResponse.json([
      { id: 'pm-1', title: 'Degradação de latência nos pagamentos', incidentId: 'inc-1', incidentTitle: 'Latência elevada no processamento de pagamentos', status: 'review', author: 'ana.silva@nextraceone.dev', severity: 'Critical', actionItemsCount: 5, openActionItemsCount: 3, createdAt: daysAgo(1), patternCount: 2, environment: 'production' },
      { id: 'pm-2', title: 'Timeout no gateway de notificações', incidentId: 'inc-4', incidentTitle: 'Timeout no gateway de notificações', status: 'published', author: 'joao.costa@nextraceone.dev', severity: 'Warning', actionItemsCount: 3, openActionItemsCount: 0, createdAt: daysAgo(4), publishedAt: daysAgo(2), patternCount: 1, environment: 'production' },
    ]),
  ),
  http.get(`${API}/operations/on-call/schedules`, () =>
    HttpResponse.json([
      { id: 'oc-1', name: 'Payments — Primária', teamName: 'Payments', serviceName: 'Payments API', currentOnCall: 'ana.silva@nextraceone.dev', nextOnCall: 'joao.costa@nextraceone.dev', rotationType: 'weekly', timezone: 'Europe/Lisbon', escalationLevels: 3, activeOverrides: 1, environment: 'production' },
      { id: 'oc-2', name: 'Plataforma — Follow the Sun', teamName: 'Platform', serviceName: 'Notifications Worker', currentOnCall: 'sre-oncall@nextraceone.dev', nextOnCall: 'sre-emea@nextraceone.dev', rotationType: 'followTheSun', timezone: 'UTC', escalationLevels: 2, activeOverrides: 0, environment: 'production' },
    ]),
  ),

  // ── IA operacional: anomalias / resumos / sugestões de runbook ──────
  http.get(`${API}/ai/anomaly/detections`, () =>
    HttpResponse.json([
      { id: 'an-1', serviceName: 'Payments API', metric: 'latency_p99_ms', observedValue: 412, baselineValue: 180, sigmaDeviation: 4.2, severity: 'critical', explanation: 'Latência P99 desviou 4.2σ do baseline após o deploy v2.14.0.', detectedAt: hoursAgo(2), status: 'open', modelVersion: 'baseline-v3', environment: 'production' },
      { id: 'an-2', serviceName: 'Inventory GraphQL', metric: 'queue_depth', observedValue: 540, baselineValue: 120, sigmaDeviation: 3.1, severity: 'high', explanation: 'Profundidade da fila acima do esperado; possível consumidor lento.', detectedAt: hoursAgo(6), status: 'acknowledged', modelVersion: 'baseline-v3', environment: 'production' },
    ]),
  ),
  http.get(`${API}/ai/incident-summarizer/summaries`, () =>
    HttpResponse.json([
      { id: 'sum-1', incidentId: 'inc-1', incidentTitle: 'Latência elevada no processamento de pagamentos', severity: 'Critical', serviceName: 'Payments API', summaryText: 'O incidente foi despoletado pelo deploy v2.14.0, que introduziu um motor de reconciliação com contenção no connection pool. Recomenda-se rollback.', generatedAt: hoursAgo(1), modelName: 'qwen3.5:9b', confidencePercent: 87, tokensUsed: 1240, requestedBy: 'ana.silva@nextraceone.dev', environment: 'production' },
    ]),
  ),
  http.get(`${API}/ai/runbook-suggester/suggestions`, () =>
    HttpResponse.json([
      { id: 'rs-1', incidentId: 'inc-1', incidentTitle: 'Latência elevada no processamento de pagamentos', serviceName: 'Payments API', environment: 'production', version: '2.14.0', runbookTitle: 'Rollback de deploy com erros 500', runbookId: 'rb-3', confidencePercent: 91, reasoning: 'O incidente correlaciona-se com um deploy recente; o runbook de rollback é o mais aplicável.', modelName: 'qwen3.5:9b', suggestedAt: hoursAgo(1), status: 'pending', tokensUsed: 860, knowledgeSources: ['runbook:rb-3', 'incident:inc-1'] },
    ]),
  ),

  // ── Chaos Engineering ───────────────────────────────────────────────
  http.get(`${API}/runtime/chaos/experiments`, () =>
    HttpResponse.json({
      items: [
        { experimentId: 'chaos-1', serviceName: 'payments-api', environment: 'Staging', experimentType: 'latency-injection', riskLevel: 'Medium', status: 'Completed', createdAt: daysAgo(2) },
        { experimentId: 'chaos-2', serviceName: 'orders-api', environment: 'Staging', experimentType: 'pod-kill', riskLevel: 'High', status: 'Running', createdAt: hoursAgo(1) },
      ],
      totalCount: 2,
    }),
  ),
  http.post(`${API}/runtime/chaos/experiments`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { serviceName?: string; environment?: string; experimentType?: string; durationSeconds?: number; targetPercentage?: number };
    return HttpResponse.json({
      experimentId: 'chaos-new', serviceName: body.serviceName ?? 'payments-api', environment: body.environment ?? 'Development',
      experimentType: body.experimentType ?? 'latency-injection',
      steps: ['Validar pré-condições de segurança', 'Injetar falha no alvo', 'Monitorizar sinais de saúde', 'Reverter e validar recuperação'],
      riskLevel: 'Medium', estimatedDurationSeconds: body.durationSeconds ?? 60, targetPercentage: body.targetPercentage ?? 10,
      safetyChecks: ['SLO de disponibilidade acima de 99%', 'Sem incidentes críticos ativos'],
      createdAt: new Date().toISOString(),
    }, { status: 201 });
  }),

  // ── Inteligência Preditiva ──────────────────────────────────────────
  http.post(`${API}/predictive/service-failure`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { serviceId?: string; serviceName?: string; predictionHorizon?: string };
    return HttpResponse.json({
      predictionId: 'pred-1', serviceId: body.serviceId ?? 'svc-payments-api', serviceName: body.serviceName ?? 'Payments API',
      failureProbabilityPercent: 62, riskLevel: 'High', predictionHorizon: body.predictionHorizon ?? '24h',
      causalFactors: ['Taxa de erro acima do baseline', 'Deploy recente correlacionado (chg-9001)', 'SLO de latência sob pressão'],
      recommendedAction: 'Preparar rollback do deploy chg-9001 e reforçar monitorização nas próximas 6h.',
      computedAt: new Date().toISOString(),
    });
  }),
  http.get(`${API}/predictive/change-risk/:changeId`, ({ params }) =>
    HttpResponse.json({
      changeId: String(params.changeId), serviceId: 'svc-payments-api', riskScore: 71, riskLevel: 'High',
      riskFactors: ['Alteração breaking', 'Blast radius elevado', 'Fora do horário comercial'],
      recommendations: ['Exigir aprovação sénior', 'Executar em janela de baixo tráfego', 'Garantir plano de rollback testado'],
      assessedAt: new Date().toISOString(),
    }),
  ),

  // ── Comparação de Ambientes / Runtime Intelligence ──────────────────
  http.get(`${API}/runtime/compare`, () =>
    HttpResponse.json({
      serviceName: 'payments-api', environment: 'production',
      beforeMetrics: { avgLatencyMs: 180, p99LatencyMs: 260, errorRate: 0.08, requestsPerSecond: 1420, cpuUsagePercent: 48, memoryUsageMb: 780 },
      afterMetrics: { avgLatencyMs: 320, p99LatencyMs: 412, errorRate: 0.34, requestsPerSecond: 1450, cpuUsagePercent: 74, memoryUsageMb: 1240 },
      beforeDataPoints: 8640, afterDataPoints: 8720,
      latencyDeltaPercent: 77.8, errorRateDeltaPercent: 325, throughputDeltaPercent: 2.1,
    }),
  ),
  http.get(`${API}/runtime/drift`, () =>
    HttpResponse.json({
      items: [
        { id: 'drift-1', serviceName: 'payments-api', environment: 'production', metricName: 'latency_p99_ms', expectedValue: 260, actualValue: 412, deviationPercent: 58.5, severity: 'High', detectedAt: hoursAgo(2), acknowledgedAt: null },
        { id: 'drift-2', serviceName: 'payments-api', environment: 'production', metricName: 'error_rate', expectedValue: 0.08, actualValue: 0.34, deviationPercent: 325, severity: 'Critical', detectedAt: hoursAgo(2), acknowledgedAt: null },
        { id: 'drift-3', serviceName: 'inventory-graphql', environment: 'production', metricName: 'queue_depth', expectedValue: 120, actualValue: 540, deviationPercent: 350, severity: 'Medium', detectedAt: hoursAgo(6), acknowledgedAt: hoursAgo(4) },
      ],
      totalCount: 3, page: 1, pageSize: 20,
    }),
  ),
  http.get(`${API}/runtime/observability`, () =>
    HttpResponse.json({
      serviceName: 'payments-api', environment: 'production', score: 74, grade: 'C', level: 'NeedsAttention',
      breakdown: { latencyScore: 62, errorScore: 58, throughputScore: 90, resourceScore: 71 },
      computedAt: hoursAgo(1),
    }),
  ),
  http.get(`${API}/runtime/timeline`, () =>
    HttpResponse.json({
      serviceName: 'payments-api', environment: 'production',
      points: [
        { releaseId: 'rel-1', releaseName: 'v2.13.0', periodStart: daysAgo(7), periodEnd: daysAgo(4), avgLatencyMs: 178, errorRate: 0.07, requestsPerSecond: 1400, snapshotCount: 720 },
        { releaseId: 'rel-2', releaseName: 'v2.13.5', periodStart: daysAgo(4), periodEnd: daysAgo(1), avgLatencyMs: 192, errorRate: 0.09, requestsPerSecond: 1430, snapshotCount: 720 },
        { releaseId: 'rel-3', releaseName: 'v2.14.0', periodStart: daysAgo(1), periodEnd: hoursAgo(0), avgLatencyMs: 320, errorRate: 0.34, requestsPerSecond: 1450, snapshotCount: 240 },
      ],
    }),
  ),
];
