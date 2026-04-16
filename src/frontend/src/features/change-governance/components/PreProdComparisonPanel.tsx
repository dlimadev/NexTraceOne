import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  GitCompare,
  TrendingUp,
  TrendingDown,
  Minus,
  AlertTriangle,
  CheckCircle2,
  Info,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import type { MetricDiff, PreProdComparisonResponse } from '../api/changeIntelligence';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function trendIcon(trend: MetricDiff['trend']) {
  switch (trend) {
    case 'Improved':
      return <TrendingUp size={14} className="text-success" />;
    case 'Degraded':
      return <TrendingDown size={14} className="text-critical" />;
    case 'Stable':
      return <Minus size={14} className="text-muted" />;
    default:
      return <Info size={14} className="text-muted" />;
  }
}

function trendVariant(trend: MetricDiff['trend']): 'success' | 'warning' | 'danger' | 'default' {
  switch (trend) {
    case 'Improved':
      return 'success';
    case 'Degraded':
      return 'danger';
    case 'Stable':
      return 'default';
    default:
      return 'default';
  }
}

function signalVariant(signal: PreProdComparisonResponse['overallSignal']): 'success' | 'warning' | 'danger' {
  switch (signal) {
    case 'Positive':
      return 'success';
    case 'Neutral':
      return 'warning';
    case 'Concerning':
      return 'danger';
  }
}

function signalIcon(signal: PreProdComparisonResponse['overallSignal']) {
  switch (signal) {
    case 'Positive':
      return <CheckCircle2 size={16} className="text-success" />;
    case 'Neutral':
      return <Info size={16} className="text-info" />;
    case 'Concerning':
      return <AlertTriangle size={16} className="text-critical" />;
  }
}

interface MetricRowProps {
  label: string;
  diff: MetricDiff | null;
}

function MetricRow({ label, diff }: MetricRowProps) {
  const { t } = useTranslation();
  if (!diff) return null;
  return (
    <tr className="border-b border-edge last:border-0">
      <td className="px-4 py-3 text-sm text-body font-medium">{label}</td>
      <td className="px-4 py-3 text-sm text-muted font-mono">
        {diff.preProductionValue != null ? diff.preProductionValue.toFixed(2) : '—'}
      </td>
      <td className="px-4 py-3 text-sm text-muted font-mono">
        {diff.productionValue != null ? diff.productionValue.toFixed(2) : '—'}
      </td>
      <td className="px-4 py-3 text-sm text-muted font-mono">
        {diff.relativeChangePercent != null
          ? `${diff.relativeChangePercent > 0 ? '+' : ''}${diff.relativeChangePercent.toFixed(1)}%`
          : '—'}
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5">
          {trendIcon(diff.trend)}
          <Badge variant={trendVariant(diff.trend)}>
            {t(`preProdComparison.trendLabel.${diff.trend}`)}
          </Badge>
        </div>
      </td>
    </tr>
  );
}

interface PreProdComparisonPanelProps {
  initialPreProdId?: string;
  initialProdId?: string;
  availableReleases: Array<{ id: string; apiAssetId: string; version: string; environment: string }>;
}

/**
 * Painel de comparação pré-produção vs produção.
 * Permite seleccionar duas releases e comparar métricas de baseline.
 */
