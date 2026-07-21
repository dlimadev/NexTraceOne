/**
 * Handlers MSW complementares — telas de catálogo e contratos que, sem stub
 * dedicado, cairiam no catch-all (`[]`) e apareceriam VAZIAS na apresentação.
 *
 * As páginas em si não crasham (código defensivo), mas um demo precisa de dados
 * realistas. Este ficheiro cobre: Feature Flags, Developer Portal (catálogo,
 * playground, analytics, subscrições), Contract Studio (draft), e endpoints de
 * detalhe de contrato/canónica/CDCT alcançáveis a partir do menu.
 *
 * Registar ANTES do catch-all. Rotas literais antes das paramétricas.
 */
import { http, HttpResponse } from 'msw';
import { stubServices } from '../fixtures/catalog';
import { stubCanonicalEntities, stubRulesetsRaw } from '../fixtures/contracts';

const API = '/api/v1';
const nowIso = () => new Date().toISOString();

/** APIs consumíveis derivadas dos serviços de catálogo (para o Developer Portal). */
const apiServices = stubServices.filter((s) =>
  ['RestApi', 'GraphqlApi'].includes(s.serviceType),
);

const catalogItems = apiServices.map((s) => ({
  apiAssetId: `api-${s.serviceId}`,
  name: `${s.displayName} Contract`,
  apiName: s.displayName,
  displayName: s.displayName,
  description: s.description,
  version: '1.0.0',
  healthStatus: 'Healthy',
  ownerServiceName: s.displayName,
}));

const pagedCatalog = (items: typeof catalogItems) => ({
  items,
  totalCount: items.length,
  page: 1,
  pageSize: 20,
  totalPages: 1,
});

/** Dashboard agregado de feature flags (tabela ctr_feature_flag_records). */
const featureFlags = [
  { id: 'ff-1', serviceId: 'svc-payments-api', serviceName: 'Payments API', flagKey: 'new-checkout-flow', displayName: 'Novo fluxo de checkout', description: 'Ativa o fluxo de checkout redesenhado.', enabled: true, environment: 'production', updatedAt: nowIso(), updatedBy: 'ana.silva@nextraceone.dev' },
  { id: 'ff-2', serviceId: 'svc-payments-api', serviceName: 'Payments API', flagKey: 'instant-refunds', displayName: 'Reembolsos instantâneos', description: 'Processa reembolsos em tempo real.', enabled: false, environment: 'production', updatedAt: nowIso(), updatedBy: 'ana.silva@nextraceone.dev' },
  { id: 'ff-3', serviceId: 'svc-orders-api', serviceName: 'Orders API', flagKey: 'split-shipments', displayName: 'Envios fracionados', description: 'Permite dividir encomendas em múltiplos envios.', enabled: true, environment: 'production', updatedAt: nowIso(), updatedBy: 'joao.pereira@nextraceone.dev' },
  { id: 'ff-4', serviceId: 'svc-inventory-graphql', serviceName: 'Inventory GraphQL', flagKey: 'realtime-stock', displayName: 'Stock em tempo real', description: 'Atualização de stock via subscrição GraphQL.', enabled: false, environment: 'staging', updatedAt: nowIso(), updatedBy: 'joao.pereira@nextraceone.dev' },
];

const featureFlagDashboard = {
  totalFlags: featureFlags.length,
  enabledFlags: featureFlags.filter((f) => f.enabled).length,
  disabledFlags: featureFlags.filter((f) => !f.enabled).length,
  affectedServices: new Set(featureFlags.map((f) => f.serviceId)).size,
  flags: featureFlags,
};

const sampleOpenApi = `openapi: 3.0.3
info:
  title: Payments API
  version: 2.3.0
paths:
  /payments:
    post:
      summary: Cria um pagamento
      responses:
        '201':
          description: Pagamento criado`;

