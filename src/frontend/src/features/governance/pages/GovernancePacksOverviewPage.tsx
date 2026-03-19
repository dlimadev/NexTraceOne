import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Package, Search, FileText, Shield, Zap, Bot, AlertTriangle,
  Activity, Settings, CheckCircle, Archive, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import type { GovernancePackSummary, GovernancePackCategory, GovernancePackStatus } from '../../../types';

type CategoryFilter = 'all' | GovernancePackCategory;
type StatusFilter = 'all' | GovernancePackStatus;

const statusBadge = (st: string): 'success' | 'warning' | 'default' => {
  switch (st) {
    case 'Published': return 'success';
    case 'Draft': return 'warning';
    case 'Deprecated':
    default: return 'default';
  }
};

const categoryIcon = (cat: string) => {
  switch (cat) {
    case 'Contracts': return <FileText size={14} />;
    case 'SourceOfTruth': return <Shield size={14} />;
    case 'Changes': return <Zap size={14} />;
    case 'Incidents': return <AlertTriangle size={14} />;
    case 'AIGovernance': return <Bot size={14} />;
    case 'Reliability': return <Activity size={14} />;
    case 'Operations':
    default: return <Settings size={14} />;
  }
};

/**
 * Página de visão geral dos Governance Packs — catálogo de pacotes de governança.
 * Parte do módulo Governance do NexTraceOne.
 */
export function GovernancePacksOverviewPage() {
  const { t } = useTranslation();
  const [categoryFilter, setCategoryFilter] = useState<CategoryFilter>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');
  const [packs, setPacks] = useState<GovernancePackSummary[]>([]);
  const [totalPacks, setTotalPacks] = useState(0);
  const [publishedCount, setPublishedCount] = useState(0);
  const [draftCount, setDraftCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    organizationGovernanceApi.listGovernancePacks()
      .then((data) => {
        if (!cancelled) {
          setPacks(data.packs);
          setTotalPacks(data.totalPacks);
          setPublishedCount(data.publishedCount);
          setDraftCount(data.draftCount);
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
  }, [t]);

  const deprecatedCount = packs.filter(p => p.status === 'Deprecated').length;

  const filtered = packs.filter(p => {
    if (categoryFilter !== 'all' && p.category !== categoryFilter) return false;
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return p.displayName.toLowerCase().includes(q)
        || p.name.toLowerCase().includes(q)
        || p.description.toLowerCase().includes(q);
    }
    return true;
  });

  const categories: { key: CategoryFilter; labelKey: string }[] = [
    { key: 'all', labelKey: 'governancePacks.filterAll' },
    { key: 'Contracts', labelKey: 'governancePacks.category.Contracts' },
    { key: 'SourceOfTruth', labelKey: 'governancePacks.category.SourceOfTruth' },
    { key: 'Changes', labelKey: 'governancePacks.category.Changes' },
    { key: 'Incidents', labelKey: 'governancePacks.category.Incidents' },
    { key: 'AIGovernance', labelKey: 'governancePacks.category.AIGovernance' },
    { key: 'Reliability', labelKey: 'governancePacks.category.Reliability' },
    { key: 'Operations', labelKey: 'governancePacks.category.Operations' },
  ];

  if (loading) {
    return (
      <PageContainer>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-heading">{t('governancePacks.title')}</h1>
          <p className="text-muted mt-1">{t('governancePacks.subtitle')}</p>
        </div>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (error) {
    return (
      <PageContainer>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-heading">{t('governancePacks.title')}</h1>
          <p className="text-muted mt-1">{t('governancePacks.subtitle')}</p>
        </div>
        <PageErrorState message={error} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governancePacks.title')}</h1>
        <p className="text-muted mt-1">{t('governancePacks.subtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governancePacks.totalPacks')} value={totalPacks} icon={<Package size={20} />} color="text-accent" />
        <StatCard title={t('governancePacks.published')} value={publishedCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('governancePacks.draft')} value={draftCount} icon={<FileText size={20} />} color="text-amber-500" />
        <StatCard title={t('governancePacks.deprecated')} value={deprecatedCount} icon={<Archive size={20} />} color="text-muted" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('governancePacks.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Draft', 'Published', 'Deprecated', 'Archived'] as StatusFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setStatusFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              statusFilter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {f === 'all' ? t('governancePacks.filterAll') : t(`governancePacks.status.${f}`)}
          </button>
        ))}
      </div>

      {/* Category filter */}
      <div className="flex flex-wrap items-center gap-2 mb-6">
        {categories.map(c => (
          <button
            key={c.key}
            onClick={() => setCategoryFilter(c.key)}
            className={`px-3 py-1 text-xs rounded-full border transition-colors ${
              categoryFilter === c.key
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(c.labelKey)}
          </button>
        ))}
      </div>

      {/* Pack grid */}
      {packs.length === 0 ? (
        <div className="p-8 text-center text-muted text-sm">{t('governancePacks.noPacks')}</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.length === 0 ? (
            <div className="col-span-full p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
          ) : (
            filtered.map(pack => (
              <Link key={pack.packId} to={`/governance/packs/${pack.packId}`} className="block">
                <Card className="h-full hover:border-accent/30 transition-colors">
                  <CardHeader>
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-muted">{categoryIcon(pack.category)}</span>
                      <span className="text-sm font-medium text-heading truncate">{pack.displayName}</span>
                    </div>
                    <div className="flex items-center gap-2 flex-wrap">
                      <Badge variant={statusBadge(pack.status)}>{t(`governancePacks.status.${pack.status}`)}</Badge>
                      <Badge variant="info">{t(categories.find(c => c.key === pack.category)?.labelKey ?? '')}</Badge>
                      <span className="text-xs text-faded font-mono">v{pack.currentVersion}</span>
                    </div>
                  </CardHeader>
                  <CardBody>
                    <p className="text-xs text-muted mb-3">{pack.description}</p>
                    <div className="flex items-center gap-4 text-xs text-faded">
                      <span>{t('governancePacks.scopes')}: {pack.scopeCount}</span>
                      <span>{t('governancePacks.rules')}: {pack.ruleCount}</span>
                    </div>
                  </CardBody>
                </Card>
              </Link>
            ))
          )}
        </div>
      )}
    </PageContainer>
  );
}
