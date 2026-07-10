/**
 * Handlers MSW do módulo de contratos.
 *
 * Cobrem os endpoints alcançáveis a partir dos itens de menu (listas, resumos,
 * entidades canónicas, rulesets, publicação, drafts). Os endpoints de DETALHE
 * de um contrato só são atingíveis ao abrir um contrato — e a lista fica vazia
 * no stub — pelo que caem no catch-all sem serem exercitados.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';

export const contractsHandlers = [
  // Listagem e resumo
  http.get(`${API}/contracts/list`, () =>
    HttpResponse.json({ items: [], totalCount: 0 }),
  ),
  http.get(`${API}/contracts/summary`, () =>
    HttpResponse.json({
      totalCount: 0,
      totalVersions: 0,
      distinctContracts: 0,
      byProtocol: [],
      approvedCount: 0,
      lockedCount: 0,
      draftCount: 0,
      inReviewCount: 0,
      deprecatedCount: 0,
    }),
  ),
  http.get(`${API}/contracts/by-service/:serviceId`, () =>
    HttpResponse.json({ items: [], contracts: [], totalCount: 0 }),
  ),

  // Entidades canónicas (nota: chave `total`, não `totalCount`)
  http.get(`${API}/contracts/canonical-entities`, () =>
    HttpResponse.json({ items: [], total: 0 }),
  ),

  // Rulesets Spectral (o cliente lê `r.data.rulesets`)
  http.get(`${API}/contracts/spectral/rulesets`, () =>
    HttpResponse.json({ rulesets: [] }),
  ),

  // Dashboard de saúde dos contratos (topViolations é acedido via .length)
  http.get(`${API}/contracts/health-dashboard`, () =>
    HttpResponse.json({
      totalContractVersions: 0,
      distinctContracts: 0,
      deprecatedVersions: 0,
      filteredCount: 0,
      percentWithExamples: 0,
      percentWithCanonicalEntities: 0,
      topViolations: [],
      healthScore: 100,
    }),
  ),

  // Drafts (Contract Studio)
  http.get(`${API}/contracts/drafts`, () =>
    HttpResponse.json({ items: [], totalCount: 0 }),
  ),

  // Central de publicação
  http.get(`${API}/publication-center`, () =>
    HttpResponse.json({ items: [], totalCount: 0 }),
  ),
];
