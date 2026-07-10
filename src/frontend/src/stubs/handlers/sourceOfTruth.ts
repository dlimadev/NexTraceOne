/**
 * Handlers MSW da Fonte de Verdade (source-of-truth) e pesquisa global.
 *
 * A vista SoT de um serviço é alcançável a partir do detalhe do serviço, pelo
 * que é construída a partir das fixtures de catálogo. A de contrato não é
 * atingível (sem contratos no stub) → cai no catch-all.
 */
import { http, HttpResponse } from 'msw';
import { stubServiceDetails, buildFallbackDetail } from '../fixtures/catalog';

const API = '/api/v1';

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
      totalApis: 0,
      totalContracts: 0,
      totalReferences: 0,
      apis: [],
      contracts: [],
      references: [],
      coverage: {
        hasOwner: true,
        hasContracts: false,
        hasDocumentation: true,
        hasRunbook: false,
        hasRecentChangeHistory: false,
        hasDependenciesMapped: false,
        hasEventTopics: false,
      },
    });
  }),
  http.get(`${API}/source-of-truth/search`, () =>
    HttpResponse.json({ services: [], contracts: [], references: [], totalResults: 0 }),
  ),
  http.get(`${API}/source-of-truth/global-search`, () =>
    HttpResponse.json({ items: [], facetCounts: {}, totalResults: 0 }),
  ),
];
