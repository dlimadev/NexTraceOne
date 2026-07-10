/**
 * Handlers MSW da Fonte de Verdade (source-of-truth) e pesquisa global.
 *
 * A vista SoT de um serviço é alcançável a partir do detalhe do serviço, pelo
 * que é construída a partir das fixtures de catálogo. A de contrato não é
 * atingível (sem contratos no stub) → cai no catch-all.
 */
import { http, HttpResponse } from 'msw';
import { stubServiceDetails, buildFallbackDetail, stubServices } from '../fixtures/catalog';

const API = '/api/v1';

/** Itens da busca global (⌘K e /search) derivados dos serviços. */
const globalSearchItems = stubServices.map((s, i) => ({
  entityId: s.serviceId,
  entityType: 'Service',
  title: s.displayName,
  subtitle: `${s.domain} · ${s.teamName}`,
  owner: s.technicalOwner,
  status: s.lifecycleStatus,
  route: `/services/${s.serviceId}`,
  relevanceScore: 1 - i * 0.1,
}));

export const sourceOfTruthHandlers = [
  http.get(`${API}/source-of-truth/services/:serviceId/coverage`, () =>
    HttpResponse.json({ metIndicators: 4, totalIndicators: 7, coverageScore: 57 }),
  ),
  http.get(`${API}/source-of-truth/services/:serviceId`, ({ params }) => {
    const id = String(params.serviceId);
    const svc = stubServiceDetails[id] ?? buildFallbackDetail(id);
    return HttpResponse.json({
      serviceId: svc.serviceId,
      name: svc.name,
      displayName: svc.displayName,
      description: svc.description,
      domain: svc.domain,
      systemArea: svc.systemArea,
      serviceType: svc.serviceType,
      teamName: svc.teamName,
      criticality: svc.criticality,
      lifecycleStatus: svc.lifecycleStatus,
      exposureType: svc.exposureType,
      technicalOwner: svc.technicalOwner,
      businessOwner: svc.businessOwner,
      documentationUrl: svc.documentationUrl,
      repositoryUrl: svc.repositoryUrl,
      totalApis: svc.apis.length,
      totalContracts: svc.apis.length > 0 ? 1 : 0,
      totalReferences: 3,
      apis: svc.apis,
      contracts: [],
      references: [
        { referenceId: 'ref-docs', title: 'Documentação', description: 'Documentação do serviço', assetType: 'Documentation', referenceType: 'Documentation', url: svc.documentationUrl },
        { referenceId: 'ref-repo', title: 'Repositório', description: 'Código-fonte', assetType: 'Repository', referenceType: 'Repository', url: svc.repositoryUrl },
        { referenceId: 'ref-ci', title: 'Pipeline CI', description: 'Pipeline de integração contínua', assetType: 'Pipeline', referenceType: 'Pipeline', url: svc.ciPipelineUrl ?? '' },
      ],
      coverage: {
        hasOwner: true,
        hasContracts: svc.apis.length > 0,
        hasDocumentation: true,
        hasRunbook: false,
        hasRecentChangeHistory: true,
        hasDependenciesMapped: svc.apis.length > 0,
        hasEventTopics: (svc.interfaces ?? []).some((i) => i.interfaceType.includes('Kafka')),
      },
    });
  }),
  http.get(`${API}/source-of-truth/search`, () =>
    HttpResponse.json({
      services: Object.values(stubServiceDetails),
      contracts: [],
      references: [],
      totalResults: stubServices.length,
    }),
  ),
  http.get(`${API}/source-of-truth/global-search`, () =>
    HttpResponse.json({
      items: globalSearchItems,
      facetCounts: { Service: stubServices.length },
      totalResults: stubServices.length,
    }),
  ),
];
