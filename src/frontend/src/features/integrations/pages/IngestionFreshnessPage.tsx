import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Gauge, CheckCircle, AlertTriangle, XCircle, Clock, Activity, Shield,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { integrationsApi } from '../api/integrations';

type FreshnessStatus = 'Fresh' | 'Acceptable' | 'Stale' | 'Failed';
type TrustLevel = 'Verified' | 'Trusted' | 'Provisional' | 'Untrusted';

interface DomainFreshness {
  domain: string;
  freshnessStatus: FreshnessStatus;
  lastReceived: string;
  lagMinutes: number;
  trustLevel: TrustLevel;
  sourceCount: number;
}

interface CriticalIssue {
  id: string;
  domain: string;
  message: string;
  severity: 'Critical' | 'Warning';
  since: string;
}

const freshnessStatusBadge = (s: string): 'success' | 'warning' | 'danger' | 'info' => {
  switch (s) {
    case 'Fresh': return 'success';
    case 'Acceptable': return 'info';
    case 'Stale': return 'warning';
    case 'Failed': return 'danger';
    default: return 'info';
  }
};

const deriveTrustLevel = (status: string): TrustLevel => {
  switch (status) {
    case 'Fresh': return 'Verified';
    case 'Acceptable': return 'Trusted';
    case 'Stale': return 'Provisional';
    case 'Failed': return 'Untrusted';
    default: return 'Provisional';
  }
};

const trustBadge = (tl: TrustLevel): 'success' | 'info' | 'warning' | 'danger' => {
  switch (tl) {
    case 'Verified': return 'success';
    case 'Trusted': return 'info';
    case 'Provisional': return 'warning';
    case 'Untrusted': return 'danger';
  }
};

const overallHealthColor = (h: string) => {
  switch (h) {
    case 'Healthy': return 'text-success';
    case 'Degraded': return 'text-warning';
    case 'Critical': return 'text-critical';
    default: return 'text-muted';
  }
};

const overallHealthIcon = (h: string) => {
  switch (h) {
    case 'Healthy': return <CheckCircle size={20} className="text-success" />;
    case 'Degraded': return <AlertTriangle size={20} className="text-warning" />;
    case 'Critical': return <XCircle size={20} className="text-critical" />;
    default: return <Activity size={20} className="text-muted" />;
  }
};

function formatLag(minutes: number): string {
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  const remaining = minutes % 60;
  if (hours < 24) return remaining > 0 ? `${hours}h ${remaining}m` : `${hours}h`;
  const days = Math.floor(hours / 24);
  const remainHours = hours % 24;
  return remainHours > 0 ? `${days}d ${remainHours}h` : `${days}d`;
}

function computeLagMinutes(latestIngestionAt: string): number {
  return Math.max(0, Math.floor((Date.now() - new Date(latestIngestionAt).getTime()) / 60_000));
}

