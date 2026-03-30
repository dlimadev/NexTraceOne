import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Award,
  ShieldCheck,
  AlertTriangle,
  FileText,
  GitBranch,
  Activity,
  BookOpen,
  Users,
  CheckCircle2,
  XCircle,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { serviceCatalogApi } from '../api/serviceCatalog';
import type {
  MaturityDashboardResponse,
  OwnershipAuditResponse,
  ServiceMaturityItemDto,
  AuditFindingDto,
} from '../api/serviceCatalog';

type TabKey = 'maturity' | 'audit';

const maturityBadgeVariant = (level: string): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (level) {
    case 'Optimizing': return 'success';
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
};

const severityBadgeVariant = (severity: string): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (severity) {
    case 'critical': return 'danger';
    case 'high': return 'warning';
    case 'medium': return 'info';
    default: return 'default';
  }
};

const scoreBarColor = (score: number): string => {
  const pct = score * 100;
  if (pct >= 80) return 'bg-emerald-500';
  if (pct >= 60) return 'bg-amber-500';
  if (pct >= 40) return 'bg-orange-500';
  return 'bg-critical';
};

function BoolIcon({ value }: { value: boolean }) {
  return value
    ? <CheckCircle2 size={14} className="text-emerald-500" />
    : <XCircle size={14} className="text-muted/40" />;
}

/** Página de Service Maturity & Ownership Audit — governança por serviço. */
export function ServiceMaturityPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<TabKey>('maturity');
  const [teamFilter, setTeamFilter] = useState('');
  const [domainFilter, setDomainFilter] = useState('');

  const tabs: { key: TabKey; labelKey: string }[] = [
    { key: 'maturity', labelKey: 'serviceMaturity.tabs.maturity' },
    { key: 'audit', labelKey: 'serviceMaturity.tabs.audit' },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('serviceMaturity.title')}
        subtitle={t('serviceMaturity.subtitle')}
      />

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <input
          type="text"
          placeholder={t('serviceMaturity.filterTeam')}
          value={teamFilter}
          onChange={(e) => setTeamFilter(e.target.value)}
          className="px-3 py-1.5 text-xs rounded-md border border-edge bg-surface text-body placeholder:text-muted w-48"
        />
        <input
          type="text"
          placeholder={t('serviceMaturity.filterDomain')}
          value={domainFilter}
          onChange={(e) => setDomainFilter(e.target.value)}
          className="px-3 py-1.5 text-xs rounded-md border border-edge bg-surface text-body placeholder:text-muted w-48"
        />
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`px-4 py-2 text-xs font-medium transition-colors border-b-2 -mb-px ${
              activeTab === tab.key
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-body'
            }`}
          >
            {t(tab.labelKey)}
          </button>
        ))}
      </div>

      {activeTab === 'maturity' && (
        <MaturityTab teamName={teamFilter || undefined} domain={domainFilter || undefined} />
      )}
      {activeTab === 'audit' && (
        <AuditTab teamName={teamFilter || undefined} domain={domainFilter || undefined} />
      )}
    </PageContainer>
  );
}

// ── Maturity Tab ──────────────────────────────────────────────────────

