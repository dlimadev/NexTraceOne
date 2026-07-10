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
];
