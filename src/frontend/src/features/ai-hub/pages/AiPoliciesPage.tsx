import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ShieldCheck, Search, Shield, Lock, Unlock, Users,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';

interface Policy {
  id: string;
  name: string;
  description: string;
  scope: string;
  scopeValue: string;
  allowExternalAI: boolean;
  internalOnly: boolean;
  maxTokensPerRequest: number;
  isActive: boolean;
}

const mockPolicies: Policy[] = [
  { id: '1', name: 'Default Engineer Policy', description: 'Standard policy for engineers', scope: 'role', scopeValue: 'Engineer', allowExternalAI: false, internalOnly: true, maxTokensPerRequest: 4096, isActive: true },
  { id: '2', name: 'Tech Lead Extended', description: 'Extended access for tech leads', scope: 'role', scopeValue: 'Tech Lead', allowExternalAI: true, internalOnly: false, maxTokensPerRequest: 8192, isActive: true },
  { id: '3', name: 'Platform Admin Full', description: 'Full AI access for platform admins', scope: 'role', scopeValue: 'Platform Admin', allowExternalAI: true, internalOnly: false, maxTokensPerRequest: 16384, isActive: true },
  { id: '4', name: 'Auditor Read-Only', description: 'Audit trail access only', scope: 'role', scopeValue: 'Auditor', allowExternalAI: false, internalOnly: true, maxTokensPerRequest: 2048, isActive: true },
  { id: '5', name: 'Restricted Intern Policy', description: 'Limited access for interns', scope: 'group', scopeValue: 'Interns', allowExternalAI: false, internalOnly: true, maxTokensPerRequest: 1024, isActive: false },
];

type StatusFilter = 'all' | 'active' | 'inactive';

/**
 * Página de AI Policies — governança de acesso, tokens e modelos IA.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function AiPoliciesPage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');

  const filtered = mockPolicies.filter((p) => {
    if (filter === 'active' && !p.isActive) return false;
    if (filter === 'inactive' && p.isActive) return false;
    if (search && !p.name.toLowerCase().includes(search.toLowerCase()) && !p.scopeValue.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const totalActive = mockPolicies.filter((p) => p.isActive).length;
  const internalOnly = mockPolicies.filter((p) => p.internalOnly && p.isActive).length;
  const externalAllowed = mockPolicies.filter((p) => p.allowExternalAI && p.isActive).length;

  const filters: { key: StatusFilter; label: string }[] = [
    { key: 'all', label: t('aiHub.filterAll') },
    { key: 'active', label: t('aiHub.filterActive') },
    { key: 'inactive', label: t('aiHub.filterInactive') },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.policiesTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.policiesSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('aiHub.policiesTotalStat')} value={mockPolicies.length} icon={<ShieldCheck size={20} />} color="text-accent" />
        <StatCard title={t('aiHub.policiesActiveStat')} value={totalActive} icon={<Shield size={20} />} color="text-success" />
        <StatCard title={t('aiHub.policiesInternalOnlyStat')} value={internalOnly} icon={<Lock size={20} />} color="text-info" />
        <StatCard title={t('aiHub.policiesExternalAllowedStat')} value={externalAllowed} icon={<Unlock size={20} />} color="text-warning" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <div className="relative flex-1 min-w-[200px] max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            placeholder={t('aiHub.searchPolicies')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-3 py-2 rounded-md bg-surface border border-edge text-body text-sm placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
          />
        </div>
        <div className="flex gap-1.5">
          {filters.map((f) => (
            <button
              key={f.key}
              onClick={() => setFilter(f.key)}
              className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${filter === f.key ? 'bg-accent text-heading' : 'bg-elevated text-muted hover:text-body'}`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {/* Policy list */}
      <div className="space-y-3">
        {filtered.map((p) => (
          <Card key={p.id}>
            <CardBody>
              <div className="flex items-start justify-between gap-4">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="text-sm font-semibold text-heading truncate">{p.name}</h3>
                    <Badge variant={p.isActive ? 'success' : 'default'}>{p.isActive ? t('aiHub.statusActive') : t('aiHub.statusInactive')}</Badge>
                  </div>
                  <p className="text-xs text-muted mb-2">{p.description}</p>
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant="info"><Users size={10} className="mr-1 inline" />{p.scope}: {p.scopeValue}</Badge>
                    {p.internalOnly && <Badge variant="default"><Lock size={10} className="mr-1 inline" />{t('aiHub.internalOnly')}</Badge>}
                    {p.allowExternalAI && <Badge variant="warning"><Unlock size={10} className="mr-1 inline" />{t('aiHub.externalAllowed')}</Badge>}
                    <span className="text-xs text-muted">{t('aiHub.maxTokens')}: {p.maxTokensPerRequest.toLocaleString()}</span>
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>
        ))}
        {filtered.length === 0 && (
          <Card>
            <CardBody>
              <p className="text-center text-muted py-8">{t('aiHub.noPoliciesFound')}</p>
            </CardBody>
          </Card>
        )}
      </div>
    </div>
  );
}
