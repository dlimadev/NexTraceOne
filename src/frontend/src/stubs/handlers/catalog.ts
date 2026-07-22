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
  stubInterfacesByService,
  stubLinksByService,
  stubMaturityByService,
  stubMaturityDashboard,
  stubOwnershipAudit,
  stubDiscoveredServices,
  stubDiscoveryDashboard,
  stubGraph,
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
  http.get(`${API}/catalog/services/:id/interfaces`, ({ params }) =>
    HttpResponse.json(stubInterfacesByService[String(params.id)] ?? []),
  ),
  http.get(`${API}/catalog/interfaces/:id`, ({ params }) => {
    const id = String(params.id);
    const found = Object.values(stubInterfacesByService).flat().find((i) => i.interfaceId === id);
    return HttpResponse.json(found ?? { interfaceId: id, name: '', serviceAssetId: '' });
  }),
  http.get(`${API}/catalog/interfaces/:id/bindings`, ({ params }) => {
    const interfaceId = String(params.id);
    return HttpResponse.json([
      {
        bindingId: 'bind-1', serviceInterfaceId: interfaceId, contractVersionId: 'ct-pay-v2',
        status: 'Active', bindingEnvironment: 'production', isDefaultVersion: true,
        activatedAt: '2026-03-05T00:00:00.000Z', activatedBy: 'ana.silva@nextraceone.dev',
        migrationNotes: 'Vinculação ativa da versão 2.3.0 do contrato Payments.',
      },
      {
        bindingId: 'bind-2', serviceInterfaceId: interfaceId, contractVersionId: 'ct-pay-v1',
        status: 'Deprecated', bindingEnvironment: 'production', isDefaultVersion: false,
        activatedAt: '2026-01-20T00:00:00.000Z', activatedBy: 'ana.silva@nextraceone.dev',
        migrationNotes: 'Versão anterior mantida para consumidores em migração.',
      },
    ]);
  }),
  http.get(`${API}/catalog/services/:id/links`, ({ params }) =>
    HttpResponse.json(stubLinksByService[String(params.id)] ?? { items: [], totalCount: 0 }),
  ),

  // ── Maturidade / auditoria de ownership ─────────────────────────
  http.get(`${API}/catalog/services/:id/maturity`, ({ params }) => {
    const id = String(params.id);
    return HttpResponse.json(stubMaturityByService[id] ?? {
      serviceId: id, serviceName: id, displayName: id, teamName: '', domain: '',
      level: 'Initial', overallScore: 0, dimensions: [], computedAt: new Date().toISOString(),
    });
  }),
  http.get(`${API}/catalog/maturity/dashboard`, () =>
    HttpResponse.json(stubMaturityDashboard),
  ),
  http.get(`${API}/catalog/ownership/audit`, () =>
    HttpResponse.json(stubOwnershipAudit),
  ),

  // ── Discovery ───────────────────────────────────────────────────
  http.get(`${API}/catalog/discovery/services`, () =>
    HttpResponse.json(stubDiscoveredServices),
  ),
  http.get(`${API}/catalog/discovery/dashboard`, () =>
    HttpResponse.json(stubDiscoveryDashboard),
  ),

  // ── Grafo de dependências ───────────────────────────────────────
  http.get(`${API}/catalog/graph`, () => HttpResponse.json(stubGraph)),
  http.get(`${API}/catalog/apis/search`, () => HttpResponse.json([])),
  http.get(`${API}/catalog/health`, () => HttpResponse.json({ nodes: [] })),
  http.get(`${API}/catalog/snapshots`, () => HttpResponse.json({ items: [] })),

  // Detalhe (rota paramétrica — por último)
  http.get(`${API}/catalog/services/:id`, ({ params }) => {
    const id = String(params.id);
    return HttpResponse.json(stubServiceDetails[id] ?? buildFallbackDetail(id));
  }),
];
