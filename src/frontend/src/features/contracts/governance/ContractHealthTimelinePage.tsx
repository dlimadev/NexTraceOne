/**
 * ContractHealthTimelinePage — linha do tempo de saúde do contrato por versão.
 * Mostra a evolução do health score ao longo das versões, com correlação de breaking changes.
 * Pilar: Contract Governance + Source of Truth.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, AlertTriangle, Zap, Loader2 } from 'lucide-react';
import { contractsApi } from '../api/contracts';

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
  const color = score >= 80 ? 'bg-green-500' : score >= 60 ? 'bg-yellow-500' : 'bg-red-500';
  const textColor = score >= 80 ? 'text-green-400' : score >= 60 ? 'text-yellow-400' : 'text-red-400';
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-1.5 bg-slate-700 rounded-full overflow-hidden">
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
  const [apiAssetId, setApiAssetId] = useState('');
  const [submittedId, setSubmittedId] = useState('');
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold text-white flex items-center gap-2">
          <TrendingUp size={24} className="text-blue-400" />
          {t('phase4.healthTimeline.title', 'Health Score Timeline')}
        </h1>
        <p className="text-slate-400 text-sm mt-1">
          {t('phase4.healthTimeline.subtitle', 'Evolution of contract health over time')}
        </p>
      </div>

      {/* Controls */}
      <div className="bg-slate-800 rounded-lg border border-slate-700 p-4">
        <div className="flex gap-3 flex-wrap">
          <div className="flex-1 min-w-[260px]">
            <label className="block text-xs text-slate-400 mb-1 uppercase tracking-wide">
              {t('phase4.healthTimeline.apiAssetIdLabel', 'API Asset ID')}
            </label>
            <input
              type="text"
              value={apiAssetId}
              onChange={(e) => { setApiAssetId(e.target.value); setValidationError(''); }}
              placeholder={t('phase4.healthTimeline.apiAssetIdPlaceholder', 'Search or enter API asset name')}
              className="w-full px-3 py-2 rounded-lg bg-slate-700 border border-slate-600 text-white text-sm placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              aria-label={t('phase4.healthTimeline.apiAssetIdLabel', 'API Asset ID')}
            />
            {validationError && (
              <p className="text-red-400 text-xs mt-1">{validationError}</p>
            )}
          </div>
          <div className="flex items-end">
            <button
              onClick={handleAnalyze}
              disabled={!apiAssetId.trim() || isLoading}
              className="px-4 py-2 text-sm font-medium rounded-lg bg-blue-600 text-white hover:bg-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isLoading ? '...' : t('phase4.healthTimeline.loadTimeline', 'Load Timeline')}
            </button>
          </div>
        </div>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <Loader2 size={24} className="animate-spin text-blue-400 mr-2" />
          <span className="text-slate-400 text-sm">{t('common.loading', 'Loading...')}</span>
        </div>
      )}

      {/* Error */}
      {isError && (
        <div className="bg-red-500/10 border border-red-500/25 rounded-lg p-4 flex items-center gap-2 text-red-400">
          <AlertTriangle size={16} />
          <span className="text-sm">{t('phase4.healthTimeline.loadError', 'Failed to load health timeline. Please verify the API Asset ID.')}</span>
          <button onClick={() => refetch()} className="ml-auto text-xs underline">{t('common.retry', 'Retry')}</button>
        </div>
      )}

      {/* Empty state */}
      {!isLoading && !isError && submittedId && !data && (
        <div className="bg-slate-800 rounded-lg border border-slate-700 p-8 text-center text-slate-400 text-sm">
          {t('common.noData', 'No data available')}
        </div>
      )}

      {/* Results */}
      {data && (
        <div className="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
          {points.length === 0 ? (
            <div className="p-8 text-center text-slate-400 text-sm">
              {t('phase4.healthTimeline.noData', 'No versions found for this contract')}
            </div>
          ) : (
            <div className="divide-y divide-slate-700">
              {/* Table header */}
              <div className="grid grid-cols-12 gap-3 px-4 py-2 text-xs font-medium text-slate-400 uppercase tracking-wide bg-slate-900/50">
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
                  className="grid grid-cols-12 gap-3 px-4 py-3 items-center hover:bg-slate-700/30 transition-colors"
                >
                  <div className="col-span-2 text-sm font-mono text-white">{point.semVer}</div>
                  <div className="col-span-4">
                    <ScoreBar score={Math.round(point.healthScore)} />
                  </div>
                  <div className="col-span-2">
                    <span className={`text-xs px-2 py-0.5 rounded-full border ${
                      point.lifecycleState === 'Published'
                        ? 'bg-green-400/10 text-green-400 border-green-400/25'
                        : point.lifecycleState === 'Deprecated'
                        ? 'bg-orange-400/10 text-orange-400 border-orange-400/25'
                        : 'bg-slate-600/50 text-slate-300 border-slate-600'
                    }`}>
                      {point.lifecycleState}
                    </span>
                  </div>
                  <div className="col-span-3 text-xs text-slate-400">
                    {new Date(point.createdAt).toLocaleDateString()}
                  </div>
                  <div className="col-span-1 flex justify-end">
                    {point.isBreakingChange && (
                      <span className="flex items-center gap-1 text-xs text-red-400 bg-red-400/10 border border-red-400/20 rounded px-1.5 py-0.5">
                        <Zap size={10} />
                        {t('phase4.healthTimeline.breakingChange', 'Breaking')}
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
