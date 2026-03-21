import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import {
  Cable, ArrowLeft, RefreshCw, CheckCircle, AlertTriangle, XCircle, Clock,
  Settings, Activity, Heart, List,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { integrationsApi } from '../api/integrations';
import type { IntegrationConnectorDetailDto } from '../../../types';

const statusBadge = (s: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (s) {
    case 'Active': return 'success';
    case 'Degraded': return 'warning';
    case 'Failed': return 'danger';
    case 'Disabled': return 'default';
    default: return 'default';
  }
};

const deriveHealthLabel = (score: number): string => {
  if (score >= 80) return 'Healthy';
  if (score >= 50) return 'Degraded';
  if (score > 0) return 'Failed';
  return 'Stale';
};

const healthBadgeVariant = (score: number): 'success' | 'warning' | 'danger' | 'info' => {
  if (score >= 80) return 'success';
  if (score >= 50) return 'warning';
  if (score > 0) return 'danger';
  return 'info';
};

const resultBadge = (r: string): 'success' | 'warning' | 'danger' => {
  switch (r) {
    case 'Success': return 'success';
    case 'PartialSuccess': return 'warning';
    default: return 'danger';
  }
};

const computeFreshnessLag = (lastSyncAt: string | null): string => {
  if (!lastSyncAt) return '\u2014';
  const diffMs = Date.now() - new Date(lastSyncAt).getTime();
  const minutes = Math.floor(diffMs / 60_000);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const remainingMin = minutes % 60;
  return remainingMin > 0 ? `${hours}h ${remainingMin}m` : `${hours}h`;
};

type TabKey = 'overview' | 'configuration' | 'executions' | 'health';

export function ConnectorDetailPage() {
  const { t } = useTranslation();
  const { connectorId } = useParams<{ connectorId: string }>();
  const [activeTab, setActiveTab] = useState<TabKey>('overview');

  const { data: connector, isLoading, isError, refetch } = useQuery({
    queryKey: ['integrations', 'connector', connectorId],
    queryFn: () => integrationsApi.getConnector(connectorId!),
    enabled: !!connectorId,
    staleTime: 30_000,
  });

  const retryMutation = useMutation({
    mutationFn: () => integrationsApi.retryConnector(connectorId!),
  });

  if (isLoading) return <PageLoadingState />;

  if (isError) {
    return (
      <PageContainer>
        <Link to="/integrations" className="text-accent hover:underline text-sm flex items-center gap-1 mb-4">
          <ArrowLeft size={14} /> {t('integrations.backToHub')}
        </Link>
        <PageErrorState action={<button onClick={() => refetch()} className="btn btn-sm btn-primary">{t('common.retry')}</button>} />
      </PageContainer>
    );
  }

  if (!connector) {
    return (
      <PageContainer>
        <Link to="/integrations" className="text-accent hover:underline text-sm flex items-center gap-1 mb-4">
          <ArrowLeft size={14} /> {t('integrations.backToHub')}
        </Link>
        <p className="text-muted">{t('integrations.connectorNotFound')}</p>
      </PageContainer>
    );
  }

  return (
    <ConnectorDetailContent
      connector={connector}
      activeTab={activeTab}
      setActiveTab={setActiveTab}
      onRetry={() => retryMutation.mutate()}
      isRetrying={retryMutation.isPending}
      retrySuccess={retryMutation.isSuccess}
      t={t}
    />
  );
}

function ConnectorDetailContent({
  connector, activeTab, setActiveTab, onRetry, isRetrying, retrySuccess, t,
}: {
  connector: IntegrationConnectorDetailDto;
  activeTab: TabKey;
  setActiveTab: (tab: TabKey) => void;
  onRetry: () => void;
  isRetrying: boolean;
  retrySuccess: boolean;
  t: (key: string) => string;
}) {
  const formatDate = (iso: string | null) => {
    if (!iso) return '\u2014';
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  const healthLabel = deriveHealthLabel(connector.healthScore);
  const freshnessLag = computeFreshnessLag(connector.lastSyncAt);
  const totalSynced = connector.recentExecutions.reduce((sum, ex) => sum + ex.recordsProcessed, 0);
  const isUnhealthy = connector.healthScore < 50;

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
          <p className="text-muted text-sm">{connector.provider} &middot; {connector.connectorType}</p>
        </div>
        <Badge variant={statusBadge(connector.status)}>{t(`integrations.${connector.status.toLowerCase()}`)}</Badge>
        <Badge variant={healthBadgeVariant(connector.healthScore)}>{t(`integrations.${healthLabel.toLowerCase()}`)}</Badge>
        <button
          onClick={onRetry}
          disabled={isRetrying}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors disabled:opacity-50"
        >
          <RefreshCw size={12} className={isRetrying ? 'animate-spin' : ''} /> {t('integrations.retryConnector')}
        </button>
      </div>

      {retrySuccess && (
        <div className="mb-4 px-4 py-2 rounded-md bg-success/15 text-success text-sm flex items-center gap-2">
          <CheckCircle size={14} /> {t('integrations.retryQueued')}
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
                <div><dt className="text-xs text-muted">{t('integrations.columnType')}</dt><dd className="text-sm text-heading mt-0.5">{connector.connectorType}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.columnProvider')}</dt><dd className="text-sm text-heading mt-0.5">{connector.provider}</dd></div>
              </dl>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <dl className="space-y-3">
                <div><dt className="text-xs text-muted">{t('integrations.lastSuccess')}</dt><dd className="text-sm text-heading mt-0.5">{formatDate(connector.lastSyncAt)}</dd></div>
                {/* lastFailure is not available in the connector detail DTO */}
                <div><dt className="text-xs text-muted">{t('integrations.lastFailure')}</dt><dd className="text-sm text-heading mt-0.5">{'\u2014'}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.freshnessLag')}</dt><dd className={`text-sm mt-0.5 ${isUnhealthy ? 'text-critical' : 'text-heading'}`}>{freshnessLag}</dd></div>
                <div><dt className="text-xs text-muted">{t('integrations.itemsSynced')}</dt><dd className="text-sm text-heading mt-0.5">{totalSynced.toLocaleString()}</dd></div>
              </dl>
            </CardBody>
          </Card>
        </div>
      )}

      {activeTab === 'configuration' && (
        <Card>
          <CardBody>
            <dl className="space-y-3">
              <div><dt className="text-xs text-muted">{t('integrations.endpoint')}</dt><dd className="text-sm text-heading font-mono mt-0.5">{connector.configuration['endpoint'] ?? '\u2014'}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.authMode')}</dt><dd className="text-sm text-heading mt-0.5">{connector.configuration['authMode'] ?? '\u2014'}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.pollingMode')}</dt><dd className="text-sm text-heading mt-0.5">{connector.syncFrequency}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.retryPolicy')}</dt><dd className="text-sm text-heading mt-0.5">{connector.configuration['retryPolicy'] ?? '\u2014'}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.enabled')}</dt><dd className="text-sm mt-0.5">{connector.status !== 'Disabled' ? <Badge variant="success">{t('common.yes')}</Badge> : <Badge variant="default">{t('common.no')}</Badge>}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.allowedDomains')}</dt><dd className="flex flex-wrap gap-1 mt-0.5">{connector.dataDomains.map(d => <Badge key={d} variant="info">{d}</Badge>)}</dd></div>
              <div><dt className="text-xs text-muted">{t('integrations.sourceScope')}</dt><dd className="text-sm text-heading mt-0.5">{connector.sources.length} source(s)</dd></div>
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
              {connector.recentExecutions.map(ex => (
                <div key={ex.executionId} className="grid grid-cols-1 md:grid-cols-7 gap-2 px-4 py-3 items-center hover:bg-hover transition-colors">
                  <span className="text-xs font-mono text-muted">{ex.executionId}</span>
                  <span className="text-xs text-muted">{formatDate(ex.startedAt)}</span>
                  <span className="text-xs text-muted">{formatDate(ex.completedAt)}</span>
                  <span><Badge variant={resultBadge(ex.result)}>{t(`integrations.${ex.result === 'PartialSuccess' ? 'partialSuccess' : ex.result.toLowerCase()}`)}</Badge></span>
                  <span className="text-xs font-mono text-heading text-right">{ex.recordsProcessed.toLocaleString()}</span>
                  {/* IngestionExecutionDto does not include a warnings field */}
                  <span className="text-xs font-mono text-right text-muted">0</span>
                  <span className={`text-xs font-mono text-right ${ex.recordsFailed > 0 ? 'text-critical' : 'text-muted'}`}>{ex.recordsFailed}</span>
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
                  <dd><Badge variant={healthBadgeVariant(connector.healthScore)}>{t(`integrations.${healthLabel.toLowerCase()}`)}</Badge></dd>
                </div>
                <div>
                  <dt className="text-xs text-muted">{t('integrations.freshnessLag')}</dt>
                  <dd className={`text-sm font-mono mt-0.5 ${isUnhealthy ? 'text-critical' : 'text-heading'}`}>{freshnessLag}</dd>
                </div>
                <div>
                  <dt className="text-xs text-muted">{t('integrations.lastSuccess')}</dt>
                  <dd className="text-sm text-heading mt-0.5">{formatDate(connector.lastSyncAt)}</dd>
                </div>
                <div>
                  {/* lastFailure is not available in the connector detail DTO */}
                  <dt className="text-xs text-muted">{t('integrations.lastFailure')}</dt>
                  <dd className="text-sm text-heading mt-0.5">{'\u2014'}</dd>
                </div>
              </dl>
            </CardBody>
          </Card>
          {isUnhealthy && (
            <Card>
              <CardBody>
                <div className="flex items-center gap-2 text-critical">
                  {connector.healthScore === 0 ? <XCircle size={16} /> : <AlertTriangle size={16} />}
                  <span className="text-sm font-medium">
                    {connector.healthScore === 0
                      ? t('integrations.failed')
                      : t('integrations.stale')} &mdash; {t('integrations.freshnessLag')}: {freshnessLag}
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
