import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, TrendingDown, Minus, AlertTriangle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import type { RiskScoreDataPoint } from '../api/changeIntelligence';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function trendIcon(prev: number | null, curr: number | null) {
  if (prev === null || curr === null) return <Minus size={12} className="text-muted" />;
  if (curr > prev + 0.05) return <TrendingUp size={12} className="text-critical" />;
  if (curr < prev - 0.05) return <TrendingDown size={12} className="text-success" />;
  return <Minus size={12} className="text-muted" />;
}

function scoreBadgeVariant(score: number | null): 'success' | 'warning' | 'danger' | 'default' {
  if (score === null) return 'default';
  if (score < 0.4) return 'success';
  if (score < 0.7) return 'warning';
  return 'danger';
}

function ScoreBar({ score }: { score: number | null }) {
  if (score === null) {
    return <div className="text-xs text-muted">—</div>;
  }
  const pct = Math.round(score * 100);
  const color =
    score < 0.4 ? 'bg-success' : score < 0.7 ? 'bg-warning' : 'bg-critical';
  return (
    <div className="flex items-center gap-2">
      <div className="w-20 bg-surface rounded-full h-1.5">
        <div className={`${color} rounded-full h-1.5 transition-all`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs font-mono text-heading">{score.toFixed(3)}</span>
    </div>
  );
}

/**
 * Painel de Tendência de Risk Score por serviço.
 * Renderiza uma tabela com sparkline de score ao longo das últimas N releases.
 * Gap 12: Risk Score Trend chart per Service (4.11).
 */
export function RiskScoreTrendPanel({ initialServiceName }: { initialServiceName?: string }) {
  const { t } = useTranslation();
  const [serviceName, setServiceName] = useState(initialServiceName ?? '');
  const [environment, setEnvironment] = useState('');
  const [limit, setLimit] = useState(20);
  const [queryKey, setQueryKey] = useState<[string, string, string, number] | null>(null);

  const { data, isLoading, isFetching } = useQuery({
    queryKey: queryKey ?? ['risk-trend-disabled'],
    queryFn: () =>
      changeIntelligenceApi.getRiskScoreTrend(
        queryKey![1],
        queryKey![2] || undefined,
        queryKey![3],
      ),
    enabled: !!queryKey,
    staleTime: 30_000,
  });

  function handleSearch() {
    if (!serviceName.trim()) return;
    setQueryKey(['risk-score-trend', serviceName.trim(), environment.trim(), limit]);
  }

  const dataPoints = data?.dataPoints ?? [];
  const scoredPoints = dataPoints.filter((p) => p.score !== null);
  const avg =
    scoredPoints.length > 0
      ? scoredPoints.reduce((s, p) => s + (p.score ?? 0), 0) / scoredPoints.length
      : null;
  const maxScore = scoredPoints.length > 0 ? Math.max(...scoredPoints.map((p) => p.score!)) : null;
  const minScore = scoredPoints.length > 0 ? Math.min(...scoredPoints.map((p) => p.score!)) : null;

  return (
    <div className="space-y-4">
      {/* Search bar */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">{t('riskTrend.title')}</h3>
        </CardHeader>
        <CardBody>
          <div className="flex flex-wrap gap-3">
            <div className="flex-1 min-w-[200px]">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('riskTrend.serviceLabel')}
              </label>
              <input
                className={INPUT_CLS}
                placeholder={t('riskTrend.servicePlaceholder')}
                value={serviceName}
                onChange={(e) => setServiceName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSearch();
                }}
              />
            </div>
            <div className="w-40">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('riskTrend.envLabel')}
              </label>
              <input
                className={INPUT_CLS}
                placeholder={t('riskTrend.envPlaceholder')}
                value={environment}
                onChange={(e) => setEnvironment(e.target.value)}
              />
            </div>
            <div className="w-24">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('riskTrend.limitLabel')}
              </label>
              <input
                type="number"
                min={5}
                max={100}
                className={INPUT_CLS}
                value={limit}
                onChange={(e) => setLimit(Number(e.target.value))}
              />
            </div>
            <div className="flex items-end">
              <button
                onClick={handleSearch}
                disabled={!serviceName.trim() || isFetching}
                className="px-4 py-2 rounded-md bg-accent text-white text-sm font-medium disabled:opacity-50 hover:bg-accent/90 transition-colors"
              >
                {isFetching ? t('common.loading') : t('common.search')}
              </button>
            </div>
          </div>
        </CardBody>
      </Card>

      {isLoading && <PageLoadingState />}

      {data && (
        <>
          {/* Stats */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('riskTrend.totalReleases')}</p>
                <p className="text-lg font-bold text-heading">{dataPoints.length}</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('riskTrend.avgScore')}</p>
                <p className="text-lg font-bold text-heading">
                  {avg !== null ? avg.toFixed(3) : '—'}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('riskTrend.maxScore')}</p>
                <p className="text-lg font-bold text-heading">
                  {maxScore !== null ? maxScore.toFixed(3) : '—'}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('riskTrend.minScore')}</p>
                <p className="text-lg font-bold text-heading">
                  {minScore !== null ? minScore.toFixed(3) : '—'}
                </p>
              </CardBody>
            </Card>
          </div>

          {/* Visual sparkline chart */}
          {scoredPoints.length > 0 && (
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold text-heading">{t('riskTrend.chartTitle')}</h3>
              </CardHeader>
              <CardBody>
                <div className="flex items-end gap-1 h-24 overflow-x-auto">
                  {dataPoints.map((point, idx) => {
                    const score = point.score ?? 0;
                    const heightPct = maxScore && maxScore > 0 ? Math.round((score / maxScore) * 100) : 0;
                    const barColor =
                      score < 0.4 ? 'bg-success' : score < 0.7 ? 'bg-warning' : 'bg-critical';
                    return (
                      <div
                        key={point.releaseId}
                        className="flex flex-col items-center shrink-0"
                        title={`${point.version} (${point.environment}) — ${point.score?.toFixed(3) ?? 'N/A'}`}
                      >
                        <div className="flex items-end h-20">
                          <div
                            className={`w-4 rounded-t ${barColor} transition-all`}
                            style={{ height: `${Math.max(4, heightPct * 0.8)}%` }}
                          />
                        </div>
                        {idx === 0 || idx === dataPoints.length - 1 ? (
                          <span className="text-[10px] text-muted mt-1 truncate w-8 text-center">
                            {point.version.slice(0, 4)}
                          </span>
                        ) : null}
                      </div>
                    );
                  })}
                </div>
                <div className="flex justify-between mt-1 text-[10px] text-muted">
                  <span>{dataPoints[0] ? new Date(dataPoints[0].createdAt).toLocaleDateString() : ''}</span>
                  <span>{dataPoints[dataPoints.length - 1] ? new Date(dataPoints[dataPoints.length - 1].createdAt).toLocaleDateString() : ''}</span>
                </div>
              </CardBody>
            </Card>
          )}

          {/* Table */}
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('riskTrend.tableTitle')}</h3>
            </CardHeader>
            <CardBody>
              {dataPoints.length === 0 ? (
                <EmptyState title={t('riskTrend.noData')} />
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs font-medium text-muted uppercase tracking-wider">
                        <th className="px-4 py-2">{t('riskTrend.colVersion')}</th>
                        <th className="px-4 py-2">{t('riskTrend.colEnvironment')}</th>
                        <th className="px-4 py-2">{t('riskTrend.colChangeLevel')}</th>
                        <th className="px-4 py-2">{t('riskTrend.colScore')}</th>
                        <th className="px-4 py-2">{t('riskTrend.colTrend')}</th>
                        <th className="px-4 py-2">{t('riskTrend.colDate')}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {dataPoints.map((point, idx) => {
                        const prev =
                          idx > 0 ? dataPoints[idx - 1].score : null;
                        return (
                          <tr
                            key={point.releaseId}
                            className="border-t border-edge hover:bg-surface/50 transition-colors"
                          >
                            <td className="px-4 py-3 font-mono text-xs">{point.version}</td>
                            <td className="px-4 py-3">
                              <Badge variant="default">{point.environment}</Badge>
                            </td>
                            <td className="px-4 py-3 text-xs text-muted">{point.changeLevel}</td>
                            <td className="px-4 py-3">
                              <ScoreBar score={point.score} />
                            </td>
                            <td className="px-4 py-3">{trendIcon(prev, point.score)}</td>
                            <td className="px-4 py-3 text-xs text-muted">
                              {new Date(point.createdAt).toLocaleDateString()}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>
        </>
      )}

      {!data && !isLoading && !queryKey && (
        <EmptyState
          icon={<AlertTriangle size={32} />}
          title={t('riskTrend.emptyTitle')}
          description={t('riskTrend.emptyMessage')}
        />
      )}
    </div>
  );
}
