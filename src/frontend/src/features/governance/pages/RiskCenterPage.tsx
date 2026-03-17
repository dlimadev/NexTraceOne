import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ShieldAlert, Search, AlertTriangle, AlertCircle,
  Shield, CheckCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import type { RiskSummaryResponse, RiskLevel } from '../../../types';

/**
 * Dados simulados de risco — alinhados com o backend GetRiskSummary.
 * Em produção, virão da API /api/v1/governance/risk/summary.
 */
const mockRiskSummary: RiskSummaryResponse = {
  overallRiskLevel: 'Medium',
  totalServicesAssessed: 42,
  criticalCount: 2,
  highCount: 5,
  mediumCount: 14,
  lowCount: 21,
  indicators: [
    {
      serviceId: 'svc-payment-gateway',
      serviceName: 'Payment Gateway',
      domain: 'Payments',
      team: 'payment-squad',
      riskLevel: 'Critical',
      dimensions: [
        { dimension: 'Operational', level: 'Critical', explanation: 'Multiple production incidents in last 30 days' },
        { dimension: 'Change', level: 'High', explanation: 'Frequent deployments without validation' },
        { dimension: 'IncidentRecurrence', level: 'Critical', explanation: 'Recurring error rate spikes' },
      ],
    },
    {
      serviceId: 'svc-order-api',
      serviceName: 'Order API',
      domain: 'Orders',
      team: 'order-squad',
      riskLevel: 'Critical',
      dimensions: [
        { dimension: 'Dependency', level: 'Critical', explanation: '12 direct consumers affected by recent changes' },
        { dimension: 'Contract', level: 'High', explanation: 'Breaking contract change without versioning' },
      ],
    },
    {
      serviceId: 'svc-catalog-sync',
      serviceName: 'Catalog Sync',
      domain: 'Catalog',
      team: 'platform-squad',
      riskLevel: 'High',
      dimensions: [
        { dimension: 'Operational', level: 'High', explanation: 'Integration partner SLA breaches' },
        { dimension: 'Documentation', level: 'Medium', explanation: 'Missing runbook and operational docs' },
      ],
    },
    {
      serviceId: 'svc-inventory-consumer',
      serviceName: 'Inventory Consumer',
      domain: 'Inventory',
      team: 'order-squad',
      riskLevel: 'High',
      dimensions: [
        { dimension: 'Change', level: 'High', explanation: 'Consumer lag after recent deployment' },
        { dimension: 'Ownership', level: 'Medium', explanation: 'No defined technical owner' },
      ],
    },
    {
      serviceId: 'svc-auth-gateway',
      serviceName: 'Auth Gateway',
      domain: 'Identity',
      team: 'identity-squad',
      riskLevel: 'Medium',
      dimensions: [
        { dimension: 'Contract', level: 'Medium', explanation: 'Schema mismatch detected in staging' },
        { dimension: 'AiGovernance', level: 'Low', explanation: 'AI-generated contract not reviewed' },
      ],
    },
    {
      serviceId: 'svc-notification-worker',
      serviceName: 'Notification Worker',
      domain: 'Platform',
      team: 'platform-squad',
      riskLevel: 'Medium',
      dimensions: [
        { dimension: 'Documentation', level: 'Medium', explanation: 'No runbook available' },
        { dimension: 'Operational', level: 'Low', explanation: 'Minor background job failures' },
      ],
    },
    {
      serviceId: 'svc-reporting-engine',
      serviceName: 'Reporting Engine',
      domain: 'Analytics',
      team: 'data-squad',
      riskLevel: 'Low',
      dimensions: [
        { dimension: 'Operational', level: 'Low', explanation: 'Stable with no recent issues' },
      ],
    },
  ],
  generatedAt: new Date().toISOString(),
};

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

  const d = mockRiskSummary;

  const filtered = d.indicators.filter(ind => {
    if (filter !== 'all' && ind.riskLevel !== filter) return false;
    if (search) {
      const q = search.toLowerCase();
      return ind.serviceName.toLowerCase().includes(q)
        || ind.domain.toLowerCase().includes(q)
        || ind.team.toLowerCase().includes(q);
    }
    return true;
  });

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.riskTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.riskSubtitle')}</p>
      </div>

      {/* Stats */}
      <PageSection>
        <ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
          <StatCard title={t('governance.risk.totalAssessed')} value={d.totalServicesAssessed} icon={<Shield size={20} />} color="text-accent" />
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
                  <div key={ind.serviceId} className="px-4 py-3 hover:bg-hover transition-colors">
                    <div className="flex items-center gap-3 mb-2">
                      {riskIcon(ind.riskLevel)}
                      <span className="text-sm font-medium text-heading">{ind.serviceName}</span>
                      <Badge variant={riskBadgeVariant(ind.riskLevel)}>
                        {t(`governance.risk.level.${ind.riskLevel}`)}
                      </Badge>
                      <span className="hidden md:inline text-xs text-muted">{ind.domain}</span>
                      <span className="hidden md:inline text-xs text-muted">•</span>
                      <span className="hidden md:inline text-xs text-muted">{ind.team}</span>
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
