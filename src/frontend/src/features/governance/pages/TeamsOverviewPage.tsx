import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Users, Search, Server, BarChart3, AlertTriangle, ArrowRight, Building2, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import type { TeamSummary } from '../../../types';

type MaturityLevel = 'Initial' | 'Developing' | 'Defined' | 'Managed' | 'Optimizing';

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

export function TeamsOverviewPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [teams, setTeams] = useState<TeamSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    organizationGovernanceApi.listTeams()
      .then((data) => {
        if (!cancelled) {
          setTeams(data.teams);
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

  const totalServices = teams.reduce((sum, team) => sum + team.serviceCount, 0);

  const filtered = teams.filter(team => {
    if (!search) return true;
    const q = search.toLowerCase();
    return team.displayName.toLowerCase().includes(q)
      || team.description?.toLowerCase().includes(q)
      || team.parentOrganizationUnit?.toLowerCase().includes(q);
  });

  if (loading) {
    return (
      <PageContainer>
        <ModuleHeader titleKey="organization.teams.title" subtitleKey="organization.teams.subtitle" />
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (error) {
    return (
      <PageContainer>
        <ModuleHeader titleKey="organization.teams.title" subtitleKey="organization.teams.subtitle" />
        <PageErrorState message={error} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <ModuleHeader
        titleKey="organization.teams.title"
        subtitleKey="organization.teams.subtitle"
      />

      {/* Stats */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatCard title={t('organization.teams.totalTeams')} value={teams.length} icon={<Users size={20} />} color="text-accent" />
          <StatCard title={t('organization.teams.totalServices')} value={totalServices} icon={<Server size={20} />} color="text-blue-500" />
          <StatCard title={t('organization.teams.avgMaturity')} value={t('organization.teams.maturityLevel.Defined')} icon={<BarChart3 size={20} />} color="text-success" />
          <StatCard title={t('organization.teams.activeIncidents')} value="—" icon={<AlertTriangle size={20} />} color="text-critical" />
        </div>
      </PageSection>

      {/* Search + Team cards */}
      <PageSection>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('organization.teams.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
        </div>

        {teams.length === 0 ? (
          <div className="p-8 text-center text-muted text-sm">{t('organization.teams.noTeams')}</div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {filtered.length === 0 ? (
              <div className="col-span-full p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(team => (
                <Card key={team.teamId}>
                  <CardHeader>
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex items-center gap-3 min-w-0">
                        <Users size={18} className="text-accent shrink-0" />
                        <h2 className="text-sm font-semibold text-heading truncate">{team.displayName}</h2>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Badge variant={statusBadgeVariant(team.status)}>
                          {t(`organization.teams.status.${team.status}`)}
                        </Badge>
                        <Badge variant={maturityBadgeVariant(team.maturityLevel)}>
                          {t(`organization.teams.maturityLevel.${team.maturityLevel}`)}
                        </Badge>
                      </div>
                    </div>
                  </CardHeader>
                  <CardBody>
                    <p className="text-xs text-muted mb-3">{team.description || '—'}</p>

                    <div className="flex items-center gap-2 text-xs text-muted mb-3">
                      <Building2 size={12} />
                      <span>{t('organization.teams.orgUnit')}: {team.parentOrganizationUnit || '—'}</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3 mb-4">
                      <div className="bg-elevated rounded-md p-2 text-center">
                        <p className="text-lg font-bold text-heading">{team.serviceCount}</p>
                        <p className="text-xs text-muted">{t('organization.teams.services')}</p>
                      </div>
                      <div className="bg-elevated rounded-md p-2 text-center">
                        <p className="text-lg font-bold text-heading">{team.contractCount}</p>
                        <p className="text-xs text-muted">{t('organization.teams.contracts')}</p>
                      </div>
                      <div className="bg-elevated rounded-md p-2 text-center">
                        <p className="text-lg font-bold text-heading">{team.memberCount}</p>
                        <p className="text-xs text-muted">{t('organization.teams.members')}</p>
                      </div>
                    </div>

                    <Link
                      to={`/governance/teams/${team.teamId}`}
                      className="flex items-center justify-center gap-2 w-full py-2 text-sm font-medium text-accent hover:text-accent/80 transition-colors rounded-md border border-edge hover:border-accent/30 hover:bg-accent/5"
                    >
                      {t('organization.teams.viewDetails')}
                      <ArrowRight size={14} />
                    </Link>
                  </CardBody>
                </Card>
              ))
            )}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
