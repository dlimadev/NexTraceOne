import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  Globe, Users, Server, Shield, GitBranch,
  ArrowRight, TrendingUp, Minus, AlertTriangle, Activity,
  CheckCircle, Tag, Calendar, ArrowLeft, BarChart3,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import type { DomainDetail, GovernanceSummary, DomainTeamDto, DomainServiceDto, CrossDomainDependencyDto } from '../../../types';

type TabId = 'overview' | 'teams' | 'services' | 'governance' | 'dependencies';

const maturityBadgeVariant = (level: string): 'success' | 'info' | 'warning' | 'danger' => {
  switch (level) {
    case 'Optimizing':
    case 'Managed':
      return 'success';
    case 'Defined':
      return 'info';
    case 'Developing':
      return 'warning';
    case 'Initial':
    default:
      return 'danger';
  }
};

const criticalityVariant = (c: string): 'danger' | 'warning' | 'info' | 'default' => {
  switch (c) {
    case 'Critical':
      return 'danger';
    case 'High':
      return 'warning';
    case 'Medium':
      return 'info';
    default:
      return 'default';
  }
};

const statusBadgeVariant = (status: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Inactive':
      return 'warning';
    case 'Archived':
      return 'danger';
    default:
      return 'default';
  }
};

const ownershipBadgeVariant = (type: string): 'success' | 'info' | 'warning' | 'default' => {
  switch (type) {
    case 'Primary':
      return 'success';
    case 'Shared':
      return 'info';
    case 'Delegated':
      return 'warning';
    default:
      return 'default';
  }
};

const trendIcon = (trend: string) => {
  switch (trend) {
    case 'Improving':
      return <TrendingUp size={14} className="text-success" />;
    case 'Declining':
      return <TrendingUp size={14} className="text-critical rotate-180" />;
    default:
      return <Minus size={14} className="text-muted" />;
  }
};

