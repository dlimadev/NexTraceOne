import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Activity, Search, CheckCircle, AlertTriangle, XCircle, RefreshCw,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';

type ExecutionResult = 'Success' | 'PartialSuccess' | 'Failed';

interface Execution {
  executionId: string;
  connectorName: string;
  connectorId: string;
  startedAt: string;
  finishedAt: string;
  durationMs: number;
  result: ExecutionResult;
  recordsReceived: number;
  recordsNormalized: number;
  warnings: number;
  errors: number;
  correlationId: string;
}

const mockExecutions: Execution[] = [
  { executionId: 'exec-101', connectorName: 'Datadog APM', connectorId: 'conn-001', startedAt: '2024-01-15T10:25:00Z', finishedAt: '2024-01-15T10:30:00Z', durationMs: 300000, result: 'Success', recordsReceived: 1450, recordsNormalized: 1450, warnings: 0, errors: 0, correlationId: 'corr-a1b2c3' },
  { executionId: 'exec-102', connectorName: 'PagerDuty Incidents', connectorId: 'conn-002', startedAt: '2024-01-15T10:20:00Z', finishedAt: '2024-01-15T10:28:00Z', durationMs: 480000, result: 'Success', recordsReceived: 230, recordsNormalized: 230, warnings: 0, errors: 0, correlationId: 'corr-d4e5f6' },
  { executionId: 'exec-103', connectorName: 'GitHub Actions', connectorId: 'conn-003', startedAt: '2024-01-15T10:18:00Z', finishedAt: '2024-01-15T10:25:00Z', durationMs: 420000, result: 'Success', recordsReceived: 870, recordsNormalized: 870, warnings: 0, errors: 0, correlationId: 'corr-g7h8i9' },
  { executionId: 'exec-104', connectorName: 'AWS CloudWatch', connectorId: 'conn-004', startedAt: '2024-01-15T09:30:00Z', finishedAt: '2024-01-15T09:45:00Z', durationMs: 900000, result: 'PartialSuccess', recordsReceived: 3120, recordsNormalized: 2980, warnings: 5, errors: 2, correlationId: 'corr-j1k2l3' },
  { executionId: 'exec-105', connectorName: 'Jira Service Desk', connectorId: 'conn-005', startedAt: '2024-01-15T10:10:00Z', finishedAt: '2024-01-15T10:20:00Z', durationMs: 600000, result: 'Success', recordsReceived: 564, recordsNormalized: 564, warnings: 0, errors: 0, correlationId: 'corr-m4n5o6' },
  { executionId: 'exec-106', connectorName: 'Splunk Logs', connectorId: 'conn-006', startedAt: '2024-01-15T07:50:00Z', finishedAt: '2024-01-15T08:10:00Z', durationMs: 1200000, result: 'PartialSuccess', recordsReceived: 10240, recordsNormalized: 9800, warnings: 12, errors: 4, correlationId: 'corr-p7q8r9' },
  { executionId: 'exec-107', connectorName: 'Prometheus Metrics', connectorId: 'conn-007', startedAt: '2024-01-15T10:24:00Z', finishedAt: '2024-01-15T10:29:00Z', durationMs: 300000, result: 'Success', recordsReceived: 4780, recordsNormalized: 4780, warnings: 0, errors: 0, correlationId: 'corr-s1t2u3' },
  { executionId: 'exec-108', connectorName: 'Confluence Wiki', connectorId: 'conn-008', startedAt: '2024-01-15T06:00:00Z', finishedAt: '2024-01-15T06:02:10Z', durationMs: 130000, result: 'Failed', recordsReceived: 0, recordsNormalized: 0, warnings: 0, errors: 3, correlationId: 'corr-v4w5x6' },
  { executionId: 'exec-109', connectorName: 'OpsGenie Alerts', connectorId: 'conn-009', startedAt: '2024-01-15T05:55:00Z', finishedAt: '2024-01-15T06:00:00Z', durationMs: 300000, result: 'Success', recordsReceived: 89, recordsNormalized: 89, warnings: 0, errors: 0, correlationId: 'corr-y7z8a1' },
  { executionId: 'exec-110', connectorName: 'Confluence Wiki', connectorId: 'conn-008', startedAt: '2024-01-15T05:00:00Z', finishedAt: '2024-01-15T05:01:45Z', durationMs: 105000, result: 'Failed', recordsReceived: 0, recordsNormalized: 0, warnings: 0, errors: 3, correlationId: 'corr-b2c3d4' },
  { executionId: 'exec-111', connectorName: 'Datadog APM', connectorId: 'conn-001', startedAt: '2024-01-15T10:20:00Z', finishedAt: '2024-01-15T10:24:30Z', durationMs: 270000, result: 'Success', recordsReceived: 1380, recordsNormalized: 1380, warnings: 0, errors: 0, correlationId: 'corr-e5f6g7' },
  { executionId: 'exec-112', connectorName: 'AWS CloudWatch', connectorId: 'conn-004', startedAt: '2024-01-15T08:30:00Z', finishedAt: '2024-01-15T08:45:00Z', durationMs: 900000, result: 'Success', recordsReceived: 3050, recordsNormalized: 3050, warnings: 1, errors: 0, correlationId: 'corr-h8i9j1' },
  { executionId: 'exec-113', connectorName: 'Splunk Logs', connectorId: 'conn-006', startedAt: '2024-01-15T06:50:00Z', finishedAt: '2024-01-15T07:10:00Z', durationMs: 1200000, result: 'PartialSuccess', recordsReceived: 9800, recordsNormalized: 9200, warnings: 8, errors: 3, correlationId: 'corr-k2l3m4' },
  { executionId: 'exec-114', connectorName: 'Confluence Wiki', connectorId: 'conn-008', startedAt: '2024-01-15T04:00:00Z', finishedAt: '2024-01-15T04:01:30Z', durationMs: 90000, result: 'Failed', recordsReceived: 0, recordsNormalized: 0, warnings: 0, errors: 2, correlationId: 'corr-n5o6p7' },
  { executionId: 'exec-115', connectorName: 'GitHub Actions', connectorId: 'conn-003', startedAt: '2024-01-15T09:18:00Z', finishedAt: '2024-01-15T09:25:00Z', durationMs: 420000, result: 'Success', recordsReceived: 920, recordsNormalized: 920, warnings: 0, errors: 0, correlationId: 'corr-q8r9s1' },
  { executionId: 'exec-116', connectorName: 'Prometheus Metrics', connectorId: 'conn-007', startedAt: '2024-01-15T10:19:00Z', finishedAt: '2024-01-15T10:24:00Z', durationMs: 300000, result: 'Success', recordsReceived: 4650, recordsNormalized: 4650, warnings: 0, errors: 0, correlationId: 'corr-t2u3v4' },
];

