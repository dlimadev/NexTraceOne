import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Scale, Search, ShieldCheck, ShieldAlert, AlertCircle, CheckCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import type { ComplianceSummaryResponse, ComplianceStatusType } from '../../../types';

/**
 * Dados simulados de compliance — alinhados com o backend GetComplianceSummary.
 * Em produção, virão da API /api/v1/governance/compliance/summary.
 */
const mockCompliance: ComplianceSummaryResponse = {
  overallScore: 74,
  totalServicesAssessed: 42,
  compliantCount: 24,
  partiallyCompliantCount: 12,
  nonCompliantCount: 6,
  coverage: {
    ownerDefined: 90,
    contractDefined: 74,
    versioningPresent: 68,
    documentationAvailable: 64,
    runbookAvailable: 45,
    dependenciesMapped: 71,
    publicationUpToDate: 60,
  },
  gaps: [
    {
      serviceId: 'svc-payment-gateway',
      serviceName: 'Payment Gateway',
      domain: 'Payments',
      team: 'payment-squad',
      status: 'NonCompliant',
      description: 'Missing runbook, outdated contract version, no dependency map',
    },
    {
      serviceId: 'svc-inventory-consumer',
      serviceName: 'Inventory Consumer',
      domain: 'Inventory',
      team: 'order-squad',
      status: 'NonCompliant',
      description: 'No technical owner defined, missing documentation',
    },
    {
      serviceId: 'svc-catalog-sync',
      serviceName: 'Catalog Sync',
      domain: 'Catalog',
      team: 'platform-squad',
      status: 'NonCompliant',
      description: 'No runbook, missing contract, outdated publication',
    },
    {
      serviceId: 'svc-auth-gateway',
      serviceName: 'Auth Gateway',
      domain: 'Identity',
      team: 'identity-squad',
      status: 'PartiallyCompliant',
      description: 'Contract exists but versioning not enforced',
    },
    {
      serviceId: 'svc-notification-worker',
      serviceName: 'Notification Worker',
      domain: 'Platform',
      team: 'platform-squad',
      status: 'PartiallyCompliant',
      description: 'Missing runbook, documentation incomplete',
    },
    {
      serviceId: 'svc-order-api',
      serviceName: 'Order API',
      domain: 'Orders',
      team: 'order-squad',
      status: 'PartiallyCompliant',
      description: 'Dependency map outdated, publication behind',
    },
    {
      serviceId: 'svc-reporting-engine',
      serviceName: 'Reporting Engine',
      domain: 'Analytics',
      team: 'data-squad',
      status: 'Compliant',
      description: 'All governance indicators met',
    },
  ],
  generatedAt: new Date().toISOString(),
};

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

  const d = mockCompliance;

  const filteredGaps = d.gaps.filter(gap => {
    if (filter === 'NonCompliant' && gap.status !== 'NonCompliant') return false;
    if (filter === 'PartiallyCompliant' && gap.status !== 'PartiallyCompliant') return false;
    if (search) {
      const q = search.toLowerCase();
      return gap.serviceName.toLowerCase().includes(q)
        || gap.domain.toLowerCase().includes(q)
        || gap.team.toLowerCase().includes(q);
    }
    return true;
  });

  const scoreColor = d.overallScore >= 80 ? 'text-success' : d.overallScore >= 60 ? 'text-amber-400' : 'text-critical';

  const coverageItems = [
    { key: 'ownerDefined', value: d.coverage.ownerDefined },
    { key: 'contractDefined', value: d.coverage.contractDefined },
    { key: 'versioningPresent', value: d.coverage.versioningPresent },
    { key: 'documentationAvailable', value: d.coverage.documentationAvailable },
    { key: 'runbookAvailable', value: d.coverage.runbookAvailable },
    { key: 'dependenciesMapped', value: d.coverage.dependenciesMapped },
    { key: 'publicationUpToDate', value: d.coverage.publicationUpToDate },
  ];

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.complianceTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.complianceSubtitle')}</p>
      </div>

      {/* Score + Stats */}
      <PageSection>
        <ContentGrid columns={4}>
          <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center col-span-2 md:col-span-1">
            <p className="text-xs text-muted mb-1">{t('governance.compliance.overallScore')}</p>
            <p className={`text-4xl font-bold ${scoreColor}`}>{d.overallScore}%</p>
          </div>
          <StatCard title={t('governance.compliance.totalAssessed')} value={d.totalServicesAssessed} icon={<Scale size={20} />} color="text-accent" />
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
                filteredGaps.map(gap => (
                  <div key={gap.serviceId} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-sm font-medium text-heading">{gap.serviceName}</span>
                        <Badge variant={statusBadgeVariant(gap.status)}>
                          {t(`governance.compliance.status.${gap.status}`)}
                        </Badge>
                      </div>
                      <p className="text-xs text-muted">{gap.description}</p>
                    </div>
                    <div className="hidden md:flex items-center gap-3 text-xs text-muted shrink-0">
                      <span className="w-24 truncate">{gap.domain}</span>
                      <span className="w-28 truncate">{gap.team}</span>
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
