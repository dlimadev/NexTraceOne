import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  Users, Server, FileText, Shield, GitBranch,
  ArrowRight, TrendingUp, Minus, AlertTriangle, Activity,
  CheckCircle, Building2, Calendar, ArrowLeft,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';
import { PageContainer } from '../../../components/shell';

type MaturityLevel = 'Initial' | 'Developing' | 'Defined' | 'Managed' | 'Optimizing';
type TabId = 'overview' | 'services' | 'contracts' | 'governance' | 'dependencies';

const mockTeamDetail = {
  teamId: 'team-platform',
  name: 'platform',
  displayName: 'Platform Engineering',
  description: 'Core platform services and infrastructure',
  status: 'Active',
  parentOrganizationUnit: 'Engineering',
  serviceCount: 8,
  contractCount: 12,
  activeIncidentCount: 2,
  recentChangeCount: 15,
  maturityLevel: 'Managed' as MaturityLevel,
  reliabilityScore: 87.5,
  services: [
    { serviceId: 'svc-api-gw', name: 'API Gateway', domain: 'Platform', criticality: 'Critical', ownershipType: 'Primary' },
    { serviceId: 'svc-auth', name: 'Auth Service', domain: 'Identity', criticality: 'Critical', ownershipType: 'Primary' },
    { serviceId: 'svc-config', name: 'Config Service', domain: 'Platform', criticality: 'High', ownershipType: 'Primary' },
  ],
  contracts: [
    { contractId: 'ctr-auth-api', name: 'Auth API v2', type: 'REST', version: '2.1.0', status: 'Published' },
    { contractId: 'ctr-config-api', name: 'Config API v1', type: 'REST', version: '1.0.3', status: 'Published' },
  ],
  crossTeamDependencies: [
    { dependencyId: 'dep-1', sourceServiceName: 'API Gateway', targetServiceName: 'Order Processor', targetTeamId: 'team-commerce', targetTeamName: 'Commerce', dependencyType: 'REST' },
    { dependencyId: 'dep-2', sourceServiceName: 'Auth Service', targetServiceName: 'User Service', targetTeamId: 'team-identity', targetTeamName: 'Identity & Access', dependencyType: 'gRPC' },
  ],
  createdAt: '2024-01-15T10:00:00Z',
};

const mockGovernanceSummary = {
  entityId: 'team-platform',
  entityName: 'Platform Engineering',
  overallMaturity: 'Managed' as MaturityLevel,
  ownershipCoverage: 0.92,
  contractCoverage: 0.85,
  documentationCoverage: 0.78,
  reliabilityScore: 87.5,
  openRiskCount: 3,
  policyViolationCount: 1,
  dimensions: [
    { dimension: 'Ownership', level: 'Managed' as MaturityLevel, score: 92, trend: 'Improving' as const },
    { dimension: 'Contracts', level: 'Defined' as MaturityLevel, score: 85, trend: 'Stable' as const },
    { dimension: 'Documentation', level: 'Developing' as MaturityLevel, score: 78, trend: 'Improving' as const },
    { dimension: 'Change Governance', level: 'Managed' as MaturityLevel, score: 88, trend: 'Stable' as const },
    { dimension: 'Incident Response', level: 'Defined' as MaturityLevel, score: 82, trend: 'Improving' as const },
    { dimension: 'Reliability', level: 'Managed' as MaturityLevel, score: 87, trend: 'Stable' as const },
  ],
};

