/**
 * Handlers MSW de runtime/observabilidade consumidos pelas abas do detalhe do
 * serviço (Observabilidade, Fiabilidade & SLOs, Incidentes, Score).
 *
 * Formas objeto com campos deep-acedidos (`.toLocaleString()`, etc.) — o
 * catch-all `[]` não as salva, precisam de handler dedicado.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';

export const runtimeHandlers = [
  // Snapshot de saúde em tempo real (ServiceObservabilityTab)
  http.get(`${API}/runtime/services/:id/health-snapshot`, () =>
    HttpResponse.json({
      requestsPerMinute: 0,
      p95LatencyMs: 0,
      errorRatePercent: 0,
      throughputTrend: 'stable',
      availabilityPercent: 100,
      lastUpdatedAt: new Date().toISOString(),
    }),
  ),

  // Score composto do serviço (ServiceScoreTab) — valores válidos evitam
  // anéis com NaN e a chave crua `serviceDetail.score.trend.undefined`.
  http.get(`${API}/services/:id/score`, () =>
    HttpResponse.json({
      maturityScore: 55,
      maturityLevel: 2,
      maturityLabel: 'Developing',
      sreScore: 68,
      dxScore: 62,
      computedAt: new Date().toISOString(),
      trend: 'stable',
    }),
  ),
];