const resultBadge = (r: ExecutionResult): 'success' | 'warning' | 'danger' => {
  switch (r) {
    case 'Success': return 'success';
    case 'PartialSuccess': return 'warning';
    case 'Failed': return 'danger';
  }
};

const resultIcon = (r: ExecutionResult) => {
  switch (r) {
    case 'Success': return <CheckCircle size={14} className="text-success" />;
    case 'PartialSuccess': return <AlertTriangle size={14} className="text-warning" />;
    case 'Failed': return <XCircle size={14} className="text-critical" />;
  }
};

type ConnectorFilter = 'all' | string;
type ResultFilter = 'all' | ExecutionResult;

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
  const [reprocessMessage, setReprocessMessage] = useState<string | null>(null);

  const data = mockExecutions;

  const successCount = data.filter(e => e.result === 'Success').length;
  const partialCount = data.filter(e => e.result === 'PartialSuccess').length;
  const failedCount = data.filter(e => e.result === 'Failed').length;

  const connectorNames = Array.from(new Set(data.map(e => e.connectorName)));

  const filtered = data.filter(e => {
    if (connectorFilter !== 'all' && e.connectorName !== connectorFilter) return false;
    if (resultFilter !== 'all' && e.result !== resultFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return e.executionId.toLowerCase().includes(q)
        || e.connectorName.toLowerCase().includes(q)
        || e.correlationId.toLowerCase().includes(q);
    }
    return true;
  });

  const formatDate = (iso: string) => {
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  const handleReprocess = (executionId: string) => {
    setReprocessMessage(`${t('integrations.reprocessQueued')} (${executionId})`);
    setTimeout(() => setReprocessMessage(null), 3000);
  };

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('integrations.executionsTitle')}</h1>
        <p className="text-muted mt-1">{t('integrations.executionsSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('integrations.executionsTitle')} value={data.length} icon={<Activity size={20} />} color="text-accent" />
        <StatCard title={t('integrations.success')} value={successCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('integrations.partialSuccess')} value={partialCount} icon={<AlertTriangle size={20} />} color="text-warning" />
        <StatCard title={t('integrations.failed')} value={failedCount} icon={<XCircle size={20} />} color="text-critical" />
      </div>

      {reprocessMessage && (
        <div className="mb-4 px-4 py-2 rounded-md bg-success/15 text-success text-sm flex items-center gap-2">
          <CheckCircle size={14} /> {reprocessMessage}
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
          {(['Success', 'PartialSuccess', 'Failed'] as ExecutionResult[]).map(r => (
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
                  <span className="text-xs text-muted">{formatDate(ex.finishedAt)}</span>
                  <span className="text-xs font-mono text-muted">{formatDuration(ex.durationMs)}</span>
                  <span className="flex items-center gap-1">
                    {resultIcon(ex.result)}
                    <Badge variant={resultBadge(ex.result)}>{t(`integrations.${ex.result === 'PartialSuccess' ? 'partialSuccess' : ex.result.toLowerCase()}`)}</Badge>
                  </span>
                  <span className="text-xs font-mono text-heading text-right">{ex.recordsReceived.toLocaleString()}</span>
                  <span className={`text-xs font-mono text-right ${ex.warnings > 0 ? 'text-warning' : 'text-muted'}`}>{ex.warnings}</span>
                  <span className={`text-xs font-mono text-right ${ex.errors > 0 ? 'text-critical' : 'text-muted'}`}>{ex.errors}</span>
                  <span className="text-right">
                    {ex.result === 'Failed' && (
                      <button
                        onClick={() => handleReprocess(ex.executionId)}
                        className="inline-flex items-center gap-1 px-2 py-1 text-xs rounded bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors"
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
