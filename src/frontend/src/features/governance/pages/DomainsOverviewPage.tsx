import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Globe, Search, Server, BarChart3, Users, ArrowRight, Tag, FileText,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { ModuleHeader } from '../../../components/ModuleHeader';
import { PageContainer } from '../../../components/shell';

type MaturityLevel = 'Initial' | 'Developing' | 'Defined' | 'Managed' | 'Optimizing';
type Criticality = 'Critical' | 'High' | 'Medium' | 'Low';

const mockDomains = [
  { domainId: 'dom-commerce', name: 'commerce', displayName: 'Commerce', description: 'E-commerce, orders, payments and inventory management', criticality: 'Critical' as Criticality, teamCount: 2, serviceCount: 10, contractCount: 14, maturityLevel: 'Defined' as MaturityLevel, capabilityClassification: 'Revenue Generation' },
  { domainId: 'dom-platform', name: 'platform', displayName: 'Platform', description: 'Core platform infrastructure, API gateway and shared services', criticality: 'Critical' as Criticality, teamCount: 1, serviceCount: 8, contractCount: 12, maturityLevel: 'Managed' as MaturityLevel, capabilityClassification: 'Technology Foundation' },
  { domainId: 'dom-identity', name: 'identity', displayName: 'Identity & Access', description: 'Authentication, authorization and user lifecycle', criticality: 'High' as Criticality, teamCount: 1, serviceCount: 4, contractCount: 6, maturityLevel: 'Managed' as MaturityLevel, capabilityClassification: 'Security & Compliance' },
  { domainId: 'dom-data', name: 'data-analytics', displayName: 'Data & Analytics', description: 'Data pipelines, warehousing, analytics and reporting', criticality: 'High' as Criticality, teamCount: 1, serviceCount: 5, contractCount: 4, maturityLevel: 'Developing' as MaturityLevel, capabilityClassification: 'Business Intelligence' },
];

const maturityBadgeVariant = (level: MaturityLevel): 'success' | 'info' | 'warning' | 'danger' => {
  switch (level) {
    case 'Optimizing':
    case 'Managed':
      return 'success';
    case 'Defined':
      return 'info';
    case 'Developing':
      return 'warning';
    case 'Initial':
      return 'danger';
  }
};

const criticalityBadgeVariant = (c: Criticality): 'danger' | 'warning' | 'info' | 'default' => {
  switch (c) {
    case 'Critical':
      return 'danger';
    case 'High':
      return 'warning';
    case 'Medium':
      return 'info';
    case 'Low':
      return 'default';
  }
};

export function DomainsOverviewPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const totalTeams = mockDomains.reduce((sum, d) => sum + d.teamCount, 0);
  const totalServices = mockDomains.reduce((sum, d) => sum + d.serviceCount, 0);

  const filtered = mockDomains.filter(domain => {
    if (!search) return true;
    const q = search.toLowerCase();
    return domain.displayName.toLowerCase().includes(q)
      || domain.description?.toLowerCase().includes(q)
      || domain.capabilityClassification?.toLowerCase().includes(q);
  });

  return (
    <PageContainer>
      <ModuleHeader
        titleKey="organization.domains.title"
        subtitleKey="organization.domains.subtitle"
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('organization.domains.totalDomains')} value={mockDomains.length} icon={<Globe size={20} />} color="text-accent" />
        <StatCard title={t('organization.domains.totalTeams')} value={totalTeams} icon={<Users size={20} />} color="text-blue-500" />
        <StatCard title={t('organization.domains.totalServices')} value={totalServices} icon={<Server size={20} />} color="text-success" />
        <StatCard title={t('organization.domains.avgMaturity')} value={t('organization.domains.maturityLevel.Defined')} icon={<BarChart3 size={20} />} color="text-info" />
      </div>

      {/* Search */}
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

      {/* Domain cards grid */}
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
                <p className="text-xs text-muted mb-3">{domain.description}</p>

                <div className="flex items-center gap-2 text-xs text-muted mb-3">
                  <Tag size={12} />
                  <span>{t('organization.domains.capability')}: {domain.capabilityClassification}</span>
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
    </PageContainer>
  );
}
