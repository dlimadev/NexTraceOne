/**
 * Handler catch-all — apanha qualquer chamada /api/v1 ainda não stubada.
 *
 * Objetivo: garantir que a app NUNCA bate na rede real em modo stub e que
 * endpoints por cobrir não deixam a promessa pendente/erro. Devolve um objeto
 * vazio (200) e regista um aviso na consola para sabermos o que falta stubar.
 *
 * Registar SEMPRE por último no array de handlers.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';

export const catchAllHandlers = [
  http.all(`${API}/*`, ({ request }) => {
    const { method } = request;
    const path = new URL(request.url).pathname;
    console.warn(`[stub] endpoint não coberto: ${method} ${path} → {} vazio. Adicione um handler em src/stubs/handlers.`);
    return HttpResponse.json({});
  }),
];
