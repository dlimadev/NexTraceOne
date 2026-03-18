import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Globe, Search, Server, BarChart3, Users, ArrowRight, Tag, Loader2, AlertTriangle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';
import { PageContainer, PageSection } from '../../../components/shell';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import type { DomainSummary } from '../../../types';

type Criticality = 'Critical' | 'High' | 'Medium' | 'Low';

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

const criticalityBadgeVariant = (c: string): 'danger' | 'warning' | 'info' | 'default' => {
  switch (c) {
    case 'Critical':
      return 'danger';
    case 'High':
      return 'warning';
    case 'Medium':
      return 'info';
    case 'Low':
    default:
      return 'default';
  }
};

export function DomainsOverviewPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [domains, setDomains] = useState<DomainSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    organizationGovernanceApi.listDomains()
      .then((data) => {
        if (!cancelled) {
          setDomains(data.domains);
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

  const totalTeams = domains.reduce((sum, d) => sum + d.teamCount, 0);
  const totalServices = domains.reduce((sum, d) => sum + d.serviceCount, 0);

  const filtered = domains.filter(domain => {
    if (!search) return true;
    const q = search.toLowerCase();
    return domain.displayName.toLowerCase().includes(q)
      || domain.description?.toLowerCase().includes(q)
      || domain.capabilityClassification?.toLowerCase().includes(q);
  });

  if (loading) {
    return (
      <PageContainer>
        <ModuleHeader titleKey="organization.domains.title" subtitleKey="organization.domains.subtitle" />
        <div className="flex items-center justify-center py-20">
          <Loader2 size={32} className="animate-spin text-accent" />
        </div>
      </PageContainer>
    );
  }

  if (error) {
    return (
      <PageContainer>
        <ModuleHeader titleKey="organization.domains.title" subtitleKey="organization.domains.subtitle" />
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <AlertTriangle size={48} className="text-critical" />
          <p className="text-sm text-muted">{error}</p>
        </div>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <ModuleHeader
        titleKey="organization.domains.title"
        subtitleKey="organization.domains.subtitle"
      />

      {/* Stats */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <StatCard title={t('organization.domains.totalDomains')} value={domains.length} icon={<Globe size={20} />} color="text-accent" />
          <StatCard title={t('organization.domains.totalTeams')} value={totalTeams} icon={<Users size={20} />} color="text-blue-500" />
          <StatCard title={t('organization.domains.totalServices')} value={totalServices} icon={<Server size={20} />} color="text-success" />
          <StatCard title={t('organization.domains.avgMaturity')} value={t('organization.domains.maturityLevel.Defined')} icon={<BarChart3 size={20} />} color="text-info" />
        </div>
      </PageSection>

      {/* Search + Domain cards */}
      <PageSection>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('organization.domains.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
        </div>

        {domains.length === 0 ? (
          <div className="p-8 text-center text-muted text-sm">{t('organization.domains.noDomains')}</div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {filtered.length === 0 ? (
              <div className="col-span-full p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(domain => (
                <Card key={domain.domainId}>
                  <CardHeader>
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex items-center gap-3 min-w-0">
                        <Globe size={18} className="text-accent shrink-0" />
                        <h2 className="text-sm font-semibold text-heading truncate">{domain.displayName}</h2>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        <Badge variant={criticalityBadgeVariant(domain.criticality)}>
                          {t(`organization.domains.criticality.${domain.criticality}`)}
                        </Badge>
                        <Badge variant={maturityBadgeVariant(domain.maturityLevel)}>
                          {t(`organization.domains.maturityLevel.${domain.maturityLevel}`)}
                        </Badge>
                      </div>
                    </div>
                  </CardHeader>
                  <CardBody>
                    <p className="text-xs text-muted mb-3">{domain.description || '—'}</p>

                    <div className="flex items-center gap-2 text-xs text-muted mb-3">
                      <Tag size={12} />
                      <span>{t('organization.domains.capability')}: {domain.capabilityClassification || '—'}</span>
                    </div>

                    <div className="grid grid-cols-3 gap-3 mb-4">
                      <div className="bg-elevated rounded-md p-2 text-center">
                        <p className="text-lg font-bold text-heading">{domain.teamCount}</p>
                        <p className="text-xs text-muted">{t('organization.domains.teams')}</p>
                      </div>
                      <div className="bg-elevated rounded-md p-2 text-center">
                        <p className="text-lg font-bold text-heading">{domain.serviceCount}</p>
                        <p className="text-xs text-muted">{t('organization.domains.services')}</p>
                      </div>
                      <div className="bg-elevated rounded-md p-2 text-center">
                        <p className="text-lg font-bold text-heading">{domain.contractCount}</p>
                        <p className="text-xs text-muted">{t('organization.domains.contracts')}</p>
                      </div>
                    </div>

                    <Link
                      to={`/governance/domains/${domain.domainId}`}
                      className="flex items-center justify-center gap-2 w-full py-2 text-sm font-medium text-accent hover:text-accent/80 transition-colors rounded-md border border-edge hover:border-accent/30 hover:bg-accent/5"
                    >
                      {t('organization.domains.viewDetails')}
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
