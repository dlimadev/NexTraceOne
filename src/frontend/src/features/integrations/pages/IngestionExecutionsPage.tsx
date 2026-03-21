import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Activity, Search, CheckCircle, AlertTriangle, XCircle, RefreshCw,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { integrationsApi } from '../api/integrations';

const resultBadge = (r: string): 'success' | 'warning' | 'danger' => {
  switch (r) {
    case 'Success': return 'success';
    case 'PartialSuccess': return 'warning';
    default: return 'danger';
  }
};

const resultIcon = (r: string) => {
  switch (r) {
    case 'Success': return <CheckCircle size={14} className="text-success" />;
    case 'PartialSuccess': return <AlertTriangle size={14} className="text-warning" />;
    default: return <XCircle size={14} className="text-critical" />;
  }
};

type ConnectorFilter = 'all' | string;
type ResultFilter = 'all' | string;

function formatDuration(ms: number): string {
  const seconds = Math.floor(ms / 1000);
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  const remaining = seconds % 60;
  return remaining > 0 ? `${minutes}m ${remaining}s` : `${minutes}m`;
}

export function IngestionExecutionsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [connectorFilter, setConnectorFilter] = useState<ConnectorFilter>('all');
  const [resultFilter, setResultFilter] = useState<ResultFilter>('all');
  const queryClient = useQueryClient();

  const { data: response, isLoading, isError, refetch } = useQuery({
    queryKey: ['integrations', 'executions'],
    queryFn: () => integrationsApi.listExecutions(),
    staleTime: 30_000,
  });

  const reprocessMutation = useMutation({
    mutationFn: (executionId: string) => integrationsApi.reprocessExecution(executionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['integrations', 'executions'] });
    },
  });

  const executions = response?.executions ?? [];

  const successCount = executions.filter(e => e.result === 'Success').length;
  const partialCount = executions.filter(e => e.result === 'PartialSuccess').length;
  const failedCount = executions.filter(e => e.result === 'Failed').length;

  const connectorNames = Array.from(new Set(executions.map(e => e.connectorName)));

  const filtered = executions.filter(e => {
    if (connectorFilter !== 'all' && e.connectorName !== connectorFilter) return false;
    if (resultFilter !== 'all' && e.result !== resultFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return e.executionId.toLowerCase().includes(q)
        || e.connectorName.toLowerCase().includes(q);
    }
    return true;
  });

  const formatDate = (iso: string | null) => {
    if (!iso) return '\u2014';
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState action={<button onClick={() => refetch()} className="btn btn-sm btn-primary">{t('common.retry')}</button>} />;

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('integrations.executionsTitle')}</h1>
        <p className="text-muted mt-1">{t('integrations.executionsSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('integrations.executionsTitle')} value={response?.totalCount ?? executions.length} icon={<Activity size={20} />} color="text-accent" />
        <StatCard title={t('integrations.success')} value={successCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('integrations.partialSuccess')} value={partialCount} icon={<AlertTriangle size={20} />} color="text-warning" />
        <StatCard title={t('integrations.failed')} value={failedCount} icon={<XCircle size={20} />} color="text-critical" />
      </div>

      {reprocessMutation.isSuccess && (
        <div className="mb-4 px-4 py-2 rounded-md bg-success/15 text-success text-sm flex items-center gap-2">
          <CheckCircle size={14} /> {t('integrations.reprocessQueued')}
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('integrations.search')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        <select
          value={connectorFilter}
          onChange={e => setConnectorFilter(e.target.value)}
          className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
        >
          <option value="all">{t('integrations.connector')}</option>
          {connectorNames.map(cn => (
            <option key={cn} value={cn}>{cn}</option>
          ))}
        </select>
        <select
          value={resultFilter}
          onChange={e => setResultFilter(e.target.value as ResultFilter)}
          className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
        >
          <option value="all">{t('integrations.result')}</option>
          {(['Success', 'PartialSuccess', 'Failed'] as const).map(r => (
            <option key={r} value={r}>{t(`integrations.${r === 'PartialSuccess' ? 'partialSuccess' : r.toLowerCase()}`)}</option>
          ))}
        </select>
      </div>

      {/* Table */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Activity size={16} className="text-accent" />
            {t('integrations.executionsTitle')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="hidden lg:grid grid-cols-10 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
            <span>{t('integrations.executionId')}</span>
            <span>{t('integrations.connector')}</span>
            <span>{t('integrations.startedAt')}</span>
            <span>{t('integrations.finishedAt')}</span>
            <span>{t('integrations.duration')}</span>
            <span>{t('integrations.result')}</span>
            <span className="text-right">{t('integrations.recordsReceived')}</span>
            <span className="text-right">{t('integrations.warnings')}</span>
            <span className="text-right">{t('integrations.errors')}</span>
            <span className="text-right">{t('common.actions')}</span>
          </div>
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('integrations.noExecutions')}</div>
            ) : (
              filtered.map(ex => (
                <div key={ex.executionId} className="grid grid-cols-1 lg:grid-cols-10 gap-2 px-4 py-3 items-center hover:bg-hover transition-colors">
                  <span className="text-xs font-mono text-muted truncate">{ex.executionId}</span>
                  <span className="text-xs text-heading truncate">{ex.connectorName}</span>
                  <span className="text-xs text-muted">{formatDate(ex.startedAt)}</span>
                  <span className="text-xs text-muted">{formatDate(ex.completedAt)}</span>
                  <span className="text-xs font-mono text-muted">{formatDuration(ex.durationMs)}</span>
                  <span className="flex items-center gap-1">
                    {resultIcon(ex.result)}
                    <Badge variant={resultBadge(ex.result)}>{t(`integrations.${ex.result === 'PartialSuccess' ? 'partialSuccess' : ex.result.toLowerCase()}`)}</Badge>
                  </span>
                  <span className="text-xs font-mono text-heading text-right">{ex.recordsProcessed.toLocaleString()}</span>
                  {/* IngestionExecutionDto does not include a warnings field */}
                  <span className="text-xs font-mono text-right text-muted">0</span>
                  <span className={`text-xs font-mono text-right ${ex.recordsFailed > 0 ? 'text-critical' : 'text-muted'}`}>{ex.recordsFailed}</span>
                  <span className="text-right">
                    {ex.result === 'Failed' && (
                      <button
                        onClick={() => reprocessMutation.mutate(ex.executionId)}
                        disabled={reprocessMutation.isPending}
                        className="inline-flex items-center gap-1 px-2 py-1 text-xs rounded bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors disabled:opacity-50"
                      >
                        <RefreshCw size={10} /> {t('integrations.reprocess')}
                      </button>
                    )}
                  </span>
                </div>
              ))
            )}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
