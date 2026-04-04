import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ShieldCheck, Users, Globe, Calendar, Clock } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import { queryKeys } from '../../../shared/api/queryKeys';

type DelegationScope = 'TeamAdmin' | 'DomainAdmin' | 'ReadOnly' | 'FullAdmin';

const scopeBadgeVariant = (scope: string): 'info' | 'warning' | 'default' | 'danger' => {
  switch (scope) {
    case 'TeamAdmin':
      return 'info';
    case 'DomainAdmin':
      return 'warning';
    case 'ReadOnly':
      return 'default';
    case 'FullAdmin':
      return 'danger';
    default:
      return 'default';
  }
};

type ScopeFilter = 'All' | DelegationScope;

export function DelegatedAdminPage() {
  const { t } = useTranslation();
  const [scopeFilter, setScopeFilter] = useState<ScopeFilter>('All');

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.governance.delegations(),
    queryFn: () => organizationGovernanceApi.listDelegations(),
    staleTime: 30_000,
  });

  const delegations = data?.delegations ?? [];
  const activeDelegations = delegations.filter(d => d.isActive).length;
  const teamScoped = delegations.filter(d => d.teamId !== null && d.teamId !== undefined).length;
  const domainScoped = delegations.filter(d => d.domainId !== null && d.domainId !== undefined).length;

  const filtered = delegations.filter(d => {
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

  if (isLoading) {
    return (
      <PageContainer>
        <ModuleHeader titleKey="organization.delegatedAdmin.title" subtitleKey="organization.delegatedAdmin.subtitle" />
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <ModuleHeader titleKey="organization.delegatedAdmin.title" subtitleKey="organization.delegatedAdmin.subtitle" />
        <PageErrorState message={t('common.errorLoading')} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <ModuleHeader
        titleKey="organization.delegatedAdmin.title"
        subtitleKey="organization.delegatedAdmin.subtitle"
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-6">
        <StatCard title={t('organization.delegatedAdmin.activeDelegations')} value={activeDelegations} icon={<ShieldCheck size={20} />} color="text-accent" />
        <StatCard title={t('organization.delegatedAdmin.teamScoped')} value={teamScoped} icon={<Users size={20} />} color="text-info" />
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
      {delegations.length === 0 ? (
        <div className="p-8 text-center text-muted text-sm">
          {t('organization.delegatedAdmin.noDelegations')}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filtered.length === 0 ? (
            <div className="col-span-full">
              <EmptyState
                title={t('governance.delegations.empty', 'No delegations found')}
                description={t('governance.delegations.emptyDescription', 'No delegated access entries match your filters.')}
              />
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
      )}
    </PageContainer>
  );
}