export const catalogContractsExtrasHandlers = [
  // ── Feature Flags ───────────────────────────────────────────────────
  http.get(`${API}/contracts/feature-flags`, () => HttpResponse.json(featureFlagDashboard)),
  http.patch(`${API}/contracts/feature-flags/:flagId`, () => HttpResponse.json({ id: 'ff-1', enabled: false })),

  // ── Developer Portal — catálogo ─────────────────────────────────────
  http.get(`${API}/developerportal/catalog/search`, () => HttpResponse.json(pagedCatalog(catalogItems))),
  http.get(`${API}/developerportal/catalog/my-apis`, () => HttpResponse.json(pagedCatalog(catalogItems.slice(0, 2)))),
  http.get(`${API}/developerportal/catalog/consuming`, () => HttpResponse.json(pagedCatalog(catalogItems.slice(1)))),
  http.get(`${API}/developerportal/catalog/:apiAssetId/health`, () =>
    HttpResponse.json({ status: 'Healthy', score: 96, lastCheckedAt: nowIso() }),
  ),
  http.get(`${API}/developerportal/catalog/:apiAssetId/timeline`, () =>
    HttpResponse.json([
      { id: 'tl-1', title: 'Versão 2.3.0 publicada', description: 'Novos campos de reconciliação.', occurredAt: '2026-03-05T00:00:00.000Z' },
      { id: 'tl-2', title: 'Versão 2.2.0 publicada', description: 'Suporte a reembolsos.', occurredAt: '2026-01-20T00:00:00.000Z' },
    ]),
  ),
  http.get(`${API}/developerportal/catalog/:apiAssetId/contract`, () => HttpResponse.json(sampleOpenApi)),
  // Detalhe do API (rota paramétrica — por último dentro de /catalog).
  http.get(`${API}/developerportal/catalog/:apiAssetId`, ({ params }) => {
    const id = String(params.apiAssetId);
    const found = catalogItems.find((c) => c.apiAssetId === id) ?? catalogItems[0];
    return HttpResponse.json({ ...found });
  }),

  // ── Developer Portal — subscrições, playground, analytics ───────────
  http.get(`${API}/developerportal/subscriptions`, () =>
    HttpResponse.json([
      { id: 'sub-1', apiAssetId: 'api-svc-payments-api', apiName: 'Payments API', subscriberEmail: 'dev@acme.io', consumerServiceName: 'checkout-web', level: 'BreakingChangesOnly', channel: 'Email', isActive: true },
    ]),
  ),
  http.get(`${API}/developerportal/playground/history`, () =>
    HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 }),
  ),
  http.post(`${API}/developerportal/playground/execute`, () =>
    HttpResponse.json({ statusCode: 200, responseStatusCode: 200, responseBody: '{\n  "status": "ok"\n}', durationMs: 142, executedAt: nowIso() }),
  ),
  http.post(`${API}/developerportal/codegen`, () =>
    HttpResponse.json({ content: '// SDK gerado\nexport class PaymentsClient {}', fileName: 'PaymentsClient.ts', language: 'typescript' }),
  ),
  http.get(`${API}/developerportal/analytics`, () =>
    HttpResponse.json({
      totalExecutions: 128, totalSubscriptions: 12, totalSearches: 340, totalApiViews: 512,
      totalPlaygroundExecutions: 128, totalCodeGenerations: 34,
      popularApis: apiServices.map((s, i) => ({ apiAssetId: `api-${s.serviceId}`, count: 40 - i * 7 })),
      topSearches: [{ term: 'payments', count: 88 }, { term: 'orders', count: 54 }, { term: 'inventory', count: 31 }],
    }),
  ),

  // ── Contract Studio — draft ─────────────────────────────────────────
  http.get(`${API}/contracts/drafts/:draftId`, ({ params }) => {
    const id = String(params.draftId);
    return HttpResponse.json({
      id, draftId: id, title: 'Payments API v2 (rascunho)', description: 'Rascunho de revisão do contrato de pagamentos.',
      serviceId: 'svc-payments-api', contractType: 'RestApi', protocol: 'OpenApi',
      specContent: sampleOpenApi, format: 'yaml', proposedVersion: '2.4.0', status: 'Editing',
      author: 'ana.silva@nextraceone.dev', isAiGenerated: false, createdAt: '2026-06-20T00:00:00.000Z',
      lastEditedAt: nowIso(), lastEditedBy: 'ana.silva@nextraceone.dev', examples: [],
    });
  }),

  // ── Pesquisa de contratos ───────────────────────────────────────────
  http.get(`${API}/contracts/search`, () =>
    HttpResponse.json({ items: [], contracts: [], totalCount: 0 }),
  ),

  // ── Promover schema a entidade canónica ─────────────────────────────
  http.post(`${API}/contracts/canonical-entities/promote`, () =>
    HttpResponse.json({ id: 'ce-promoted-1' }, { status: 201 }),
  ),

  // ── Entidades canónicas — detalhe / usos ────────────────────────────
  http.get(`${API}/contracts/canonical-entities/:entityId/usages`, () =>
    HttpResponse.json({
      usages: [
        { usageId: 'u-1', contractVersionId: 'ct-pay-v2', contractTitle: 'Payments API v2', semVer: '2.3.0', usageType: 'Schema', location: '#/components/schemas/Payment' },
      ],
    }),
  ),
  http.get(`${API}/contracts/canonical-entities/:entityId`, ({ params }) => {
    const id = String(params.entityId);
    const found = stubCanonicalEntities.items.find((e) => e.id === id) ?? stubCanonicalEntities.items[0];
    return HttpResponse.json({ ...found });
  }),

  // ── Ruleset Spectral — detalhe / actualização ──────────────────────
  http.get(`${API}/contracts/spectral/rulesets/:rulesetId`, ({ params }) => {
    const id = String(params.rulesetId);
    const found = stubRulesetsRaw.rulesets.find((r) => r.rulesetId === id) ?? stubRulesetsRaw.rulesets[0];
    return HttpResponse.json({ ...found });
  }),
  http.put(`${API}/contracts/spectral/rulesets/:rulesetId`, ({ params }) =>
    HttpResponse.json({ rulesetId: String(params.rulesetId) }),
  ),

  // ── Impacto em cascata de uma entidade canónica ─────────────────────
  http.get(`${API}/contracts/canonical-entities/:entityId/impact/cascade`, ({ params }) =>
    HttpResponse.json({
      entityId: String(params.entityId),
      totalImpactedContracts: 4,
      totalImpactedServices: 3,
      maxDepth: 2,
      nodes: [
        { nodeId: 'ct-pay-v2', label: 'Payments API v2', nodeType: 'Contract', depth: 1, impactSeverity: 'High' },
        { nodeId: 'svc-payments-api', label: 'Payments API', nodeType: 'Service', depth: 1, impactSeverity: 'High' },
        { nodeId: 'svc-orders-api', label: 'Orders API', nodeType: 'Service', depth: 2, impactSeverity: 'Medium' },
      ],
      edges: [
        { source: 'ce-payment', target: 'ct-pay-v2' },
        { source: 'ct-pay-v2', target: 'svc-payments-api' },
        { source: 'svc-payments-api', target: 'svc-orders-api' },
      ],
    }),
  ),

  // ── Benchmark de maturidade (comparação entre serviços) ─────────────
  http.get(`${API}/catalog/maturity/benchmark`, () =>
    HttpResponse.json({
      averageScore: 0.72,
      p50: 0.74,
      p90: 0.91,
      services: apiServices.map((s, i) => ({ serviceId: s.serviceId, displayName: s.displayName, overallScore: 0.9 - i * 0.08, level: 'Managed' })),
    }),
  ),
];
