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
    incidentType: 'Performance', severity: 'Critical', status: 'Investigating', serviceId: 'svc-payments-api',
    serviceDisplayName: 'Payments API', ownerTeam: 'Payments', environment: 'production', createdAt: hoursAgo(3),
    hasCorrelatedChanges: true, correlationConfidence: 'High', mitigationStatus: 'Available',
  },
  {
    incidentId: 'inc-2', reference: 'INC-2040', title: 'Erros 500 esporádicos no checkout',
    incidentType: 'Availability', severity: 'Major', status: 'Mitigating', serviceId: 'svc-orders-api',
    serviceDisplayName: 'Orders API', ownerTeam: 'Orders', environment: 'production', createdAt: hoursAgo(8),
    hasCorrelatedChanges: true, correlationConfidence: 'Medium', mitigationStatus: 'InProgress',
  },
  {
    incidentId: 'inc-3', reference: 'INC-2039', title: 'Fila de eventos de inventário acumulada',
    incidentType: 'Performance', severity: 'Minor', status: 'Monitoring', serviceId: 'svc-inventory-graphql',
    serviceDisplayName: 'Inventory GraphQL', ownerTeam: 'Inventory', environment: 'staging', createdAt: daysAgo(1),
    hasCorrelatedChanges: false, correlationConfidence: 'Low', mitigationStatus: 'NotAvailable',
  },
  {
    incidentId: 'inc-4', reference: 'INC-2035', title: 'Timeout no gateway de notificações',
    incidentType: 'Availability', severity: 'Warning', status: 'Resolved', serviceId: 'svc-notifications-worker',
    serviceDisplayName: 'Notifications Worker', ownerTeam: 'Platform', environment: 'production', createdAt: daysAgo(3),
    hasCorrelatedChanges: false, correlationConfidence: 'Low', mitigationStatus: 'Completed',
  },
];

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
];
