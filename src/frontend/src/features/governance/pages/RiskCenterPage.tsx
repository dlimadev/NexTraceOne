import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ShieldAlert, Search, AlertTriangle, AlertCircle,
  Shield, CheckCircle, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import type { RiskSummaryResponse, RiskLevel } from '../../../types';
import { organizationGovernanceApi } from '../api/organizationGovernance';



type RiskFilter = 'all' | RiskLevel;

const riskBadgeVariant = (level: RiskLevel): 'success' | 'warning' | 'danger' | 'default' => {
  switch (level) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'warning';
    case 'Low': return 'success';
    default: return 'default';
  }
};

const riskIcon = (level: RiskLevel) => {
  switch (level) {
    case 'Critical': return <ShieldAlert size={14} className="text-critical" />;
    case 'High': return <AlertTriangle size={14} className="text-orange-400" />;
    case 'Medium': return <AlertCircle size={14} className="text-amber-400" />;
    case 'Low': return <CheckCircle size={14} className="text-emerald-400" />;
    default: return <Shield size={14} className="text-muted" />;
  }
};

/**
 * Página de Risk Center — análise de risco operacional contextualizado por serviço e mudança.
 * Parte do módulo Governance do NexTraceOne.
 */
export function RiskCenterPage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<RiskFilter>('all');
  const [search, setSearch] = useState('');
  const [data, setData] = useState<RiskSummaryResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    organizationGovernanceApi.getRiskSummary()
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

  const filtered = d.indicators.filter(ind => {
    if (filter !== 'all' && ind.riskLevel !== filter) return false;
    if (search) {
      const q = search.toLowerCase();
      return ind.packName.toLowerCase().includes(q)
        || ind.category.toLowerCase().includes(q)
        || ind.dimensions.some((dim) => dim.explanation.toLowerCase().includes(q));
    }
    return true;
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.riskTitle')}
        subtitle={t('governance.riskSubtitle')}
      />

      {/* Stats */}
      <PageSection>
        <ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
          <StatCard title={t('governance.risk.totalAssessed')} value={d.totalPacksAssessed} icon={<Shield size={20} />} color="text-accent" />
          <StatCard title={t('governance.risk.critical')} value={d.criticalCount} icon={<ShieldAlert size={20} />} color="text-critical" />
          <StatCard title={t('governance.risk.high')} value={d.highCount} icon={<AlertTriangle size={20} />} color="text-orange-500" />
          <StatCard title={t('governance.risk.medium')} value={d.mediumCount} icon={<AlertCircle size={20} />} color="text-amber-500" />
          <StatCard title={t('governance.risk.low')} value={d.lowCount} icon={<CheckCircle size={20} />} color="text-emerald-500" />
        </ContentGrid>
      </PageSection>

      {/* Filters + Risk list */}
      <PageSection>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('governance.risk.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          {(['all', 'Critical', 'High', 'Medium', 'Low'] as RiskFilter[]).map(f => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
                filter === f
                  ? 'bg-accent/10 text-accent border-accent/30'
                  : 'bg-surface text-muted border-edge hover:text-body'
              }`}
            >
              {f === 'all' ? t('governance.risk.filterAll') : t(`governance.risk.filter${f}`)}
            </button>
          ))}
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <ShieldAlert size={16} className="text-accent" />
              {t('governance.risk.serviceRiskList')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {filtered.length === 0 ? (
                <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
              ) : (
                filtered.map(ind => (
                  <div key={ind.packId} className="px-4 py-3 hover:bg-hover transition-colors">
                    <div className="flex items-center gap-3 mb-2">
                      {riskIcon(ind.riskLevel)}
                      <span className="text-sm font-medium text-heading">{ind.packName}</span>
                      <Badge variant={riskBadgeVariant(ind.riskLevel)}>
                        {t(`governance.risk.level.${ind.riskLevel}`)}
                      </Badge>
                      <span className="hidden md:inline text-xs text-muted">{ind.category}</span>
                    </div>
                    <div className="flex flex-wrap gap-2 ml-7">
                      {ind.dimensions.map((dim, i) => (
                        <div key={i} className="text-xs">
                          <Badge variant={riskBadgeVariant(dim.level)} className="mr-1">
                            {t(`governance.risk.dimension.${dim.dimension}`)}
                          </Badge>
                          <span className="text-muted">{dim.explanation}</span>
                        </div>
                      ))}
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