function MaturityTab({ teamName, domain }: { teamName?: string; domain?: string }) {
  const { t } = useTranslation();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const { data, isLoading, error } = useQuery<MaturityDashboardResponse>({
    queryKey: ['maturity-dashboard', teamName, domain],
    queryFn: () => serviceCatalogApi.getMaturityDashboard({ teamName, domain }),
  });

  if (isLoading) return <PageLoadingState />;
  if (error || !data) return <PageErrorState />;

  const { summary, services } = data;

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
        <SummaryCard icon={<Award size={16} />} label={t('serviceMaturity.totalServices')} value={summary.totalServices} />
        <SummaryCard icon={<Activity size={16} />} label={t('serviceMaturity.avgScore')} value={`${Math.round(summary.averageScore * 100)}%`} />
        <SummaryCard icon={<Users size={16} />} label={t('serviceMaturity.withoutOwnership')} value={summary.withoutOwnership} variant={summary.withoutOwnership > 0 ? 'warning' : 'default'} />
        <SummaryCard icon={<FileText size={16} />} label={t('serviceMaturity.withoutContracts')} value={summary.withoutContracts} variant={summary.withoutContracts > 0 ? 'warning' : 'default'} />
        <SummaryCard icon={<BookOpen size={16} />} label={t('serviceMaturity.withoutDocs')} value={summary.withoutDocumentation} variant={summary.withoutDocumentation > 0 ? 'warning' : 'default'} />
        <SummaryCard icon={<ShieldCheck size={16} />} label={t('serviceMaturity.withoutRunbooks')} value={summary.withoutRunbooks} variant={summary.withoutRunbooks > 0 ? 'warning' : 'default'} />
      </div>

      {/* Level Distribution */}
      <Card>
        <CardHeader>
          <span className="text-sm font-semibold text-heading">{t('serviceMaturity.levelDistribution')}</span>
        </CardHeader>
        <CardBody>
          <div className="flex items-end gap-3 h-24">
            {(['Initial', 'Developing', 'Defined', 'Managed', 'Optimizing'] as const).map(level => {
              const count = level === 'Initial' ? summary.initial
                : level === 'Developing' ? summary.developing
                : level === 'Defined' ? summary.defined
                : level === 'Managed' ? summary.managed
                : summary.optimizing;
              const pct = summary.totalServices > 0 ? (count / summary.totalServices) * 100 : 0;
              return (
                <div key={level} className="flex flex-col items-center flex-1">
                  <span className="text-xs text-muted mb-1">{count}</span>
                  <div className="w-full bg-surface rounded" style={{ height: `${Math.max(pct, 4)}%` }}>
                    <div className={`w-full h-full rounded ${
                      level === 'Optimizing' ? 'bg-emerald-500'
                      : level === 'Managed' ? 'bg-emerald-400'
                      : level === 'Defined' ? 'bg-blue-400'
                      : level === 'Developing' ? 'bg-amber-400'
                      : 'bg-critical'
                    }`} />
                  </div>
                  <span className="text-[10px] text-muted mt-1">{t(`serviceMaturity.level.${level}`)}</span>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* Service List */}
      <Card>
        <CardHeader>
          <span className="text-sm font-semibold text-heading">{t('serviceMaturity.serviceList')}</span>
        </CardHeader>
        <CardBody>
          {services.length === 0 ? (
            <p className="text-sm text-muted text-center py-8">{t('serviceMaturity.noServices')}</p>
          ) : (
            <div className="divide-y divide-edge">
              {services.map((svc: ServiceMaturityItemDto) => (
                <div key={svc.serviceId} className="py-3">
                  <button
                    onClick={() => setExpandedId(expandedId === svc.serviceId ? null : svc.serviceId)}
                    className="w-full flex items-center gap-3 text-left"
                  >
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-heading truncate">{svc.displayName || svc.serviceName}</span>
                        <Badge variant={maturityBadgeVariant(svc.level)}>
                          {t(`serviceMaturity.level.${svc.level}`)}
                        </Badge>
                      </div>
                      <div className="flex items-center gap-3 mt-0.5">
                        <span className="text-[11px] text-muted">{svc.teamName || '—'}</span>
                        <span className="text-[11px] text-muted">{svc.domain}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-24 bg-surface rounded-full h-1.5">
                        <div
                          className={`${scoreBarColor(svc.overallScore)} rounded-full h-1.5 transition-all`}
                          style={{ width: `${svc.overallScore * 100}%` }}
                        />
                      </div>
                      <span className="text-xs text-muted w-8 text-right">{Math.round(svc.overallScore * 100)}%</span>
                      {expandedId === svc.serviceId ? <ChevronUp size={14} className="text-muted" /> : <ChevronDown size={14} className="text-muted" />}
                    </div>
                  </button>
                  {expandedId === svc.serviceId && (
                    <div className="mt-3 grid grid-cols-3 md:grid-cols-6 gap-3 pl-2">
                      <DimensionPill icon={<Users size={12} />} label={t('serviceMaturity.dim.ownership')} ok={svc.hasOwnership} />
                      <DimensionPill icon={<FileText size={12} />} label={t('serviceMaturity.dim.contracts')} ok={svc.hasContracts} />
                      <DimensionPill icon={<BookOpen size={12} />} label={t('serviceMaturity.dim.documentation')} ok={svc.hasDocumentation} />
                      <DimensionPill icon={<GitBranch size={12} />} label={t('serviceMaturity.dim.repository')} ok={svc.hasRepository} />
                      <DimensionPill icon={<Activity size={12} />} label={t('serviceMaturity.dim.monitoring')} ok={svc.hasMonitoring} />
                      <DimensionPill icon={<ShieldCheck size={12} />} label={t('serviceMaturity.dim.runbook')} ok={svc.hasRunbook} />
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}

// ── Audit Tab ────────────────────────────────────────────────────────

function AuditTab({ teamName, domain }: { teamName?: string; domain?: string }) {
  const { t } = useTranslation();

  const { data, isLoading, error } = useQuery<OwnershipAuditResponse>({
    queryKey: ['ownership-audit', teamName, domain],
    queryFn: () => serviceCatalogApi.getOwnershipAudit({ teamName, domain }),
  });

  if (isLoading) return <PageLoadingState />;
  if (error || !data) return <PageErrorState />;

  const { summary, findings } = data;

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3">
        <SummaryCard icon={<ShieldCheck size={16} />} label={t('serviceMaturity.audit.totalAudited')} value={summary.totalServicesAudited} />
        <SummaryCard icon={<CheckCircle2 size={16} />} label={t('serviceMaturity.audit.healthy')} value={summary.healthyServices} variant="success" />
        <SummaryCard icon={<AlertTriangle size={16} />} label={t('serviceMaturity.audit.withIssues')} value={summary.servicesWithIssues} variant={summary.servicesWithIssues > 0 ? 'warning' : 'default'} />
        <SummaryCard icon={<XCircle size={16} />} label={t('serviceMaturity.audit.critical')} value={summary.criticalFindings} variant={summary.criticalFindings > 0 ? 'danger' : 'default'} />
        <SummaryCard icon={<Users size={16} />} label={t('serviceMaturity.audit.noTeam')} value={summary.withoutTeam} variant={summary.withoutTeam > 0 ? 'danger' : 'default'} />
        <SummaryCard icon={<FileText size={16} />} label={t('serviceMaturity.audit.noContracts')} value={summary.apisWithoutContracts} variant={summary.apisWithoutContracts > 0 ? 'warning' : 'default'} />
      </div>

      {/* Findings List */}
      <Card>
        <CardHeader>
          <span className="text-sm font-semibold text-heading">{t('serviceMaturity.audit.findings')}</span>
        </CardHeader>
        <CardBody>
          {findings.length === 0 ? (
            <div className="text-center py-8">
              <CheckCircle2 size={32} className="mx-auto mb-2 text-emerald-500 opacity-60" />
              <p className="text-sm text-muted">{t('serviceMaturity.audit.noFindings')}</p>
            </div>
          ) : (
            <div className="divide-y divide-edge">
              {findings.map((f: AuditFindingDto) => (
                <div key={f.serviceId} className="py-3">
                  <div className="flex items-center gap-2 mb-1">
                    <Badge variant={severityBadgeVariant(f.severity)}>
                      {f.severity.toUpperCase()}
                    </Badge>
                    <span className="text-sm font-medium text-heading">{f.displayName || f.serviceName}</span>
                    <span className="text-[11px] text-muted">{f.teamName || '—'}</span>
                    <span className="text-[11px] text-muted">{f.domain}</span>
                  </div>
                  <div className="flex flex-wrap gap-1.5 pl-1">
                    {f.findings.map((finding, idx) => (
                      <span key={idx} className="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] rounded bg-surface border border-edge text-muted">
                        <AlertTriangle size={10} className="text-amber-500" />
                        {t(`serviceMaturity.audit.finding.${finding.split(':')[0]}`, { count: finding.split(':')[1] ?? '' })}
                      </span>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}

// ── Shared Components ────────────────────────────────────────────────

function SummaryCard({ icon, label, value, variant = 'default' }: {
  icon: React.ReactNode;
  label: string;
  value: string | number;
  variant?: 'default' | 'success' | 'warning' | 'danger';
}) {
  const colorClass = variant === 'success' ? 'text-emerald-500'
    : variant === 'warning' ? 'text-amber-500'
    : variant === 'danger' ? 'text-critical'
    : 'text-accent';

  return (
    <div className="bg-surface border border-edge rounded-lg p-3">
      <div className={`mb-1 ${colorClass}`}>{icon}</div>
      <p className="text-lg font-semibold text-heading">{value}</p>
      <p className="text-[11px] text-muted">{label}</p>
    </div>
  );
}

function DimensionPill({ icon, label, ok }: { icon: React.ReactNode; label: string; ok: boolean }) {
  return (
    <div className={`flex items-center gap-1.5 px-2 py-1 rounded text-[11px] border ${
      ok
        ? 'bg-emerald-500/5 border-emerald-500/20 text-emerald-600'
        : 'bg-surface border-edge text-muted'
    }`}>
      {icon}
      <span>{label}</span>
      <BoolIcon value={ok} />
    </div>
  );
}

export { ServiceMaturityPage as default };