export function DomainDetailPage() {
  const { t } = useTranslation();
  const { domainId } = useParams<{ domainId: string }>();
  const [activeTab, setActiveTab] = useState<TabId>('overview');
  const [domain, setDomain] = useState<DomainDetail | null>(null);
  const [gov, setGov] = useState<GovernanceSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!domainId) return;
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setLoading(true);
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setError(null);

    Promise.all([
      organizationGovernanceApi.getDomainDetail(domainId),
      organizationGovernanceApi.getDomainGovernanceSummary(domainId).catch(() => null),
    ])
      .then(([domainData, govData]) => {
        if (!cancelled) {
          setDomain(domainData);
          setGov(govData);
          setLoading(false);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err.message || t('common.errorLoading'));
          setLoading(false);
        }
      });

    return () => { cancelled = true; };
  }, [domainId, t]);

  const tabs: { id: TabId; labelKey: string; icon: React.ReactNode }[] = [
    { id: 'overview', labelKey: 'organization.domainDetail.tabs.overview', icon: <Globe size={16} /> },
    { id: 'teams', labelKey: 'organization.domainDetail.tabs.teams', icon: <Users size={16} /> },
    { id: 'services', labelKey: 'organization.domainDetail.tabs.services', icon: <Server size={16} /> },
    { id: 'governance', labelKey: 'organization.domainDetail.tabs.governance', icon: <Shield size={16} /> },
    { id: 'dependencies', labelKey: 'organization.domainDetail.tabs.dependencies', icon: <GitBranch size={16} /> },
  ];

  const formatDate = (iso: string) => new Date(iso).toLocaleDateString();
  const formatPct = (v: number) => `${Math.round(v * 100)}%`;

  if (loading) {
    return (
      <PageContainer>
        <Link to="/governance/domains" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
          <ArrowLeft size={14} />
          {t('organization.domains.title')}
        </Link>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (error || !domain) {
    return (
      <PageContainer>
        <Link to="/governance/domains" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
          <ArrowLeft size={14} />
          {t('organization.domains.title')}
        </Link>
        <PageErrorState message={error || t('organization.domainDetail.notFound')} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* Back link */}
      <Link to="/governance/domains" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
        <ArrowLeft size={14} />
        {t('organization.domains.title')}
      </Link>

      <ModuleHeader
        titleKey="organization.domainDetail.title"
        subtitleKey="organization.domainDetail.subtitle"
        actions={
          <div className="flex items-center gap-2">
            <Badge variant={criticalityVariant(domain.criticality)}>
              {t(`organization.domains.criticality.${domain.criticality}`)}
            </Badge>
            <Badge variant={maturityBadgeVariant(domain.maturityLevel)}>
              {t(`organization.domains.maturityLevel.${domain.maturityLevel}`)}
            </Badge>
          </div>
        }
      />

      {/* Domain info header */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-wrap items-start gap-6">
            <div className="flex-1 min-w-0">
              <h2 className="text-lg font-bold text-heading">{domain.displayName}</h2>
              <p className="text-sm text-muted mt-1">{domain.description || '—'}</p>
              <div className="flex flex-wrap items-center gap-4 mt-3 text-xs text-muted">
                <span className="flex items-center gap-1">
                  <Tag size={12} />
                  {t('organization.domainDetail.capability')}: {domain.capabilityClassification || '—'}
                </span>
                <span className="flex items-center gap-1">
                  <Calendar size={12} />
                  {t('organization.domainDetail.createdAt')}: {formatDate(domain.createdAt)}
                </span>
              </div>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Tabs */}
      <div className="flex items-center gap-1 border-b border-edge mb-6 overflow-x-auto">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              activeTab === tab.id
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-body hover:border-edge'
            }`}
          >
            {tab.icon}
            {t(tab.labelKey)}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === 'overview' && <OverviewTab domain={domain} t={t} />}
      {activeTab === 'teams' && <TeamsTab teams={domain.teams} t={t} />}
      {activeTab === 'services' && <ServicesTab services={domain.services} t={t} />}
      {activeTab === 'governance' && <GovernanceTab gov={gov} t={t} formatPct={formatPct} />}
      {activeTab === 'dependencies' && <DependenciesTab deps={domain.crossDomainDependencies} t={t} />}
    </PageContainer>
  );
}

/* ─── Overview Tab ─── */

function OverviewTab({ domain, t }: { domain: DomainDetail; t: (key: string) => string }) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      <StatCard title={t('organization.domainDetail.teams')} value={domain.teamCount} icon={<Users size={20} />} color="text-accent" />
      <StatCard title={t('organization.domainDetail.services')} value={domain.serviceCount} icon={<Server size={20} />} color="text-blue-500" />
      <StatCard title={t('organization.domainDetail.activeIncidents')} value={domain.activeIncidentCount} icon={<AlertTriangle size={20} />} color="text-critical" />
      <StatCard title={t('organization.domainDetail.reliabilityScore')} value={`${domain.reliabilityScore}%`} icon={<Activity size={20} />} color="text-success" />
    </div>
  );
}

/* ─── Teams Tab ─── */

function TeamsTab({ teams, t }: { teams: DomainTeamDto[]; t: (key: string) => string }) {
  if (teams.length === 0) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.domainDetail.noTeams')}</div>;
  }

  return (
    <Card>
      <CardHeader>
        <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
          <Users size={16} className="text-accent" />
          {t('organization.domainDetail.teams')}
        </h2>
      </CardHeader>
      <CardBody className="p-0">
        <div className="divide-y divide-edge">
          {teams.map(team => (
            <Link key={team.teamId} to={`/governance/teams/${team.teamId}`} className="px-4 py-3 flex items-center gap-3 hover:bg-hover transition-colors">
              <Users size={14} className="text-muted shrink-0" />
              <span className="text-sm font-medium text-heading flex-1 truncate">{team.displayName}</span>
              <span className="text-xs text-muted hidden md:inline">{team.serviceCount} {t('organization.domainDetail.services')}</span>
              <Badge variant={ownershipBadgeVariant(team.ownershipType ?? 'Unknown')}>
                {t(`organization.domainDetail.ownershipType.${team.ownershipType ?? 'Unknown'}`)}
              </Badge>
              <ArrowRight size={14} className="text-muted" />
            </Link>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

/* ─── Services Tab ─── */

function ServicesTab({ services, t }: { services: DomainServiceDto[]; t: (key: string) => string }) {
  if (services.length === 0) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.domainDetail.noServices')}</div>;
  }

  return (
    <Card>
      <CardHeader>
        <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
          <Server size={16} className="text-accent" />
          {t('organization.domainDetail.services')}
        </h2>
      </CardHeader>
      <CardBody className="p-0">
        <div className="divide-y divide-edge">
          {services.map(svc => (
            <Link key={svc.serviceId} to={`/services/${svc.serviceId}`} className="px-4 py-3 flex items-center gap-3 hover:bg-hover transition-colors">
              <Server size={14} className="text-muted shrink-0" />
              <span className="text-sm font-medium text-heading flex-1 truncate">{svc.name}</span>
              <span className="text-xs text-muted hidden md:inline">{svc.teamName}</span>
              <Badge variant={criticalityVariant(svc.criticality)}>{svc.criticality}</Badge>
              <Badge variant={statusBadgeVariant(svc.status ?? 'Unknown')}>{svc.status ?? 'Unknown'}</Badge>
              <ArrowRight size={14} className="text-muted" />
            </Link>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

/* ─── Governance Tab ─── */

function GovernanceTab({ gov, t, formatPct }: { gov: GovernanceSummary | null; t: (key: string) => string; formatPct: (v: number) => string }) {
  if (!gov) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.domainDetail.noGovernanceData')}</div>;
  }

  return (
    <div className="space-y-6">
      {/* Summary */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Shield size={16} className="text-accent" />
            {t('organization.domainDetail.governanceSummary')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="flex flex-wrap items-center gap-4 mb-4">
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted">{t('organization.domainDetail.overallMaturity')}:</span>
              <Badge variant={maturityBadgeVariant(gov.overallMaturity)}>
                {t(`organization.domains.maturityLevel.${gov.overallMaturity}`)}
              </Badge>
            </div>
            <div className="flex items-center gap-4 text-sm">
              <span className="flex items-center gap-1 text-muted">
                <AlertTriangle size={14} className="text-warning" />
                {t('organization.domainDetail.risks')}: <span className="text-heading font-medium">{gov.openRiskCount}</span>
              </span>
              <span className="flex items-center gap-1 text-muted">
                <Shield size={14} className="text-critical" />
                {t('organization.domainDetail.policyViolations')}: <span className="text-heading font-medium">{gov.policyViolationCount}</span>
              </span>
            </div>
          </div>

          {/* Coverage bars */}
          <div className="space-y-3">
            {([
              { key: 'ownership', value: gov.ownershipCoverage },
              { key: 'contracts', value: gov.contractCoverage },
              { key: 'documentation', value: gov.documentationCoverage },
            ] as const).map(cov => (
              <div key={cov.key}>
                <div className="flex items-center justify-between text-xs mb-1">
                  <span className="text-muted">{t(`organization.domainDetail.coverage.${cov.key}`)}</span>
                  <span className="text-heading font-medium">{formatPct(cov.value)}</span>
                </div>
                <div className="h-2 bg-elevated rounded-full overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all ${
                      cov.value >= 0.9 ? 'bg-success' : cov.value >= 0.7 ? 'bg-warning' : 'bg-critical'
                    }`}
                    style={{ width: formatPct(cov.value) }}
                  />
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Dimensions */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <BarChart3 size={16} className="text-accent" />
            {t('organization.domainDetail.dimensions.dimension')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {gov.dimensions.map(dim => (
              <div key={dim.dimension} className="px-4 py-3 flex items-center gap-3">
                <CheckCircle size={14} className={dim.score >= 85 ? 'text-success' : dim.score >= 70 ? 'text-warning' : 'text-critical'} />
                <span className="text-sm font-medium text-heading flex-1 truncate">{dim.dimension}</span>
                <Badge variant={maturityBadgeVariant(dim.level)}>
                  {t(`organization.domains.maturityLevel.${dim.level}`)}
                </Badge>
                <span className="text-sm font-mono text-heading w-12 text-right">{dim.score}</span>
                <div className="flex items-center gap-1 text-xs text-muted w-24">
                  {trendIcon(dim.trend)}
                  <span>{dim.trend}</span>
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}

/* ─── Dependencies Tab ─── */

function DependenciesTab({ deps, t }: { deps: CrossDomainDependencyDto[]; t: (key: string) => string }) {
  if (deps.length === 0) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.domainDetail.noDependencies')}</div>;
  }

  return (
    <div className="space-y-6">
      {/* Outbound */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <GitBranch size={16} className="text-accent" />
            {t('organization.domainDetail.dependencies.outbound')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {deps.map(dep => (
              <div key={dep.dependencyId} className="px-4 py-3 flex items-center gap-3">
                <Server size={14} className="text-muted shrink-0" />
                <span className="text-sm font-medium text-heading">{dep.sourceServiceName}</span>
                <ArrowRight size={14} className="text-muted shrink-0" />
                <span className="text-sm font-medium text-heading">{dep.targetServiceName}</span>
                <Link
                  to={`/governance/domains/${dep.targetDomainId}`}
                  className="text-xs text-accent hover:text-accent/80 transition-colors"
                >
                  {dep.targetDomainName}
                </Link>
                <Badge variant="info">{dep.dependencyType}</Badge>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Inbound placeholder */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <GitBranch size={16} className="text-blue-500" />
            {t('organization.domainDetail.dependencies.inbound')}
          </h2>
        </CardHeader>
        <CardBody>
          <p className="text-sm text-muted text-center py-4">{t('organization.domainDetail.noDependencies')}</p>
        </CardBody>
      </Card>
    </div>
  );
}