export function IngestionFreshnessPage() {
  const { t } = useTranslation();

  const { data: freshnessResponse, isLoading: loadingFreshness, isError: errorFreshness, refetch: refetchFreshness } = useQuery({
    queryKey: ['integrations', 'freshness'],
    queryFn: () => integrationsApi.getFreshness(),
    staleTime: 30_000,
  });

  const { data: health } = useQuery({
    queryKey: ['integrations', 'health'],
    queryFn: () => integrationsApi.getHealth(),
    staleTime: 30_000,
  });

  const indicators = useMemo(() => freshnessResponse?.indicators ?? [], [freshnessResponse?.indicators]);
  const overallHealth = health?.overallHealth ?? 'Degraded';

  const domains: DomainFreshness[] = useMemo(() => indicators.map(ind => ({
    domain: ind.dataDomain,
    freshnessStatus: ind.freshnessStatus as FreshnessStatus,
    lastReceived: ind.latestIngestionAt,
    lagMinutes: computeLagMinutes(ind.latestIngestionAt),
    trustLevel: deriveTrustLevel(ind.freshnessStatus),
    sourceCount: ind.totalSources,
  })), [indicators]);

  const criticalIssues: CriticalIssue[] = useMemo(() =>
    indicators
      .filter(ind => ind.freshnessStatus === 'Stale' || ind.freshnessStatus === 'Failed')
      .map((ind, idx) => ({
        id: `ci-${idx}`,
        domain: ind.dataDomain,
        message: `${ind.dataDomain}: ${ind.staleSources}/${ind.totalSources} source(s) ${ind.freshnessStatus.toLowerCase()}.`,
        severity: (ind.freshnessStatus === 'Failed' ? 'Critical' : 'Warning') as 'Critical' | 'Warning',
        since: ind.latestIngestionAt,
      })),
    [indicators],
  );

  const freshCount = domains.filter(d => d.freshnessStatus === 'Fresh').length;
  const staleCount = domains.filter(d => d.freshnessStatus === 'Stale' || d.freshnessStatus === 'Failed').length;
  const totalSources = domains.reduce((sum, d) => sum + d.sourceCount, 0);

  const formatDate = (iso: string) => {
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  if (loadingFreshness) return <PageLoadingState />;
  if (errorFreshness) return <PageErrorState action={<button onClick={() => refetchFreshness()} className="btn btn-sm btn-primary">{t('common.retry')}</button>} />;

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('integrations.freshnessTitle')}</h1>
        <p className="text-muted mt-1">{t('integrations.freshnessSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard
          title={t('integrations.overallHealth')}
          value={t(`integrations.${overallHealth.toLowerCase()}`)}
          icon={overallHealthIcon(overallHealth)}
          color={overallHealthColor(overallHealth)}
        />
        <StatCard title={t('integrations.fresh')} value={freshCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('integrations.staleFeeds')} value={staleCount} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('integrations.sourceCount')} value={totalSources} icon={<Activity size={20} />} color="text-accent" />
      </div>

      {/* Critical issues */}
      {criticalIssues.length > 0 && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-critical" />
              {t('integrations.criticalIssues')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {criticalIssues.map(issue => (
                <div key={issue.id} className="px-4 py-3 flex items-start gap-3 hover:bg-hover transition-colors">
                  {issue.severity === 'Critical'
                    ? <XCircle size={16} className="text-critical shrink-0 mt-0.5" />
                    : <AlertTriangle size={16} className="text-warning shrink-0 mt-0.5" />
                  }
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 mb-0.5">
                      <Badge variant={issue.severity === 'Critical' ? 'danger' : 'warning'}>{issue.severity === 'Critical' ? t('integrations.critical') : t('integrations.degraded')}</Badge>
                      <span className="text-xs text-muted">{issue.domain}</span>
                    </div>
                    <p className="text-sm text-heading">{issue.message}</p>
                    <p className="text-xs text-muted mt-0.5">{formatDate(issue.since)}</p>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Domain freshness cards */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Gauge size={16} className="text-accent" />
            {t('integrations.domainFreshness')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          {/* Header row */}
          <div className="hidden md:grid grid-cols-6 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
            <span>{t('integrations.domain')}</span>
            <span>{t('integrations.columnStatus')}</span>
            <span>{t('integrations.lastReceived')}</span>
            <span>{t('integrations.lagMinutes')}</span>
            <span>{t('integrations.trustLevel')}</span>
            <span className="text-right">{t('integrations.sourceCount')}</span>
          </div>
          <div className="divide-y divide-edge">
            {domains.map(dm => (
              <div
                key={dm.domain}
                className={`grid grid-cols-1 md:grid-cols-6 gap-2 px-4 py-3 items-center hover:bg-hover transition-colors ${
                  dm.freshnessStatus === 'Failed' ? 'bg-critical/5' : dm.freshnessStatus === 'Stale' ? 'bg-warning/5' : ''
                }`}
              >
                <span className="text-sm font-medium text-heading flex items-center gap-2">
                  {dm.freshnessStatus === 'Fresh' && <CheckCircle size={14} className="text-success" />}
                  {dm.freshnessStatus === 'Acceptable' && <Clock size={14} className="text-info" />}
                  {dm.freshnessStatus === 'Stale' && <AlertTriangle size={14} className="text-warning" />}
                  {dm.freshnessStatus === 'Failed' && <XCircle size={14} className="text-critical" />}
                  {dm.domain}
                </span>
                <span>
                  <Badge variant={freshnessStatusBadge(dm.freshnessStatus)}>
                    {t(`integrations.${dm.freshnessStatus.toLowerCase()}`)}
                  </Badge>
                </span>
                <span className="text-xs text-muted">{formatDate(dm.lastReceived)}</span>
                <span className={`text-xs font-mono ${dm.lagMinutes > 120 ? 'text-critical' : dm.lagMinutes > 30 ? 'text-warning' : 'text-muted'}`}>
                  {formatLag(dm.lagMinutes)}
                </span>
                <span>
                  <Badge variant={trustBadge(dm.trustLevel)}>
                    {t(`integrations.${dm.trustLevel.toLowerCase()}`)}
                  </Badge>
                </span>
                <span className="text-xs font-mono text-heading text-right">{dm.sourceCount}</span>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
