/**
 * ContractHealthTimelinePage — linha do tempo de saúde do contrato por versão.
 * Mostra a evolução do health score ao longo das versões, com correlação de breaking changes.
 * Pilar: Contract Governance + Source of Truth.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, AlertTriangle, Zap, Loader2 } from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { stateToVariant } from '../lib/contractVariants';
import { contractsApi } from '../api/contracts';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField } from '../../../shared/ui';
import { HealthTrendSparkline } from './HealthTrendSparkline';

type HealthTimelinePoint = {
  semVer: string;
  healthScore: number;
  createdAt: string;
  lifecycleState: string;
  isBreakingChange: boolean;
};

type HealthTimelineResponse = {
  apiAssetId: string;
  points: HealthTimelinePoint[];
};

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function ScoreBar({ score }: { score: number }) {
  const color = score >= 80 ? 'bg-success' : score >= 60 ? 'bg-warning' : 'bg-critical';
  const textColor = score >= 80 ? 'text-success' : score >= 60 ? 'text-warning' : 'text-critical';
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-1.5 bg-elevated rounded-full overflow-hidden">
        <div className={`h-full rounded-full transition-all ${color}`} style={{ width: `${score}%` }} />
      </div>
      <span className={`text-sm font-semibold tabular-nums w-8 text-right ${textColor}`}>{score}</span>
    </div>
  );
}

/**
 * Página de linha do tempo de health score do contrato.
 */
export function ContractHealthTimelinePage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const initialAssetId = searchParams.get('apiAssetId') ?? '';
  const [apiAssetId, setApiAssetId] = useState(initialAssetId);
  const [submittedId, setSubmittedId] = useState(initialAssetId);
  const [validationError, setValidationError] = useState('');

  const { data, isLoading, isError, refetch } = useQuery<HealthTimelineResponse>({
    queryKey: ['contract-health-timeline', submittedId],
    queryFn: () => contractsApi.getContractHealthTimeline(submittedId) as Promise<HealthTimelineResponse>,
    enabled: !!submittedId,
    staleTime: 30_000,
  });

  const handleAnalyze = () => {
    const trimmed = apiAssetId.trim();
    if (!trimmed) return;
    if (UUID_REGEX.test(trimmed)) {
      setValidationError('');
      setSubmittedId(trimmed);
    } else {
      // Accept non-UUID values as search terms (asset names)
      setValidationError('');
      setSubmittedId(trimmed);
    }
  };

  const points = data?.points ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('phase4.healthTimeline.title', 'Health Score Timeline')}
        subtitle={t('phase4.healthTimeline.subtitle', 'Evolution of contract health over time')}
        icon={<TrendingUp />}
      />

      {/* Controls */}
      <div className="bg-elevated rounded-lg border border-edge p-4">
        <div className="flex gap-3 flex-wrap">
          <div className="flex-1 min-w-[260px]">
            <TextField
              label={t('phase4.healthTimeline.apiAssetIdLabel', 'API Asset ID')}
              size="sm"
              value={apiAssetId}
              onChange={(e) => { setApiAssetId(e.target.value); setValidationError(''); }}
              placeholder={t('phase4.healthTimeline.apiAssetIdPlaceholder', 'Search or enter API asset name')}
              error={validationError || undefined}
            />
          </div>
          <div className="flex items-end">
            <Button
              variant="primary"
              onClick={handleAnalyze}
              disabled={!apiAssetId.trim()}
              loading={isLoading}
            >
              {t('phase4.healthTimeline.loadTimeline', 'Load Timeline')}
            </Button>
          </div>
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 size={24} className="animate-spin text-accent mr-2" />
          <span className="text-muted text-sm">{t('common.loading', 'Loading...')}</span>
        </div>
      )}

      {/* Error */}
      {isError && (
        <div className="bg-critical/10 border border-critical/25 rounded-lg p-4 flex items-center gap-2 text-critical">
          <AlertTriangle size={16} />
          <span className="text-sm">{t('phase4.healthTimeline.loadError', 'Failed to load health timeline. Please verify the API Asset ID.')}</span>
          <Button variant="ghost" size="xs" onClick={() => refetch()} className="ml-auto">{t('common.retry', 'Retry')}</Button>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && !isError && submittedId && !data && (
        <div className="bg-elevated rounded-lg border border-edge p-8 text-center text-muted text-sm">
          {t('common.noData', 'No data available')}
        </div>
      )}

      {/* Results */}
      {data && (
        <div className="space-y-4">
          {points.length >= 2 && <HealthTrendSparkline points={points} />}
          <div className="bg-elevated rounded-lg border border-edge overflow-hidden">
          {points.length === 0 ? (
            <div className="p-8 text-center text-muted text-sm">
              {t('phase4.healthTimeline.noData', 'No versions found for this contract')}
            </div>
          ) : (
            <div className="divide-y divide-edge">
              {/* Table header */}
              <div className="grid grid-cols-12 gap-3 px-4 py-2 text-xs font-medium text-muted uppercase tracking-wide bg-panel/50">
                <div className="col-span-2">{t('phase4.healthTimeline.version', 'Version')}</div>
                <div className="col-span-4">{t('phase4.healthTimeline.healthScore', 'Health Score')}</div>
                <div className="col-span-2">{t('phase4.healthTimeline.lifecycleState', 'State')}</div>
                <div className="col-span-3">{t('phase4.healthTimeline.createdAt', 'Created At')}</div>
                <div className="col-span-1"></div>
              </div>

              {/* Rows */}
              {points.map((point) => (
                <div
                  key={point.semVer}
                  className="grid grid-cols-12 gap-3 px-4 py-3 items-center hover:bg-elevated/60 transition-colors"
                >
                  <div className="col-span-2 text-sm font-mono text-heading">{point.semVer}</div>
                  <div className="col-span-4">
                    <ScoreBar score={Math.round(point.healthScore)} />
                  </div>
                  <div className="col-span-2">
                    <Badge variant={stateToVariant(point.lifecycleState)} size="xs">
                      {point.lifecycleState}
                    </Badge>
                  </div>
                  <div className="col-span-3 text-xs text-muted">
                    {new Date(point.createdAt).toLocaleDateString()}
                  </div>
                  <div className="col-span-1 flex justify-end">
                    {point.isBreakingChange && (
                      <Badge variant="danger" size="xs" icon={<Zap size={10} />}>
                        {t('phase4.healthTimeline.breakingChange', 'Breaking')}
                      </Badge>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
          </div>
        </div>
      )}
    </PageContainer>
  );
}
