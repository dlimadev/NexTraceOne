import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ShieldCheck, Users, Globe, Calendar, Clock } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';

type DelegationScope = 'TeamAdmin' | 'DomainAdmin' | 'ReadOnly' | 'FullAdmin';

interface Delegation {
  delegationId: string;
  granteeUserId: string;
  granteeDisplayName: string;
  scope: DelegationScope;
  teamId: string | null;
  teamName: string | null;
  domainId: string | null;
  domainName: string | null;
  reason: string;
  isActive: boolean;
  grantedAt: string;
  expiresAt: string | null;
}

const mockDelegations: Delegation[] = [
  {
    delegationId: 'del-1',
    granteeUserId: 'usr-001',
    granteeDisplayName: 'Maria Santos',
    scope: 'TeamAdmin',
    teamId: 'team-commerce',
    teamName: 'Commerce',
    domainId: null,
    domainName: null,
    reason: 'Team lead for Commerce during Q1 sprint',
    isActive: true,
    grantedAt: '2025-12-01T10:00:00Z',
    expiresAt: '2026-06-01T00:00:00Z',
  },
  {
    delegationId: 'del-2',
    granteeUserId: 'usr-002',
    granteeDisplayName: 'João Oliveira',
    scope: 'DomainAdmin',
    teamId: null,
    teamName: null,
    domainId: 'dom-platform',
    domainName: 'Platform',
    reason: 'Domain steward for Platform domain',
    isActive: true,
    grantedAt: '2025-11-15T08:00:00Z',
    expiresAt: null,
  },
  {
    delegationId: 'del-3',
    granteeUserId: 'usr-003',
    granteeDisplayName: 'Ana Costa',
    scope: 'ReadOnly',
    teamId: 'team-identity',
    teamName: 'Identity & Access',
    domainId: null,
    domainName: null,
    reason: 'Audit access for compliance review',
    isActive: false,
    grantedAt: '2025-10-01T10:00:00Z',
    expiresAt: '2025-12-31T00:00:00Z',
  },
];

const scopeBadgeVariant = (scope: DelegationScope): 'info' | 'warning' | 'default' | 'danger' => {
  switch (scope) {
    case 'TeamAdmin':
      return 'info';
    case 'DomainAdmin':
      return 'warning';
    case 'ReadOnly':
      return 'default';
    case 'FullAdmin':
      return 'danger';
  }
};

type ScopeFilter = 'All' | DelegationScope;

export function DelegatedAdminPage() {
  const { t } = useTranslation();
  const [scopeFilter, setScopeFilter] = useState<ScopeFilter>('All');

  const activeDelegations = mockDelegations.filter(d => d.isActive).length;
  const teamScoped = mockDelegations.filter(d => d.teamId !== null).length;
  const domainScoped = mockDelegations.filter(d => d.domainId !== null).length;

  const filtered = mockDelegations.filter(d => {
    if (scopeFilter === 'All') return true;
    return d.scope === scopeFilter;
  });

  const filterOptions: { value: ScopeFilter; labelKey: string }[] = [
    { value: 'All', labelKey: 'organization.delegatedAdmin.filterAll' },
    { value: 'TeamAdmin', labelKey: 'organization.delegatedAdmin.filterTeamAdmin' },
    { value: 'DomainAdmin', labelKey: 'organization.delegatedAdmin.filterDomainAdmin' },
    { value: 'ReadOnly', labelKey: 'organization.delegatedAdmin.filterReadOnly' },
  ];

  const formatDate = (iso: string) => new Date(iso).toLocaleDateString();

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <ModuleHeader
        titleKey="organization.delegatedAdmin.title"
        subtitleKey="organization.delegatedAdmin.subtitle"
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-6">
        <StatCard title={t('organization.delegatedAdmin.activeDelegations')} value={activeDelegations} icon={<ShieldCheck size={20} />} color="text-accent" />
        <StatCard title={t('organization.delegatedAdmin.teamScoped')} value={teamScoped} icon={<Users size={20} />} color="text-blue-500" />
        <StatCard title={t('organization.delegatedAdmin.domainScoped')} value={domainScoped} icon={<Globe size={20} />} color="text-success" />
      </div>

      {/* Scope filter */}
      <div className="flex flex-wrap items-center gap-2 mb-4">
        {filterOptions.map(opt => (
          <button
            key={opt.value}
            onClick={() => setScopeFilter(opt.value)}
            className={`px-3 py-1.5 text-xs font-medium rounded-md border transition-colors ${
              scopeFilter === opt.value
                ? 'bg-accent/15 text-accent border-accent/30'
                : 'bg-elevated text-muted border-edge hover:border-accent/20 hover:text-body'
            }`}
          >
            {t(opt.labelKey)}
          </button>
        ))}
      </div>

      {/* Delegation cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filtered.length === 0 ? (
          <div className="col-span-full p-8 text-center text-muted text-sm">
            {t('organization.delegatedAdmin.noDelegations')}
          </div>
        ) : (
          filtered.map(delegation => (
            <Card key={delegation.delegationId}>
              <CardHeader>
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-3 min-w-0">
                    <ShieldCheck size={18} className="text-accent shrink-0" />
                    <h2 className="text-sm font-semibold text-heading truncate">
                      {delegation.granteeDisplayName}
                    </h2>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <Badge variant={delegation.isActive ? 'success' : 'default'}>
                      {t(`organization.delegatedAdmin.status.${delegation.isActive ? 'active' : 'inactive'}`)}
                    </Badge>
                    <Badge variant={scopeBadgeVariant(delegation.scope)}>
                      {t(`organization.delegatedAdmin.scope.${delegation.scope}`)}
                    </Badge>
                  </div>
                </div>
              </CardHeader>
              <CardBody>
                {/* Context: team or domain */}
                {delegation.teamName && (
                  <div className="flex items-center gap-2 text-xs text-muted mb-2">
                    <Users size={12} />
                    <span>{t('organization.delegatedAdmin.team')}: {delegation.teamName}</span>
                  </div>
                )}
                {delegation.domainName && (
                  <div className="flex items-center gap-2 text-xs text-muted mb-2">
                    <Globe size={12} />
                    <span>{t('organization.delegatedAdmin.domain')}: {delegation.domainName}</span>
                  </div>
                )}

                {/* Reason */}
                <p className="text-xs text-muted mb-3">{delegation.reason}</p>

                {/* Dates */}
                <div className="flex items-center gap-4 text-xs text-muted">
                  <span className="flex items-center gap-1">
                    <Calendar size={12} />
                    {t('organization.delegatedAdmin.grantedAt')}: {formatDate(delegation.grantedAt)}
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock size={12} />
                    {t('organization.delegatedAdmin.expiresAt')}:{' '}
                    {delegation.expiresAt
                      ? formatDate(delegation.expiresAt)
                      : t('organization.delegatedAdmin.noExpiry')}
                  </span>
                </div>
              </CardBody>
            </Card>
          ))
        )}
      </div>
    </div>
  );
}
