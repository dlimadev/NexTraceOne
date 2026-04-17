import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Activity, CheckCircle, AlertTriangle, XCircle,
  TrendingUp, TrendingDown, Minus, Shield, ArrowLeft,
  FileText, Link2, BookOpen, GitBranch,
} from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { reliabilityApi } from '../api/reliability';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const statusBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default'; icon: React.ReactNode } => {
  switch (status) {
    case 'Healthy': return { variant: 'success', icon: <CheckCircle size={14} /> };
    case 'Degraded': return { variant: 'warning', icon: <AlertTriangle size={14} /> };
    case 'Unavailable': return { variant: 'danger', icon: <XCircle size={14} /> };
    case 'NeedsAttention': return { variant: 'default', icon: <Shield size={14} /> };
    default: return { variant: 'default', icon: <Minus size={14} /> };
  }
};

const trendIcon = (dir: string) => {
  switch (dir) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    default: return <Minus size={14} className="text-muted" />;
  }
};

export function ServiceReliabilityDetailPage() {
  const { serviceId } = useParams<{ serviceId: string }>();
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['reliability-detail', serviceId, activeEnvironmentId],
    queryFn: () => reliabilityApi.getServiceDetail(serviceId!),
    enabled: !!serviceId,
    staleTime: 30_000,
    retry: false,
  });

  if (isLoading) return <PageLoadingState />;

  if (isError) {
    const isNotFound = (error as { response?: { status?: number } })?.response?.status === 404;
    return (
      <PageContainer>
        <NavLink to="/operations/reliability" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
          <ArrowLeft size={14} /> {t('reliability.detail.backToOverview')}
        </NavLink>
        <PageErrorState
          message={isNotFound ? t('reliability.detail.notFound') : t('reliability.loadError')}
          onRetry={!isNotFound ? refetch : undefined}
        />
      </PageContainer>
    );
  }

  if (!data) return null;

  const badge = statusBadge(data.status);

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <NavLink to="/operations/reliability" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
        <ArrowLeft size={14} /> {t('reliability.detail.backToOverview')}
      </NavLink>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{data.identity.displayName}</h1>
          <p className="text-sm text-muted mt-0.5">{data.identity.serviceId} · {data.identity.serviceType} · {data.identity.domain}</p>
        </div>
        <Badge variant={badge.variant} className="flex items-center gap-1 text-sm px-3 py-1">
          {badge.icon}
          {t(`reliability.status.${data.status}`)}
        </Badge>
      </div>

      {/* Overview Cards */}
      <div className="grid md:grid-cols-4 gap-4 mb-6">
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.availability')}</p><p className="text-lg font-semibold text-heading">{data.metrics.availabilityPercent.toFixed(2)}%</p></CardBody></Card>
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.latencyP99')}</p><p className="text-lg font-semibold text-heading">{data.metrics.latencyP99Ms.toFixed(1)} ms</p></CardBody></Card>
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.errorRate')}</p><p className="text-lg font-semibold text-heading">{data.metrics.errorRatePercent.toFixed(2)}%</p></CardBody></Card>
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.throughput')}</p><p className="text-lg font-semibold text-heading">{data.metrics.requestsPerSecond.toFixed(0)} rps</p></CardBody></Card>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        {/* Operational Summary */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><Activity size={16} className="text-accent" />{t('reliability.detail.operationalSummary')}</h2></CardHeader>
          <CardBody>
            <p className="text-sm text-body">{data.operationalSummary}</p>
            <div className="mt-3 flex items-center gap-2 text-xs text-muted">
              {trendIcon(data.trend.direction)}
              <span>{t(`reliability.trend.${data.trend.direction}`)} · {data.trend.timeframe}</span>
            </div>
            {data.trend.summary && <p className="text-xs text-muted mt-1">{data.trend.summary}</p>}
          </CardBody>
        </Card>

        {/* Anomaly Summary */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><AlertTriangle size={16} className="text-warning" />{t('reliability.detail.anomalySummary')}</h2></CardHeader>
          <CardBody>
            <p className="text-sm text-body">{data.anomalySummary}</p>
          </CardBody>
        </Card>

        {/* Ownership */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading">{t('reliability.detail.ownership')}</h2></CardHeader>
          <CardBody>
            <dl className="grid grid-cols-2 gap-2 text-sm">
              <div><dt className="text-xs text-muted">{t('reliability.detail.team')}</dt><dd className="text-body">{data.identity.teamName}</dd></div>
              <div><dt className="text-xs text-muted">{t('reliability.detail.criticality')}</dt><dd className="text-body">{data.identity.criticality}</dd></div>
            </dl>
          </CardBody>
        </Card>

        {/* Recent Changes */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><GitBranch size={16} className="text-accent" />{t('reliability.detail.recentChanges')}</h2></CardHeader>
          <CardBody>
            {data.recentChanges.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noRecentChanges')}</p>
            ) : (
              <ul className="space-y-2">
                {data.recentChanges.map(c => (
                  <li key={c.changeId} className="text-sm">
                    <span className="text-body">{c.description}</span>
                    <span className="text-xs text-muted ml-2">{new Date(c.deployedAt).toLocaleDateString()}</span>
                    <Badge variant={c.confidenceStatus === 'Validated' ? 'success' : 'warning'} className="ml-2 text-[10px]">{c.confidenceStatus}</Badge>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Linked Incidents */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><AlertTriangle size={16} className="text-critical" />{t('reliability.detail.linkedIncidents')}</h2></CardHeader>
          <CardBody>
            {data.linkedIncidents.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noIncidents')}</p>
            ) : (
              <ul className="space-y-2">
                {data.linkedIncidents.map(inc => (
                  <li key={inc.incidentId} className="text-sm">
                    <span className="font-medium text-heading">{inc.reference}</span>
                    <span className="text-body ml-2">{inc.title}</span>
                    <Badge variant="danger" className="ml-2 text-[10px]">{inc.status}</Badge>
                    <span className="text-xs text-muted ml-2">{new Date(inc.reportedAt).toLocaleDateString()}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Dependencies */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><Link2 size={16} className="text-accent" />{t('reliability.detail.dependencies')}</h2></CardHeader>
          <CardBody>
            {data.dependencies.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noDependencies')}</p>
            ) : (
              <ul className="space-y-2">
                {data.dependencies.map(dep => {
                  const depBadge = statusBadge(dep.status);
                  return (
                    <li key={dep.serviceId} className="flex items-center gap-2 text-sm">
                      <Badge variant={depBadge.variant} className="flex items-center gap-1 text-[10px]">
                        {depBadge.icon} {t(`reliability.status.${dep.status}`)}
                      </Badge>
                      <NavLink to={`/operations/reliability/${dep.serviceId}`} className="text-accent hover:underline">{dep.displayName}</NavLink>
                    </li>
                  );
                })}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Contracts */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><FileText size={16} className="text-accent" />{t('reliability.detail.linkedContracts')}</h2></CardHeader>
          <CardBody>
            {data.linkedContracts.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noContracts')}</p>
            ) : (
              <ul className="space-y-2">
                {data.linkedContracts.map(c => (
                  <li key={c.contractVersionId} className="text-sm">
                    <span className="text-body">{c.name}</span>
                    <span className="text-xs text-muted ml-2">v{c.version} · {c.protocol} · {c.lifecycleState}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Runbooks */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><BookOpen size={16} className="text-accent" />{t('reliability.detail.runbooks')}</h2></CardHeader>
          <CardBody>
            {data.runbooks.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noRunbooks')}</p>
            ) : (
              <ul className="space-y-2">
                {data.runbooks.map((r, i) => (
                  <li key={i} className="text-sm">
                    {r.url ? (
                      <a href={r.url} className="text-accent hover:underline">{r.title}</a>
                    ) : (
                      <span className="text-body">{r.title}</span>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>

      {/* Coverage */}
      <Card className="mt-6">
        <CardHeader><h2 className="text-sm font-semibold text-heading">{t('reliability.detail.coverage')}</h2></CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
            {[
              { key: 'signals', value: data.coverage.hasOperationalSignals },
              { key: 'runbook', value: data.coverage.hasRunbook },
              { key: 'owner', value: data.coverage.hasOwner },
              { key: 'deps', value: data.coverage.hasDependenciesMapped },
              { key: 'changes', value: data.coverage.hasRecentChangeContext },
              { key: 'incidents', value: data.coverage.hasIncidentLinkage },
            ].map(({ key, value }) => (
              <div key={key} className={`flex items-center gap-2 text-sm px-3 py-2 rounded-md border ${value ? 'border-success/25 bg-success/5 text-success' : 'border-critical/25 bg-critical/15 text-critical'}`}>
                {value ? <CheckCircle size={14} /> : <XCircle size={14} />}
                {t(`reliability.coverage.${key}`)}
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
