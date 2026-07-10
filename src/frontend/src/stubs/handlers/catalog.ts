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

  // Detalhe (rota paramétrica — por último)
  http.get(`${API}/catalog/services/:id`, ({ params }) => {
    const id = String(params.id);
    return HttpResponse.json(stubServiceDetails[id] ?? buildFallbackDetail(id));
  }),
];
