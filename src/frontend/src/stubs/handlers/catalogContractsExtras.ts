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
/** Gera um draftId único que o GET /contracts/drafts/:draftId resolve para um draft de demo. */
const newDraftId = () => `draft-${Math.random().toString(36).slice(2, 10)}`;

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

  // ── Contract Studio — criação de draft ──────────────────────────────
  // Sem estes handlers, o POST cai no catch-all (`[]`) → draftId indefinido →
  // o editor in-place mostra "Rascunho não encontrado". Devolvem um draftId
  // que o GET /contracts/drafts/:draftId acima resolve para um draft de demo.
  http.post(`${API}/contracts/drafts`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { title?: string };
    return HttpResponse.json(
      { draftId: newDraftId(), title: body.title ?? 'Novo contrato', status: 'Editing', createdAt: nowIso() },
      { status: 201 },
    );
  }),
  http.post(`${API}/contracts/drafts/soap`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { title?: string; serviceName?: string; targetNamespace?: string; soapVersion?: string };
    return HttpResponse.json(
      {
        draftId: newDraftId(), title: body.title ?? 'Novo contrato SOAP', status: 'Editing',
        serviceName: body.serviceName ?? '', targetNamespace: body.targetNamespace ?? '',
        soapVersion: body.soapVersion ?? '1.1', createdAt: nowIso(),
      },
      { status: 201 },
    );
  }),
  http.post(`${API}/contracts/drafts/event`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { title?: string; asyncApiVersion?: string; defaultContentType?: string };
    return HttpResponse.json(
      {
        draftId: newDraftId(), title: body.title ?? 'Novo contrato Event', status: 'Editing',
        asyncApiVersion: body.asyncApiVersion ?? '2.6.0', defaultContentType: body.defaultContentType ?? 'application/json',
        createdAt: nowIso(),
      },
      { status: 201 },
    );
  }),
  http.post(`${API}/contracts/drafts/background-service`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { title?: string; serviceName?: string; category?: string; triggerType?: string };
    return HttpResponse.json(
      {
        draftId: newDraftId(), title: body.title ?? 'Novo Background Service', status: 'Editing',
        serviceName: body.serviceName ?? '', category: body.category ?? 'Job',
        triggerType: body.triggerType ?? 'OnDemand', createdAt: nowIso(),
      },
      { status: 201 },
    );
  }),
  http.post(`${API}/contracts/drafts/ai/generate`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { title?: string };
    return HttpResponse.json(
      { draftId: newDraftId(), title: body.title ?? 'Contrato gerado por IA', status: 'Editing', createdAt: nowIso() },
      { status: 201 },
    );
  }),

  // ── Linting nativo do contrato (validate/spectral) ──────────────────
  http.post(`${API}/contracts/:contractVersionId/validate/spectral`, () =>
    HttpResponse.json({
      issues: [
        { ruleId: 'info-description', ruleName: 'API has a description', severity: 'Info', message: 'Recomenda-se info.description.', path: '$.info.description', source: 'internal' },
      ],
      summary: {
        totalIssues: 1, errorCount: 0, warningCount: 0, infoCount: 1, hintCount: 0, blockedCount: 0,
        isPublishReady: true, isReviewReady: true, sources: ['internal'],
        validatedAt: nowIso(), overallStatus: 'Valid',
      },
    }),
  ),

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
  // Forma alinhada ao backend GetCanonicalEntityImpactCascade.Response:
  // { rootEntityId, rootEntityName, totalContractsAffected, totalUniqueEntitiesInCascade,
  //   cascadeNodes: [{ entityName, depth, affectedContractIds[], children[] }],
  //   maxDepthReached, riskLevel }
  http.get(`${API}/contracts/canonical-entities/:entityId/impact/cascade`, ({ params }) => {
    const rootName = String(params.entityId);
    return HttpResponse.json({
      rootEntityId: rootName,
      rootEntityName: rootName,
      totalContractsAffected: 4,
      totalUniqueEntitiesInCascade: 3,
      maxDepthReached: 2,
      riskLevel: 'Medium',
      cascadeNodes: [
        {
          entityName: rootName,
          depth: 0,
          affectedContractIds: ['ct-pay-v2', 'ct-pay-v1'],
          children: [
            {
              entityName: 'Money',
              depth: 1,
              affectedContractIds: ['ct-pay-v2'],
              children: [
                { entityName: 'Currency', depth: 2, affectedContractIds: ['ct-orders-v1'], children: [] },
              ],
            },
            { entityName: 'Address', depth: 1, affectedContractIds: ['ct-cust-v1'], children: [] },
          ],
        },
      ],
    });
  }),

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