export function PreProdComparisonPanel({
  initialPreProdId = '',
  initialProdId = '',
  availableReleases,
}: PreProdComparisonPanelProps) {
  const { t } = useTranslation();
  const [preProdId, setPreProdId] = useState(initialPreProdId);
  const [prodId, setProdId] = useState(initialProdId);
  const [enabled, setEnabled] = useState(false);

  const comparisonQuery = useQuery({
    queryKey: ['pre-prod-comparison', preProdId, prodId],
    queryFn: () => changeIntelligenceApi.getPreProdComparison(preProdId, prodId),
    enabled: enabled && !!preProdId && !!prodId && preProdId !== prodId,
  });

  const handleCompare = () => {
    setEnabled(true);
    comparisonQuery.refetch();
  };

  const data = comparisonQuery.data;

  return (
    <div className="space-y-4">
      {/* Selector panel */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <GitCompare size={16} className="text-accent" />
            <h2 className="text-sm font-semibold text-heading">
              {t('preProdComparison.title')}
            </h2>
          </div>
          <p className="text-xs text-muted mt-0.5">{t('preProdComparison.subtitle')}</p>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('preProdComparison.preProdRelease')}
              </label>
              <select
                value={preProdId}
                onChange={(e) => { setPreProdId(e.target.value); setEnabled(false); }}
                className={INPUT_CLS}
              >
                <option value="">{t('preProdComparison.selectRelease')}</option>
                {availableReleases.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.apiAssetId} — v{r.version} ({r.environment})
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-body mb-1">
                {t('preProdComparison.prodRelease')}
              </label>
              <select
                value={prodId}
                onChange={(e) => { setProdId(e.target.value); setEnabled(false); }}
                className={INPUT_CLS}
              >
                <option value="">{t('preProdComparison.selectRelease')}</option>
                {availableReleases.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.apiAssetId} — v{r.version} ({r.environment})
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="flex justify-end">
            <Button
              onClick={handleCompare}
              disabled={!preProdId || !prodId || preProdId === prodId}
              loading={comparisonQuery.isFetching}
            >
              {t('preProdComparison.compare')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {/* Results */}
      {comparisonQuery.isLoading && <PageLoadingState />}
      {comparisonQuery.isError && (
        <PageErrorState message={t('preProdComparison.loadFailed')} />
      )}
      {data && (
        <>
          {/* Overall signal */}
          <Card>
            <CardBody>
              <div className="flex items-center gap-3">
                {signalIcon(data.overallSignal)}
                <div>
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-semibold text-heading">
                      {t('preProdComparison.overallSignal')}
                    </p>
                    <Badge variant={signalVariant(data.overallSignal)}>
                      {t(`preProdComparison.signal.${data.overallSignal}`)}
                    </Badge>
                  </div>
                  <p className="text-xs text-muted mt-0.5">{data.overallRationale}</p>
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Metrics table */}
          {data.hasBaselineData ? (
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold text-heading">
                  {t('preProdComparison.metricsTitle')}
                </h3>
              </CardHeader>
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-4 py-3 font-medium text-muted">{t('preProdComparison.metric')}</th>
                      <th className="px-4 py-3 font-medium text-muted">{t('preProdComparison.preProd')}</th>
                      <th className="px-4 py-3 font-medium text-muted">{t('preProdComparison.prod')}</th>
                      <th className="px-4 py-3 font-medium text-muted">{t('preProdComparison.change')}</th>
                      <th className="px-4 py-3 font-medium text-muted">{t('preProdComparison.trend')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    <MetricRow label={t('preProdComparison.errorRate')} diff={data.errorRate} />
                    <MetricRow label={t('preProdComparison.avgLatency')} diff={data.avgLatencyMs} />
                    <MetricRow label={t('preProdComparison.p95Latency')} diff={data.p95LatencyMs} />
                    <MetricRow label={t('preProdComparison.p99Latency')} diff={data.p99LatencyMs} />
                    <MetricRow label={t('preProdComparison.rpm')} diff={data.requestsPerMinute} />
                    <MetricRow label={t('preProdComparison.throughput')} diff={data.throughput} />
                  </tbody>
                </table>
              </div>
            </Card>
          ) : (
            <Card>
              <CardBody>
                <p className="text-sm text-muted py-4 text-center">
                  {t('preProdComparison.noBaseline')}
                </p>
              </CardBody>
            </Card>
          )}
        </>
      )}

      {!data && !comparisonQuery.isLoading && (
        <Card>
          <CardBody>
            <p className="text-sm text-muted py-12 text-center">
              {t('preProdComparison.prompt')}
            </p>
          </CardBody>
        </Card>
      )}
    </div>
  );
}
