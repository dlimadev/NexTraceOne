import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import {
  Cable, ArrowLeft, RefreshCw, CheckCircle, AlertTriangle, XCircle, Clock,
  Settings, Activity, Heart, List,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';

type ConnectorStatus = 'Active' | 'Degraded' | 'Failed' | 'Disabled';
type ConnectorHealth = 'Healthy' | 'Degraded' | 'Failed' | 'Stale';
type ExecutionResult = 'Success' | 'PartialSuccess' | 'Failed';

interface ConnectorDetail {
  connectorId: string;
  name: string;
  type: string;
  provider: string;
  status: ConnectorStatus;
  health: ConnectorHealth;
  description: string;
  environment: string;
  lastSuccess: string;
  lastFailure: string | null;
  freshnessLag: string;
  itemsSynced: number;
  endpoint: string;
  authMode: string;
  pollingMode: string;
  retryPolicy: string;
  enabled: boolean;
  allowedDomains: string[];
  sourceScope: string;
  allowedTeams: string[];
  executions: Execution[];
  lastHealthCheck: string;
}

interface Execution {
  executionId: string;
  startedAt: string;
  finishedAt: string;
  result: ExecutionResult;
  recordsProcessed: number;
  warnings: number;
  errors: number;
}

const mockConnectors: Record<string, ConnectorDetail> = {
  'conn-001': {
    connectorId: 'conn-001', name: 'Datadog APM', type: 'APM', provider: 'Datadog',
    status: 'Active', health: 'Healthy', description: 'Imports APM traces, service maps and error rates from Datadog.',
    environment: 'Production', lastSuccess: '2024-01-15T10:30:00Z', lastFailure: null,
    freshnessLag: '2m', itemsSynced: 14520, endpoint: 'https://api.datadoghq.com/v2',
    authMode: 'API Key', pollingMode: 'Scheduled (5min)', retryPolicy: '3 retries / exponential backoff',
    enabled: true, allowedDomains: ['Telemetry', 'Runtime'], sourceScope: 'All services',
    allowedTeams: ['Team Platform', 'Team Commerce'],
    executions: [
      { executionId: 'exec-001a', startedAt: '2024-01-15T10:25:00Z', finishedAt: '2024-01-15T10:30:00Z', result: 'Success', recordsProcessed: 1450, warnings: 0, errors: 0 },
      { executionId: 'exec-001b', startedAt: '2024-01-15T10:20:00Z', finishedAt: '2024-01-15T10:24:30Z', result: 'Success', recordsProcessed: 1380, warnings: 0, errors: 0 },
      { executionId: 'exec-001c', startedAt: '2024-01-15T10:15:00Z', finishedAt: '2024-01-15T10:19:45Z', result: 'Success', recordsProcessed: 1520, warnings: 1, errors: 0 },
      { executionId: 'exec-001d', startedAt: '2024-01-15T10:10:00Z', finishedAt: '2024-01-15T10:14:30Z', result: 'Success', recordsProcessed: 1410, warnings: 0, errors: 0 },
      { executionId: 'exec-001e', startedAt: '2024-01-15T10:05:00Z', finishedAt: '2024-01-15T10:09:50Z', result: 'PartialSuccess', recordsProcessed: 1200, warnings: 3, errors: 1 },
    ],
    lastHealthCheck: '2024-01-15T10:31:00Z',
  },
  'conn-008': {
    connectorId: 'conn-008', name: 'Confluence Wiki', type: 'Wiki', provider: 'Atlassian',
    status: 'Failed', health: 'Failed', description: 'Syncs knowledge base articles and runbooks from Confluence spaces.',
    environment: 'Production', lastSuccess: '2024-01-14T18:00:00Z', lastFailure: '2024-01-15T06:00:00Z',
    freshnessLag: '16h 32m', itemsSynced: 1230, endpoint: 'https://company.atlassian.net/wiki/rest/api',
    authMode: 'OAuth 2.0', pollingMode: 'Scheduled (1h)', retryPolicy: '5 retries / exponential backoff',
    enabled: true, allowedDomains: ['Knowledge'], sourceScope: 'Selected spaces',
    allowedTeams: ['Team Platform'],
    executions: [
      { executionId: 'exec-008a', startedAt: '2024-01-15T06:00:00Z', finishedAt: '2024-01-15T06:02:10Z', result: 'Failed', recordsProcessed: 0, warnings: 0, errors: 3 },
      { executionId: 'exec-008b', startedAt: '2024-01-15T05:00:00Z', finishedAt: '2024-01-15T05:01:45Z', result: 'Failed', recordsProcessed: 0, warnings: 0, errors: 3 },
      { executionId: 'exec-008c', startedAt: '2024-01-15T04:00:00Z', finishedAt: '2024-01-15T04:01:30Z', result: 'Failed', recordsProcessed: 0, warnings: 0, errors: 2 },
      { executionId: 'exec-008d', startedAt: '2024-01-14T18:00:00Z', finishedAt: '2024-01-14T18:05:20Z', result: 'Success', recordsProcessed: 85, warnings: 1, errors: 0 },
      { executionId: 'exec-008e', startedAt: '2024-01-14T17:00:00Z', finishedAt: '2024-01-14T17:04:50Z', result: 'Success', recordsProcessed: 82, warnings: 0, errors: 0 },
    ],
    lastHealthCheck: '2024-01-15T10:31:00Z',
  },
};

const statusBadge = (s: ConnectorStatus): 'success' | 'warning' | 'danger' | 'default' => {
  switch (s) {
    case 'Active': return 'success';
    case 'Degraded': return 'warning';
    case 'Failed': return 'danger';
    case 'Disabled': return 'default';
  }
};

const healthBadge = (h: ConnectorHealth): 'success' | 'warning' | 'danger' | 'info' => {
  switch (h) {
    case 'Healthy': return 'success';
    case 'Degraded': return 'warning';
    case 'Failed': return 'danger';
    case 'Stale': return 'info';
  }
};

const resultBadge = (r: ExecutionResult): 'success' | 'warning' | 'danger' => {
  switch (r) {
    case 'Success': return 'success';
    case 'PartialSuccess': return 'warning';
    case 'Failed': return 'danger';
  }
};

type TabKey = 'overview' | 'configuration' | 'executions' | 'health';

export function ConnectorDetailPage() {
  const { t } = useTranslation();
  const { connectorId } = useParams<{ connectorId: string }>();
  const [activeTab, setActiveTab] = useState<TabKey>('overview');
  const [retryMessage, setRetryMessage] = useState<string | null>(null);

  const connector = connectorId ? mockConnectors[connectorId] : undefined;

  if (!connector) {
    // Fallback for unknown IDs — use first mock for demo
    const fallback = Object.values(mockConnectors)[0];
    if (!fallback) {
      return (
        <PageContainer>
          <Link to="/integrations" className="text-accent hover:underline text-sm flex items-center gap-1 mb-4">
            <ArrowLeft size={14} /> {t('integrations.backToHub')}
          </Link>
          <p className="text-muted">{t('integrations.connectorNotFound')}</p>
        </PageContainer>
      );
    }
    return <ConnectorDetailContent connector={fallback} activeTab={activeTab} setActiveTab={setActiveTab} retryMessage={retryMessage} setRetryMessage={setRetryMessage} t={t} />;
  }

  return <ConnectorDetailContent connector={connector} activeTab={activeTab} setActiveTab={setActiveTab} retryMessage={retryMessage} setRetryMessage={setRetryMessage} t={t} />;
}

function ConnectorDetailContent({
  connector, activeTab, setActiveTab, retryMessage, setRetryMessage, t,
}: {
  connector: ConnectorDetail;
  activeTab: TabKey;
  setActiveTab: (tab: TabKey) => void;
  retryMessage: string | null;
  setRetryMessage: (msg: string | null) => void;
  t: (key: string) => string;
}) {
  const formatDate = (iso: string | null) => {
    if (!iso) return '—';
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  const handleRetry = () => {
    setRetryMessage(t('integrations.retryQueued'));
    setTimeout(() => setRetryMessage(null), 3000);
  };

  const tabs: { key: TabKey; labelKey: string; icon: React.ReactNode }[] = [
    { key: 'overview', labelKey: 'integrations.tabOverview', icon: <List size={14} /> },
    { key: 'configuration', labelKey: 'integrations.tabConfiguration', icon: <Settings size={14} /> },
    { key: 'executions', labelKey: 'integrations.tabExecutions', icon: <Activity size={14} /> },
    { key: 'health', labelKey: 'integrations.tabHealth', icon: <Heart size={14} /> },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Back link */}
      <Link to="/integrations" className="text-accent hover:underline text-sm flex items-center gap-1 mb-4">
        <ArrowLeft size={14} /> {t('integrations.backToHub')}
      </Link>

      {/* Header */}
      <div className="flex items-center gap-3 mb-6">
        <Cable size={24} className="text-accent" />
        <div className="flex-1">
          <h1 className="text-2xl font-bold text-heading">{connector.name}</h1>
          <p className="text-muted text-sm">{connector.provider} · {connector.type}</p>
        </div>
        <Badge variant={statusBadge(connector.status)}>{t(`integrations.${connector.status.toLowerCase()}`)}</Badge>
        <Badge variant={healthBadge(connector.health)}>{t(`integrations.${connector.health.toLowerCase()}`)}</Badge>
        <button
          onClick={handleRetry}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors"
        >
          <RefreshCw size={12} /> {t('integrations.retryConnector')}
        </button>
      </div>

      {retryMessage && (
        <div className="mb-4 px-4 py-2 rounded-md bg-success/15 text-success text-sm flex items-center gap-2">
          <CheckCircle size={14} /> {retryMessage}
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px ${
              activeTab === tab.key
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-body'
            }`}
          >
            {tab.icon} {t(tab.labelKey)}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === 'overview' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Card>
            <CardBody>
              <dl className="space-y-3">
                <div><dt className="text-xs text-muted">{t('integrations.description')}</dt><dd className="text-sm text-heading mt-0.5">{connector.description}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.environment')}</dt><dd className="text-sm text-heading mt-0.5">{connector.environment}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.columnType')}</dt><dd className="text-sm text-heading mt-0.5">{connector.type}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.columnProvider')}</dt><dd className="text-sm text-heading mt-0.5">{connector.provider}</dd></div>
              </dl>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <dl className="space-y-3">
                <div><dt className="text-xs text-muted">{t('integrations.lastSuccess')}</dt><dd className="text-sm text-heading mt-0.5">{formatDate(connector.lastSuccess)}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.lastFailure')}</dt><dd className="text-sm text-heading mt-0.5">{formatDate(connector.lastFailure)}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.freshnessLag')}</dt><dd className={`text-sm mt-0.5 ${connector.health === 'Failed' || connector.health === 'Stale' ? 'text-critical' : 'text-heading'}`}>{connector.freshnessLag}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.itemsSynced')}</dt><dd className="text-sm text-heading mt-0.5">{connector.itemsSynced.toLocaleString()}</dd></div>
              </dl>
            </CardBody>
          </Card>
        </div>
      )}

      {activeTab === 'configuration' && (
        <Card>
          <CardBody>
            <dl className="space-y-3">
              <div><dt className="text-xs text-muted">{t('integrations.endpoint')}</dt><dd className="text-sm text-heading font-mono mt-0.5">{connector.endpoint}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.authMode')}</dt><dd className="text-sm text-heading mt-0.5">{connector.authMode}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.pollingMode')}</dt><dd className="text-sm text-heading mt-0.5">{connector.pollingMode}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.retryPolicy')}</dt><dd className="text-sm text-heading mt-0.5">{connector.retryPolicy}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.enabled')}</dt><dd className="text-sm mt-0.5">{connector.enabled ? <Badge variant="success">{t('common.yes')}</Badge> : <Badge variant="default">{t('common.no')}</Badge>}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.allowedDomains')}</dt><dd className="flex flex-wrap gap-1 mt-0.5">{connector.allowedDomains.map(d => <Badge key={d} variant="info">{d}</Badge>)}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.sourceScope')}</dt><dd className="text-sm text-heading mt-0.5">{connector.sourceScope}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.allowedTeams')}</dt><dd className="flex flex-wrap gap-1 mt-0.5">{connector.allowedTeams.map(tm => <Badge key={tm} variant="default">{tm}</Badge>)}</dd></div>
            </dl>
          </CardBody>
        </Card>
      )}

      {activeTab === 'executions' && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Activity size={16} className="text-accent" />
              {t('integrations.recentExecutions')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="hidden md:grid grid-cols-7 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
              <span>{t('integrations.executionId')}</span>
              <span>{t('integrations.startedAt')}</span>
              <span>{t('integrations.finishedAt')}</span>
              <span>{t('integrations.result')}</span>
              <span className="text-right">{t('integrations.recordsProcessed')}</span>
              <span className="text-right">{t('integrations.warnings')}</span>
              <span className="text-right">{t('integrations.errors')}</span>
            </div>
            <div className="divide-y divide-edge">
              {connector.executions.map(ex => (
                <div key={ex.executionId} className="grid grid-cols-1 md:grid-cols-7 gap-2 px-4 py-3 items-center hover:bg-hover transition-colors">
                  <span className="text-xs font-mono text-muted">{ex.executionId}</span>
                  <span className="text-xs text-muted">{formatDate(ex.startedAt)}</span>
                  <span className="text-xs text-muted">{formatDate(ex.finishedAt)}</span>
                  <span><Badge variant={resultBadge(ex.result)}>{t(`integrations.${ex.result === 'PartialSuccess' ? 'partialSuccess' : ex.result.toLowerCase()}`)}</Badge></span>
                  <span className="text-xs font-mono text-heading text-right">{ex.recordsProcessed.toLocaleString()}</span>
                  <span className={`text-xs font-mono text-right ${ex.warnings > 0 ? 'text-warning' : 'text-muted'}`}>{ex.warnings}</span>
                  <span className={`text-xs font-mono text-right ${ex.errors > 0 ? 'text-critical' : 'text-muted'}`}>{ex.errors}</span>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {activeTab === 'health' && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <dl className="space-y-3">
                <div className="flex items-center gap-2">
                  <dt className="text-xs text-muted">{t('integrations.columnHealth')}:</dt>
                  <dd><Badge variant={healthBadge(connector.health)}>{t(`integrations.${connector.health.toLowerCase()}`)}</Badge></dd>
                </div>
                <div>
                  <dt className="text-xs text-muted">{t('integrations.freshnessLag')}</dt>
                  <dd className={`text-sm font-mono mt-0.5 ${connector.health === 'Failed' || connector.health === 'Stale' ? 'text-critical' : 'text-heading'}`}>{connector.freshnessLag}</dd>
                </div>
                <div>
                  <dt className="text-xs text-muted">{t('integrations.lastSuccess')}</dt>
                  <dd className="text-sm text-heading mt-0.5">{formatDate(connector.lastSuccess)}</dd>
                </div>
                <div>
                  <dt className="text-xs text-muted">{t('integrations.lastFailure')}</dt>
                  <dd className="text-sm text-heading mt-0.5">{formatDate(connector.lastFailure)}</dd>
                </div>
              </dl>
            </CardBody>
          </Card>
          {(connector.health === 'Failed' || connector.health === 'Stale') && (
            <Card>
              <CardBody>
                <div className="flex items-center gap-2 text-critical">
                  {connector.health === 'Failed' ? <XCircle size={16} /> : <AlertTriangle size={16} />}
                  <span className="text-sm font-medium">
                    {connector.health === 'Failed'
                      ? t('integrations.failed')
                      : t('integrations.stale')} — {t('integrations.freshnessLag')}: {connector.freshnessLag}
                  </span>
                </div>
              </CardBody>
            </Card>
          )}
        </div>
      )}
    </div>
  );
}
