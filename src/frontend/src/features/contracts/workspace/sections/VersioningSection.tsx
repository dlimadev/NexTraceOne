import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { GitCompare, Clock, ArrowRight, AlertTriangle, Plus, Minus, RefreshCw } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { LifecycleBadge } from '../../shared/components';
import { LoadingState, ErrorState } from '../../shared/components/StateIndicators';
import { contractsApi } from '../../api/contracts';

interface VersioningSectionProps {
  apiAssetId: string;
  currentVersionId: string;
  className?: string;
}

/**
 * Secção de versionamento — histórico de versões e diff semântico.
 */
export function VersioningSection({ apiAssetId, currentVersionId, className = '' }: VersioningSectionProps) {
  const { t } = useTranslation();
  const [baseVersionId, setBaseVersionId] = useState('');
  const [targetVersionId, setTargetVersionId] = useState(currentVersionId);

  const historyQuery = useQuery({
    queryKey: ['contract-history', apiAssetId],
    queryFn: () => contractsApi.getHistory(apiAssetId),
    enabled: !!apiAssetId,
  });

  const diffMutation = useMutation({
    mutationFn: () => contractsApi.computeDiff(baseVersionId, targetVersionId),
  });

  const versions = historyQuery.data ?? [];

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Version History */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Clock size={14} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('contracts.versionHistory', 'Version History')}
            </h3>
            <span className="text-xs text-muted">({versions.length})</span>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {historyQuery.isLoading && <LoadingState size="sm" />}
          {historyQuery.isError && <ErrorState onRetry={() => historyQuery.refetch()} />}

          {!historyQuery.isLoading && versions.length === 0 && (
            <EmptyState
              title={t('contracts.noContracts', 'No versions')}
              size="compact"
            />
          )}

          {versions.length > 0 && (
            <div className="divide-y divide-edge">
              {versions.map((v: any) => (
                <div
                  key={v.id}
                  className={`flex items-center gap-3 px-4 py-3 text-xs transition-colors hover:bg-elevated/30
                    ${v.id === currentVersionId ? 'bg-accent/5 border-l-2 border-accent' : ''}`}
                >
                  <span className="font-mono font-medium text-heading w-16">v{v.version}</span>
                  <LifecycleBadge state={v.lifecycleState} />
                  <span className="text-muted flex-1 truncate">
                    {v.protocol} · {v.format}
                  </span>
                  <span className="text-muted tabular-nums">
                    {new Date(v.createdAt).toLocaleDateString()}
                  </span>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>

      {/* Semantic Diff */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <GitCompare size={14} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('contracts.diff.title', 'Semantic Diff')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-3 mb-4">
            <select
              value={baseVersionId}
              onChange={(e) => setBaseVersionId(e.target.value)}
              className="flex-1 text-xs bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
            >
              <option value="">{t('contracts.diff.selectBaseVersion', 'Select Base Version')}</option>
              {versions.map((v: any) => (
                <option key={v.id} value={v.id}>v{v.version} — {v.lifecycleState}</option>
              ))}
            </select>

            <ArrowRight size={14} className="text-muted flex-shrink-0" />

            <select
              value={targetVersionId}
              onChange={(e) => setTargetVersionId(e.target.value)}
              className="flex-1 text-xs bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
            >
              <option value="">{t('contracts.diff.selectTargetVersion', 'Select Target Version')}</option>
              {versions.map((v: any) => (
                <option key={v.id} value={v.id}>v{v.version} — {v.lifecycleState}</option>
              ))}
            </select>

            <button
              onClick={() => diffMutation.mutate()}
              disabled={!baseVersionId || !targetVersionId || diffMutation.isPending}
              className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {diffMutation.isPending ? <RefreshCw size={12} className="animate-spin" /> : <GitCompare size={12} />}
              {t('contracts.diff.computeDiff', 'Compute Diff')}
            </button>
          </div>

          {/* Diff results */}
          {diffMutation.isError && (
            <div className="text-xs text-red-400 mb-2">
              {t('contracts.errors.diffFailed', 'Failed to compute diff')}
            </div>
          )}

          {diffMutation.data && (
            <DiffResults data={diffMutation.data} />
          )}
        </CardBody>
      </Card>
    </div>
  );
}

// ── Diff Results ──────────────────────────────────────────────────────────────

function DiffResults({ data }: { data: any }) {
  const { t } = useTranslation();

  const sections = [
    { key: 'breakingChanges', labelKey: 'contracts.diff.breakingChanges', color: 'text-red-400', Icon: AlertTriangle },
    { key: 'additiveChanges', labelKey: 'contracts.diff.additiveChanges', color: 'text-emerald-400', Icon: Plus },
    { key: 'nonBreakingChanges', labelKey: 'contracts.diff.nonBreakingChanges', color: 'text-blue-400', Icon: Minus },
  ];

  return (
    <div className="space-y-3">
      {/* Summary badges */}
      <div className="flex items-center gap-3">
        <span className={`text-xs font-medium px-2 py-0.5 rounded ${
          data.changeLevel === 'Breaking' ? 'bg-red-900/30 text-red-400' :
          data.changeLevel === 'Additive' ? 'bg-emerald-900/30 text-emerald-400' :
          'bg-blue-900/30 text-blue-400'
        }`}>
          {data.changeLevel}
        </span>
        {data.suggestedSemVer && (
          <span className="text-xs text-muted">
            {t('contracts.diff.suggestedVersion', 'Suggested')}: <span className="font-mono text-heading">{data.suggestedSemVer}</span>
          </span>
        )}
      </div>

      {/* Change lists */}
      {sections.map(({ key, labelKey, color, Icon }) => {
        const changes = data[key];
        if (!changes || changes.length === 0) return null;

        return (
          <div key={key}>
            <h4 className={`text-xs font-medium mb-1.5 flex items-center gap-1.5 ${color}`}>
              <Icon size={12} />
              {t(labelKey)} ({changes.length})
            </h4>
            <ul className="space-y-1">
              {changes.map((change: any, idx: number) => (
                <li key={idx} className="flex items-start gap-2 text-xs">
                  <span className="font-mono text-[10px] text-muted truncate max-w-[200px]">{change.path}</span>
                  <span className="text-body flex-1">{change.description}</span>
                </li>
              ))}
            </ul>
          </div>
        );
      })}

      {!data.breakingChanges?.length && !data.additiveChanges?.length && !data.nonBreakingChanges?.length && (
        <p className="text-xs text-muted">{t('contracts.diff.noChanges', 'No changes detected')}</p>
      )}
    </div>
  );
}
