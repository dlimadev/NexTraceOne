/**
 * Handlers MSW do módulo de contratos.
 *
 * Cobrem os endpoints alcançáveis a partir dos itens de menu (listas, resumos,
 * entidades canónicas, rulesets, publicação, drafts, saúde). Os endpoints de
 * DETALHE de um contrato específico caem no catch-all.
 */
import { http, HttpResponse } from 'msw';
import {
  stubContractList,
  stubContractsSummary,
  stubContractsByService,
  stubCanonicalEntities,
  stubRulesetsRaw,
  stubHealthDashboard,
  stubPublicationCenter,
  buildContractSot,
  buildContractDetail,
  buildContractViolations,
  buildValidationSummary,
  buildContractScorecard,
  buildContractHistory,
  buildContractIntegrity,
  buildContractDeployments,
  buildContractSubscribers,
  buildParsePreview,
} from '../fixtures/contracts';

const API = '/api/v1';

export const contractsHandlers = [
  // Listagem e resumo
  http.get(`${API}/contracts/list`, () => HttpResponse.json(stubContractList)),
  http.get(`${API}/contracts/summary`, () => HttpResponse.json(stubContractsSummary)),
  http.get(`${API}/contracts/by-service/:serviceId`, ({ params }) =>
    HttpResponse.json(
      stubContractsByService[String(params.serviceId)] ?? { items: [], contracts: [], totalCount: 0 },
    ),
  ),

  // Entidades canónicas (nota: chave `total`, não `totalCount`)
  http.get(`${API}/contracts/canonical-entities`, () =>
    HttpResponse.json(stubCanonicalEntities),
  ),

  // Rulesets Spectral (o cliente lê `r.data.rulesets`)
  http.get(`${API}/contracts/spectral/rulesets`, () =>
    HttpResponse.json(stubRulesetsRaw),
  ),

  // Dashboard de saúde dos contratos (topViolations é acedido via .length)
  http.get(`${API}/contracts/health-dashboard`, () =>
    HttpResponse.json(stubHealthDashboard),
  ),

  // Drafts (Contract Studio)
  http.get(`${API}/contracts/drafts`, () =>
    HttpResponse.json({ items: [], totalCount: 0 }),
  ),

  // Central de publicação
  http.get(`${API}/publication-center`, () =>
    HttpResponse.json(stubPublicationCenter),
  ),

  // Vista Source of Truth de um contrato (governance deep-acedido)
  http.get(`${API}/source-of-truth/contracts/:contractVersionId`, ({ params }) =>
    HttpResponse.json(buildContractSot(String(params.contractVersionId))),
  ),

  // Detalhe de uma versão de contrato (workspace `/contracts/:id`).
  // Fornece specContent para evitar crash em useSpecPreview.
  http.get(`${API}/contracts/:contractVersionId/detail`, ({ params }) =>
    HttpResponse.json(buildContractDetail(String(params.contractVersionId))),
  ),

  // Violações — o cliente lê `r.data.violations` (envelope obrigatório).
  http.get(`${API}/contracts/:contractVersionId/violations`, ({ params }) =>
    HttpResponse.json(buildContractViolations(String(params.contractVersionId))),
  ),

  // Resumo de validação (Governança → ValidationSection, `sources` deep-acedido).
  http.get(`${API}/contracts/:contractVersionId/validation-summary`, ({ params }) =>
    HttpResponse.json(buildValidationSummary(String(params.contractVersionId))),
  ),

  // Scorecard técnico (Governança → Scorecard; NaN sem forma dedicada).
  http.get(`${API}/contracts/:contractVersionId/scorecard`, ({ params }) =>
    HttpResponse.json(buildContractScorecard(String(params.contractVersionId))),
  ),

  // Histórico de versões por apiAssetId (Versionamento/Changelog; lê `.data.versions`).
  http.get(`${API}/contracts/history/:apiAssetId`, ({ params }) =>
    HttpResponse.json(buildContractHistory(String(params.apiAssetId))),
  ),

  // Integridade estrutural (Conformidade → validateIntegrity).
  http.get(`${API}/contracts/:contractVersionId/validate`, ({ params }) =>
    HttpResponse.json(buildContractIntegrity(String(params.contractVersionId))),
  ),

  // Deployments de uma versão de contrato.
  http.get(`${API}/contracts/:contractVersionId/deployments`, ({ params }) =>
    HttpResponse.json(buildContractDeployments(String(params.contractVersionId))),
  ),

  // Subscritores formais via Developer Portal (ConsumersSection).
  http.get(`${API}/developerportal/catalog/:apiAssetId/consumers`, ({ params }) =>
    HttpResponse.json(buildContractSubscribers(String(params.apiAssetId))),
  ),

  // Live preview do spec (Contrato → Pré-visualização). POST com { specContent, protocol }.
  http.post(`${API}/contracts/parse-preview`, async ({ request }) => {
    const body = (await request.json().catch(() => ({}))) as { specContent?: string; protocol?: string };
    return HttpResponse.json(buildParsePreview(body.specContent ?? '', body.protocol ?? 'OpenApi'));
  }),
];