const maturityBadgeVariant = (level: MaturityLevel): 'success' | 'info' | 'warning' | 'danger' => {
  switch (level) {
    case 'Optimizing':
    case 'Managed':
      return 'success';
    case 'Defined':
      return 'info';
    case 'Developing':
      return 'warning';
    case 'Initial':
      return 'danger';
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

export function TeamDetailPage() {
  const { t } = useTranslation();
  const { teamId } = useParams<{ teamId: string }>();
  const [activeTab, setActiveTab] = useState<TabId>('overview');

  // In a real app, fetch by teamId — for now use mock
  const team = { ...mockTeamDetail, teamId: teamId ?? mockTeamDetail.teamId };
  const gov = mockGovernanceSummary;

  const tabs: { id: TabId; labelKey: string; icon: React.ReactNode }[] = [
    { id: 'overview', labelKey: 'organization.teamDetail.tabs.overview', icon: <Users size={16} /> },
    { id: 'services', labelKey: 'organization.teamDetail.tabs.services', icon: <Server size={16} /> },
    { id: 'contracts', labelKey: 'organization.teamDetail.tabs.contracts', icon: <FileText size={16} /> },
    { id: 'governance', labelKey: 'organization.teamDetail.tabs.governance', icon: <Shield size={16} /> },
    { id: 'dependencies', labelKey: 'organization.teamDetail.tabs.dependencies', icon: <GitBranch size={16} /> },
  ];

  const formatDate = (iso: string) => new Date(iso).toLocaleDateString();
  const formatPct = (v: number) => `${Math.round(v * 100)}%`;

  return (
    <PageContainer>
      {/* Back link */}
      <Link to="/organization/teams" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
        <ArrowLeft size={14} />
        {t('organization.teams.title')}
      </Link>

      <ModuleHeader
        titleKey="organization.teamDetail.title"
        subtitleKey="organization.teamDetail.subtitle"
        actions={
          <div className="flex items-center gap-2">
            <Badge variant={statusBadgeVariant(team.status)}>
              {t(`organization.teams.status.${team.status}`)}
            </Badge>
            <Badge variant={maturityBadgeVariant(team.maturityLevel)}>
              {t(`organization.teams.maturityLevel.${team.maturityLevel}`)}
            </Badge>
          </div>
        }
      />

      {/* Team info header */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-wrap items-start gap-6">
            <div className="flex-1 min-w-0">
              <h2 className="text-lg font-bold text-heading">{team.displayName}</h2>
              <p className="text-sm text-muted mt-1">{team.description}</p>
              <div className="flex flex-wrap items-center gap-4 mt-3 text-xs text-muted">
                <span className="flex items-center gap-1">
                  <Building2 size={12} />
                  {t('organization.teamDetail.orgUnit')}: {team.parentOrganizationUnit}
                </span>
                <span className="flex items-center gap-1">
                  <Calendar size={12} />
                  {t('organization.teamDetail.createdAt')}: {formatDate(team.createdAt)}
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
      {activeTab === 'overview' && <OverviewTab team={team} t={t} />}
      {activeTab === 'services' && <ServicesTab services={team.services} t={t} />}
      {activeTab === 'contracts' && <ContractsTab contracts={team.contracts} t={t} />}
      {activeTab === 'governance' && <GovernanceTab gov={gov} t={t} formatPct={formatPct} />}
      {activeTab === 'dependencies' && <DependenciesTab deps={team.crossTeamDependencies} t={t} />}
    </PageContainer>
  );
}

/* ─── Overview Tab ─── */

function OverviewTab({ team, t }: { team: typeof mockTeamDetail; t: (key: string) => string }) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      <StatCard title={t('organization.teamDetail.services')} value={team.serviceCount} icon={<Server size={20} />} color="text-accent" />
      <StatCard title={t('organization.teamDetail.contracts')} value={team.contractCount} icon={<FileText size={20} />} color="text-blue-500" />
      <StatCard title={t('organization.teamDetail.activeIncidents')} value={team.activeIncidentCount} icon={<AlertTriangle size={20} />} color="text-critical" />
      <StatCard title={t('organization.teamDetail.reliabilityScore')} value={`${team.reliabilityScore}%`} icon={<Activity size={20} />} color="text-success" />
    </div>
  );
}

/* ─── Services Tab ─── */

function ServicesTab({ services, t }: { services: typeof mockTeamDetail.services; t: (key: string) => string }) {
  if (services.length === 0) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.teamDetail.noServices')}</div>;
  }

  return (
    <Card>
      <CardHeader>
        <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
          <Server size={16} className="text-accent" />
          {t('organization.teamDetail.services')}
        </h2>
      </CardHeader>
      <CardBody className="p-0">
        <div className="divide-y divide-edge">
          {services.map(svc => (
            <Link key={svc.serviceId} to={`/services/${svc.serviceId}`} className="px-4 py-3 flex items-center gap-3 hover:bg-hover transition-colors">
              <Server size={14} className="text-muted shrink-0" />
              <span className="text-sm font-medium text-heading flex-1 truncate">{svc.name}</span>
              <span className="text-xs text-muted hidden md:inline">{svc.domain}</span>
              <Badge variant={criticalityVariant(svc.criticality)}>{svc.criticality}</Badge>
              <span className="text-xs text-muted hidden md:inline">{svc.ownershipType}</span>
              <ArrowRight size={14} className="text-muted" />
            </Link>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

/* ─── Contracts Tab ─── */

function ContractsTab({ contracts, t }: { contracts: typeof mockTeamDetail.contracts; t: (key: string) => string }) {
  if (contracts.length === 0) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.teamDetail.noContracts')}</div>;
  }

  return (
    <Card>
      <CardHeader>
        <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
          <FileText size={16} className="text-accent" />
          {t('organization.teamDetail.contracts')}
        </h2>
      </CardHeader>
      <CardBody className="p-0">
        <div className="divide-y divide-edge">
          {contracts.map(ctr => (
            <div key={ctr.contractId} className="px-4 py-3 flex items-center gap-3">
              <FileText size={14} className="text-muted shrink-0" />
              <span className="text-sm font-medium text-heading flex-1 truncate">{ctr.name}</span>
              <Badge variant="info">{ctr.type}</Badge>
              <span className="text-xs font-mono text-muted">{ctr.version}</span>
              <Badge variant={ctr.status === 'Published' ? 'success' : 'default'}>{ctr.status}</Badge>
            </div>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

/* ─── Governance Tab ─── */

function GovernanceTab({ gov, t, formatPct }: { gov: typeof mockGovernanceSummary; t: (key: string) => string; formatPct: (v: number) => string }) {
  return (
    <div className="space-y-6">
      {/* Summary */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Shield size={16} className="text-accent" />
            {t('organization.teamDetail.governanceSummary')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="flex flex-wrap items-center gap-4 mb-4">
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted">{t('organization.teamDetail.overallMaturity')}:</span>
              <Badge variant={maturityBadgeVariant(gov.overallMaturity)}>
                {t(`organization.teams.maturityLevel.${gov.overallMaturity}`)}
              </Badge>
            </div>
            <div className="flex items-center gap-4 text-sm">
              <span className="flex items-center gap-1 text-muted">
                <AlertTriangle size={14} className="text-warning" />
                {t('organization.teamDetail.risks')}: <span className="text-heading font-medium">{gov.openRiskCount}</span>
              </span>
              <span className="flex items-center gap-1 text-muted">
                <Shield size={14} className="text-critical" />
                {t('organization.teamDetail.policyViolations')}: <span className="text-heading font-medium">{gov.policyViolationCount}</span>
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
                  <span className="text-muted">{t(`organization.teamDetail.coverage.${cov.key}`)}</span>
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
            {t('organization.teamDetail.dimensions.dimension')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {gov.dimensions.map(dim => (
              <div key={dim.dimension} className="px-4 py-3 flex items-center gap-3">
                <CheckCircle size={14} className={dim.score >= 85 ? 'text-success' : dim.score >= 70 ? 'text-warning' : 'text-critical'} />
                <span className="text-sm font-medium text-heading flex-1 truncate">{dim.dimension}</span>
                <Badge variant={maturityBadgeVariant(dim.level)}>
                  {t(`organization.teams.maturityLevel.${dim.level}`)}
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

function DependenciesTab({ deps, t }: { deps: typeof mockTeamDetail.crossTeamDependencies; t: (key: string) => string }) {
  if (deps.length === 0) {
    return <div className="p-8 text-center text-muted text-sm">{t('organization.teamDetail.noDependencies')}</div>;
  }

  return (
    <div className="space-y-6">
      {/* Outbound */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <GitBranch size={16} className="text-accent" />
            {t('organization.teamDetail.dependencies.outbound')}
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
                  to={`/organization/teams/${dep.targetTeamId}`}
                  className="text-xs text-accent hover:text-accent/80 transition-colors"
                >
                  {dep.targetTeamName}
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
            {t('organization.teamDetail.dependencies.inbound')}
          </h2>
        </CardHeader>
        <CardBody>
          <p className="text-sm text-muted text-center py-4">{t('organization.teamDetail.noDependencies')}</p>
        </CardBody>
      </Card>
    </div>
  );
}
