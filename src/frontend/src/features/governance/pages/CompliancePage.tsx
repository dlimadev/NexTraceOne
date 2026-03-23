import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Scale, Search, ShieldCheck, ShieldAlert, AlertCircle, CheckCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import type { ComplianceSummaryResponse, ComplianceStatusType, CompliancePackRowDto } from '../../../types';
import { organizationGovernanceApi } from '../api/organizationGovernance';



type ComplianceFilter = 'all' | 'NonCompliant' | 'PartiallyCompliant';

const statusBadgeVariant = (status: ComplianceStatusType): 'success' | 'warning' | 'danger' | 'default' => {
  switch (status) {
    case 'Compliant': return 'success';
    case 'PartiallyCompliant': return 'warning';
    case 'NonCompliant': return 'danger';
    default: return 'default';
  }
};

/**
 * Página de Compliance — conformidade técnico-operacional e cobertura de governança.
 * Parte do módulo Governance do NexTraceOne.
 */
export function CompliancePage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<ComplianceFilter>('all');
  const [search, setSearch] = useState('');
  const [data, setData] = useState<ComplianceSummaryResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setLoading(true);
    setError(null);
    organizationGovernanceApi.getComplianceSummary()
      .then((d) => { if (!cancelled) { setData(d); setLoading(false); } })
      .catch((err) => { if (!cancelled) { setError(err.message || t('common.errorLoading')); setLoading(false); } });
    return () => { cancelled = true; };
  }, [t]);

  if (loading) {
    return <PageContainer><PageLoadingState /></PageContainer>;
  }

  if (error || !data) {
    return <PageContainer><PageErrorState message={error ?? undefined} /></PageContainer>;
  }

  const d = data;

  const filteredGaps = d.packs.filter((gap: CompliancePackRowDto) => {
    if (filter === 'NonCompliant' && gap.status !== 'NonCompliant') return false;
    if (filter === 'PartiallyCompliant' && gap.status !== 'PartiallyCompliant') return false;
    if (search) {
      const q = search.toLowerCase();
      return gap.packName.toLowerCase().includes(q)
        || gap.category.toLowerCase().includes(q)
        || gap.packStatus.toLowerCase().includes(q);
    }
    return true;
  });

  const scoreColor = d.overallScore >= 80 ? 'text-success' : d.overallScore >= 60 ? 'text-amber-400' : 'text-critical';
  const percent = (value: number, total: number) => (total > 0 ? Math.round((value / total) * 100) : 0);

  const coverageItems = [
    { key: 'compliant', label: t('governance.compliance.compliant'), value: percent(d.compliantCount, d.totalPacksAssessed) },
    { key: 'partiallyCompliant', label: t('governance.compliance.partiallyCompliant'), value: percent(d.partiallyCompliantCount, d.totalPacksAssessed) },
    { key: 'nonCompliant', label: t('governance.compliance.nonCompliant'), value: percent(d.nonCompliantCount, d.totalPacksAssessed) },
    { key: 'completedRollouts', label: t('governance.compliance.rolloutCompletion', 'Rollout completion'), value: percent(d.completedRollouts, d.totalRollouts) },
    { key: 'failedRollouts', label: t('governance.compliance.rolloutFailure', 'Rollout failure'), value: percent(d.failedRollouts, d.totalRollouts) },
    { key: 'approvedWaivers', label: t('governance.compliance.waiverApproval', 'Waiver approval'), value: percent(d.approvedWaivers, d.totalWaivers) },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.complianceTitle')}
        subtitle={t('governance.complianceSubtitle')}
      />

      {/* Score + Stats */}
      <PageSection>
        <ContentGrid columns={4}>
          <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center col-span-2 md:col-span-1">
            <p className="text-xs text-muted mb-1">{t('governance.compliance.overallScore')}</p>
            <p className={`text-4xl font-bold ${scoreColor}`}>{d.overallScore}%</p>
          </div>
          <StatCard title={t('governance.compliance.totalAssessed')} value={d.totalPacksAssessed} icon={<Scale size={20} />} color="text-accent" />
          <StatCard title={t('governance.compliance.compliant')} value={d.compliantCount} icon={<CheckCircle size={20} />} color="text-emerald-500" />
          <StatCard title={t('governance.compliance.partiallyCompliant')} value={d.partiallyCompliantCount} icon={<AlertCircle size={20} />} color="text-amber-500" />
          <StatCard title={t('governance.compliance.nonCompliant')} value={d.nonCompliantCount} icon={<ShieldAlert size={20} />} color="text-critical" />
        </ContentGrid>
      </PageSection>

      {/* Coverage Indicators */}
      <PageSection>
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <ShieldCheck size={16} className="text-accent" />
              {t('governance.compliance.coverageIndicators')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {coverageItems.map(item => {
                const barColor = item.value >= 80 ? 'bg-emerald-500' : item.value >= 60 ? 'bg-amber-500' : 'bg-critical';
                return (
                  <div key={item.key}>
                    <div className="flex items-center justify-between mb-1">
                      <p className="text-xs text-muted">{t(`governance.compliance.${item.key}`)}</p>
                      <p className="text-xs font-medium text-heading">{item.value}%</p>
                    </div>
                    <div className="w-full bg-surface rounded-full h-2">
                      <div
                        className={`${barColor} rounded-full h-2 transition-all`}
                        style={{ width: `${item.value}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Filters + Gaps list */}
      <PageSection>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('governance.compliance.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          {(['all', 'NonCompliant', 'PartiallyCompliant'] as ComplianceFilter[]).map(f => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
                filter === f
                  ? 'bg-accent/10 text-accent border-accent/30'
                  : 'bg-surface text-muted border-edge hover:text-body'
              }`}
            >
              {f === 'all'
                ? t('governance.compliance.filterAll')
                : f === 'NonCompliant'
                  ? t('governance.compliance.filterNonCompliant')
                  : t('governance.compliance.filterPartially')}
            </button>
          ))}
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <ShieldAlert size={16} className="text-accent" />
              {t('governance.compliance.gaps')}
            </h2>
            <p className="text-xs text-muted mt-1">{t('governance.compliance.gapsDescription')}</p>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {filteredGaps.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                filteredGaps.map((gap: CompliancePackRowDto) => (
                  <div key={gap.packId} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 mb-1 flex-wrap">
                        <span className="text-sm font-medium text-heading">{gap.packName}</span>
                        <Badge variant={statusBadgeVariant(gap.status)}>
                          {t(`governance.compliance.status.${gap.status}`)}
                        </Badge>
                        <Badge variant="default">{gap.packStatus}</Badge>
                      </div>
                      <p className="text-xs text-muted">
                        {gap.category} · {t('governance.compliance.rollouts', 'Rollouts')}: {gap.completedRollouts}/{gap.rolloutCount} · {t('governance.compliance.waivers', 'Waivers')}: {gap.approvedWaivers}/{gap.pendingWaivers + gap.approvedWaivers}
                      </p>
                    </div>
                    <div className="hidden md:flex items-center gap-3 text-xs text-muted shrink-0">
                      <span className="w-28 truncate">{gap.category}</span>
                      <span className="w-24 text-right">{gap.failedRollouts}</span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
