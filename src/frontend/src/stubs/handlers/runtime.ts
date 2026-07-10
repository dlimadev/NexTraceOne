/**
 * Handlers MSW de runtime/observabilidade consumidos pelas abas do detalhe do
 * serviço (Observabilidade, Fiabilidade & SLOs, Incidentes, Score).
 *
 * Formas objeto com campos deep-acedidos (`.toLocaleString()`, etc.) — o
 * catch-all `[]` não as salva, precisam de handler dedicado.
 */
import { http, HttpResponse } from 'msw';

const API = '/api/v1';

/** Métricas de runtime por serviço (Observabilidade). */
const snapshotByService: Record<string, { rpm: number; p95: number; err: number; trend: 'up' | 'down' | 'stable'; avail: number }> = {
  'svc-payments-api': { rpm: 8420, p95: 142, err: 0.12, trend: 'up', avail: 99.98 },
  'svc-orders-api': { rpm: 3150, p95: 98, err: 0.05, trend: 'stable', avail: 99.99 },
  'svc-notifications-worker': { rpm: 1240, p95: 210, err: 0.30, trend: 'down', avail: 99.90 },
  'svc-inventory-graphql': { rpm: 2075, p95: 176, err: 0.18, trend: 'stable', avail: 99.95 },
  'svc-legacy-billing': { rpm: 320, p95: 540, err: 1.20, trend: 'down', avail: 99.20 },
};

export const runtimeHandlers = [
  // Snapshot de saúde em tempo real (ServiceObservabilityTab)
  http.get(`${API}/runtime/services/:id/health-snapshot`, ({ params }) => {
    const m = snapshotByService[String(params.id)] ?? { rpm: 1000, p95: 150, err: 0.1, trend: 'stable' as const, avail: 99.95 };
    return HttpResponse.json({
      requestsPerMinute: m.rpm,
      p95LatencyMs: m.p95,
      errorRatePercent: m.err,
      throughputTrend: m.trend,
      availabilityPercent: m.avail,
      lastUpdatedAt: new Date().toISOString(),
    });
  }),

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
