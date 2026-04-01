import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { GitCompare, Clock, ArrowRight, AlertTriangle, Plus, Minus, RefreshCw, Info } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { LifecycleBadge } from '../../shared/components';
import { LoadingState, ErrorState } from '../../shared/components/StateIndicators';
import { contractsApi } from '../../api/contracts';
import type { ContractVersion, SemanticDiff, ChangeEntry, ContractProtocol } from '../../../../types';

interface VersioningSectionProps {
  apiAssetId: string;
  currentVersionId: string;
  className?: string;
}

/**
 * Etiqueta e descrição do que é comparado no diff por protocolo.
 */
function getDiffProtocolHint(protocol: ContractProtocol | string): string {
  switch (protocol) {
    case 'WorkerService': return 'Compares trigger type, schedule, inputs, outputs and side effects';
    case 'Wsdl': return 'Compares WSDL portTypes, operations and message parts';
    case 'AsyncApi': return 'Compares channels, operations and message schemas';
    case 'Swagger':
    case 'OpenApi': return 'Compares REST paths, methods, parameters and schemas';
    default: return 'Semantic diff between contract versions';
  }
}

/**
 * Secção de versionamento — histórico de versões e diff semântico.
 * Suporta todos os protocolos: OpenAPI, Swagger, WSDL, AsyncAPI e WorkerService.
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
  // Derive protocol from selected target version for the diff hint
  const targetVersion = versions.find((v: ContractVersion) => v.id === targetVersionId);
  const diffProtocolHint = getDiffProtocolHint(targetVersion?.protocol ?? '');

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
              {versions.map((v: ContractVersion) => (
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
            {targetVersion && (
              <span className="text-[10px] text-muted flex items-center gap-1" title={diffProtocolHint}>
                <Info size={10} />
                {targetVersion.protocol}
              </span>
            )}
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
              {versions.map((v: ContractVersion) => (
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
              {versions.map((v: ContractVersion) => (
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
            <div className="text-xs text-critical mb-2">
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

function DiffResults({ data }: { data: SemanticDiff }) {
  const { t } = useTranslation();

  const breakingChanges = data.changes.filter((change) => change.isBreaking);
  const additiveChanges = data.changes.filter((change) => !change.isBreaking && change.changeType === 'Added');
  const nonBreakingChanges = data.changes.filter((change) => !change.isBreaking && change.changeType !== 'Added');
  const changeLevel = data.isBreaking ? 'Breaking' : additiveChanges.length > 0 ? 'Additive' : 'NonBreaking';

  const sections: Array<{
    key: 'breakingChanges' | 'additiveChanges' | 'nonBreakingChanges';
    labelKey: string;
    color: string;
    Icon: typeof AlertTriangle;
    changes: ChangeEntry[];
  }> = [
    { key: 'breakingChanges', labelKey: 'contracts.diff.breakingChanges', color: 'text-critical', Icon: AlertTriangle, changes: breakingChanges },
    { key: 'additiveChanges', labelKey: 'contracts.diff.additiveChanges', color: 'text-success', Icon: Plus, changes: additiveChanges },
    { key: 'nonBreakingChanges', labelKey: 'contracts.diff.nonBreakingChanges', color: 'text-info', Icon: Minus, changes: nonBreakingChanges },
  ];

  return (
    <div className="space-y-3">
      {/* Summary badges */}
      <div className="flex items-center gap-3">
        <span className={`text-xs font-medium px-2 py-0.5 rounded ${
          changeLevel === 'Breaking' ? 'bg-critical/15 text-critical' :
          changeLevel === 'Additive' ? 'bg-success/15 text-success' :
          'bg-info/15 text-info'
        }`}>
          {changeLevel}
        </span>
        {data.suggestedVersion && (
          <span className="text-xs text-muted">
            {t('contracts.diff.suggestedVersion', 'Suggested')}: <span className="font-mono text-heading">{data.suggestedVersion}</span>
          </span>
        )}
      </div>

      {/* Change lists */}
      {sections.map(({ key, labelKey, color, Icon, changes }) => {
        if (changes.length === 0) return null;

        return (
          <div key={key}>
            <h4 className={`text-xs font-medium mb-1.5 flex items-center gap-1.5 ${color}`}>
              <Icon size={12} />
              {t(labelKey)} ({changes.length})
            </h4>
            <ul className="space-y-1">
              {changes.map((change: ChangeEntry, idx: number) => (
                <li key={idx} className="flex items-start gap-2 text-xs">
                  <span className="font-mono text-[10px] text-muted truncate max-w-[200px]">{change.path}</span>
                  <span className="text-body flex-1">{change.description}</span>
                </li>
              ))}
            </ul>
          </div>
        );
      })}

      {data.changes.length === 0 && (
        <p className="text-xs text-muted">{t('contracts.diff.noChanges', 'No changes detected')}</p>
      )}
    </div>
  );
}
