import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Package, Search, FileText, Shield, Zap, Bot, AlertTriangle,
  Activity, Settings, CheckCircle, Archive,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';

/**
 * Tipos locais para Governance Packs — alinhados com o backend.
 */
type PackCategory = 'Contracts' | 'SourceOfTruth' | 'Changes' | 'Incidents' | 'AIGovernance' | 'Reliability' | 'Operations';
type PackStatus = 'Draft' | 'Published' | 'Deprecated';

interface GovernancePack {
  packId: string;
  name: string;
  displayName: string;
  description: string;
  category: PackCategory;
  status: PackStatus;
  version: string;
  scopeCount: number;
  ruleCount: number;
  createdAt: string;
}

/**
 * Dados simulados de governance packs — alinhados com o backend.
 * Em produção, virão da API /api/v1/governance/packs.
 */
const mockPacks: GovernancePack[] = [
  {
    packId: 'contracts-baseline',
    name: 'contracts-baseline',
    displayName: 'Contracts Baseline',
    description: 'Baseline governance rules for API and event contract management, versioning, and compatibility',
    category: 'Contracts',
    status: 'Published',
    version: '2.1.0',
    scopeCount: 12,
    ruleCount: 8,
    createdAt: new Date(Date.now() - 120 * 86400000).toISOString(),
  },
  {
    packId: 'source-of-truth-standards',
    name: 'source-of-truth-standards',
    displayName: 'Source of Truth Standards',
    description: 'Standards for service catalog completeness, ownership, and documentation as source of truth',
    category: 'SourceOfTruth',
    status: 'Published',
    version: '1.3.0',
    scopeCount: 8,
    ruleCount: 6,
    createdAt: new Date(Date.now() - 90 * 86400000).toISOString(),
  },
  {
    packId: 'change-governance-pack',
    name: 'change-governance-pack',
    displayName: 'Change Governance Pack',
    description: 'Production change confidence rules including blast radius assessment and validation gates',
    category: 'Changes',
    status: 'Published',
    version: '3.0.0',
    scopeCount: 15,
    ruleCount: 10,
    createdAt: new Date(Date.now() - 150 * 86400000).toISOString(),
  },
  {
    packId: 'ai-usage-policy',
    name: 'ai-usage-policy',
    displayName: 'AI Usage Policy',
    description: 'Governance rules for AI model usage, token budgets, prompt auditing, and external AI integrations',
    category: 'AIGovernance',
    status: 'Draft',
    version: '0.9.0',
    scopeCount: 5,
    ruleCount: 7,
    createdAt: new Date(Date.now() - 20 * 86400000).toISOString(),
  },
  {
    packId: 'operational-readiness-pack',
    name: 'operational-readiness-pack',
    displayName: 'Operational Readiness Pack',
    description: 'Operational readiness standards for runbooks, incident response, monitoring, and reliability',
    category: 'Operations',
    status: 'Deprecated',
    version: '1.0.0',
    scopeCount: 10,
    ruleCount: 5,
    createdAt: new Date(Date.now() - 200 * 86400000).toISOString(),
  },
];

type CategoryFilter = 'all' | PackCategory;
type StatusFilter = 'all' | PackStatus;

const statusBadge = (st: PackStatus): 'success' | 'warning' | 'default' => {
  switch (st) {
    case 'Published': return 'success';
    case 'Draft': return 'warning';
    case 'Deprecated': return 'default';
  }
};

const categoryIcon = (cat: PackCategory) => {
  switch (cat) {
    case 'Contracts': return <FileText size={14} />;
    case 'SourceOfTruth': return <Shield size={14} />;
    case 'Changes': return <Zap size={14} />;
    case 'Incidents': return <AlertTriangle size={14} />;
    case 'AIGovernance': return <Bot size={14} />;
    case 'Reliability': return <Activity size={14} />;
    case 'Operations': return <Settings size={14} />;
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

  const totalPacks = mockPacks.length;
  const publishedCount = mockPacks.filter(p => p.status === 'Published').length;
  const draftCount = mockPacks.filter(p => p.status === 'Draft').length;
  const deprecatedCount = mockPacks.filter(p => p.status === 'Deprecated').length;

  const filtered = mockPacks.filter(p => {
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
        {(['all', 'Draft', 'Published', 'Deprecated'] as StatusFilter[]).map(f => (
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
                    <span className="text-xs text-faded font-mono">v{pack.version}</span>
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
    </PageContainer>
  );
}
