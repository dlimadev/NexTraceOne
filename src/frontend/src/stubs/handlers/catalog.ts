/**
 * Handlers MSW do catálogo de serviços.
 *
 * Cobrem a jornada principal: listagem, resumo, detalhe e registo.
 * NOTA de ordem: rotas específicas (summary, search) têm de vir ANTES da
 * rota paramétrica /catalog/services/:id, senão o :id captura "summary".
 */
import { http, HttpResponse } from 'msw';
import {
  stubServiceList,
  stubServicesSummary,
  stubServiceDetails,
  buildFallbackDetail,
} from '../fixtures/catalog';

const API = '/api/v1';

export const catalogHandlers = [
  // Rotas específicas primeiro
  http.get(`${API}/catalog/services/summary`, () =>
    HttpResponse.json(stubServicesSummary),
  ),
  http.get(`${API}/catalog/services/search`, () =>
    HttpResponse.json(stubServiceList.items),
  ),

  // Listagem
  http.get(`${API}/catalog/services`, () =>
    HttpResponse.json(stubServiceList),
  ),

  // Registo — devolve um ID determinístico a partir do nome do serviço
  http.post(`${API}/catalog/services`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { name?: string };
    const slug = (body.name ?? 'new-service').toString().trim().toLowerCase().replace(/\s+/g, '-');
    return HttpResponse.json({ id: `svc-${slug}` }, { status: 201 });
  }),

  // ── Sub-recursos de um serviço ──────────────────────────────────
  // Interfaces e bindings devolvem ARRAY (o consumidor faz .map/.length).
  http.get(`${API}/catalog/services/:id/interfaces`, () => HttpResponse.json([])),
  http.get(`${API}/catalog/interfaces/:id`, ({ params }) =>
    HttpResponse.json({ interfaceId: String(params.id), name: '', serviceAssetId: '' }),
  ),
  http.get(`${API}/catalog/interfaces/:id/bindings`, () => HttpResponse.json([])),
  http.get(`${API}/catalog/services/:id/links`, () =>
    HttpResponse.json({ items: [], totalCount: 0 }),
  ),

  // ── Maturidade / auditoria de ownership ─────────────────────────
  http.get(`${API}/catalog/services/:id/maturity`, ({ params }) =>
    HttpResponse.json({
      serviceId: String(params.id),
      serviceName: String(params.id),
      displayName: String(params.id),
      teamName: '',
      domain: '',
      level: 'Initial',
      overallScore: 0,
      dimensions: [],
      computedAt: new Date().toISOString(),
    }),
  ),
  http.get(`${API}/catalog/maturity/dashboard`, () =>
    HttpResponse.json({
      summary: {
        totalServices: 0, averageScore: 0, optimizing: 0, managed: 0, defined: 0,
        developing: 0, initial: 0, withoutOwnership: 0, withoutContracts: 0,
        withoutDocumentation: 0, withoutRunbooks: 0, withoutMonitoring: 0,
      },
      services: [],
      computedAt: new Date().toISOString(),
    }),
  ),
  http.get(`${API}/catalog/ownership/audit`, () =>
    HttpResponse.json({
      summary: {
        totalServicesAudited: 0, servicesWithIssues: 0, healthyServices: 0,
        criticalFindings: 0, highFindings: 0, mediumFindings: 0, withoutTeam: 0,
        withoutTechnicalOwner: 0, withoutDocumentation: 0, withoutRunbook: 0,
        apisWithoutContracts: 0,
      },
      findings: [],
      auditedAt: new Date().toISOString(),
    }),
  ),

  // ── Discovery ───────────────────────────────────────────────────
  http.get(`${API}/catalog/discovery/services`, () =>
    HttpResponse.json({ items: [], totalCount: 0 }),
  ),
  http.get(`${API}/catalog/discovery/dashboard`, () =>
    HttpResponse.json({
      totalDiscovered: 0, pending: 0, matched: 0, registered: 0, ignored: 0,
      newThisWeek: 0, recentRuns: [],
    }),
  ),

  // ── Grafo de dependências ───────────────────────────────────────
  http.get(`${API}/catalog/graph`, () => HttpResponse.json({ services: [], apis: [] })),
  http.get(`${API}/catalog/apis/search`, () => HttpResponse.json([])),
  http.get(`${API}/catalog/health`, () => HttpResponse.json({ nodes: [] })),
  http.get(`${API}/catalog/snapshots`, () => HttpResponse.json({ items: [] })),

  // Detalhe (rota paramétrica — por último)
  http.get(`${API}/catalog/services/:id`, ({ params }) => {
    const id = String(params.id);
    return HttpResponse.json(stubServiceDetails[id] ?? buildFallbackDetail(id));
  }),
];
