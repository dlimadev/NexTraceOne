import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  DollarSign, Search, TrendingUp, TrendingDown, Minus,
  AlertTriangle, CheckCircle, AlertCircle, XCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type { FinOpsSummaryResponse, CostEfficiencyType } from '../../../types';

/**
 * Formata valores monetários respeitando o locale ativo.
 * Utiliza Intl.NumberFormat para formatação internacionalizada.
 */
function formatCurrency(value: number, locale = 'en-US'): string {
  return new Intl.NumberFormat(locale, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value);
}

/**
 * Dados simulados de FinOps — alinhados com o backend GetFinOpsSummary.
 * Em produção, virão da API /api/v1/governance/finops/summary.
 */
const mockFinOps: FinOpsSummaryResponse = {
  totalMonthlyCost: 128500,
  totalWaste: 18200,
  overallEfficiency: 'Acceptable',
  costTrend: 'Stable',
  services: [
    {
      serviceId: 'svc-payment-gateway',
      serviceName: 'Payment Gateway',
      domain: 'Payments',
      team: 'payment-squad',
      efficiency: 'Inefficient',
      monthlyCost: 32000,
      trend: 'Declining',
      wasteSignals: [
        { description: 'Over-provisioned compute instances', pattern: 'CPU utilization below 15% for 30 days', estimatedWaste: 8500 },
        { description: 'Unused reserved capacity', pattern: 'Reserved instances with zero traffic', estimatedWaste: 3200 },
      ],
    },
    {
      serviceId: 'svc-order-api',
      serviceName: 'Order API',
      domain: 'Orders',
      team: 'order-squad',
      efficiency: 'Acceptable',
      monthlyCost: 24500,
      trend: 'Stable',
      wasteSignals: [
        { description: 'Log retention exceeds policy', pattern: 'Storing 90 days instead of 30 days policy', estimatedWaste: 1800 },
      ],
    },
    {
      serviceId: 'svc-catalog-sync',
      serviceName: 'Catalog Sync',
      domain: 'Catalog',
      team: 'platform-squad',
      efficiency: 'Wasteful',
      monthlyCost: 18200,
      trend: 'Declining',
      wasteSignals: [
        { description: 'Duplicate data processing pipelines', pattern: 'Two identical ETL jobs running in parallel', estimatedWaste: 4200 },
        { description: 'Idle staging environment', pattern: 'Staging environment running 24/7 with no usage', estimatedWaste: 2500 },
      ],
    },
    {
      serviceId: 'svc-auth-gateway',
      serviceName: 'Auth Gateway',
      domain: 'Identity',
      team: 'identity-squad',
      efficiency: 'Efficient',
      monthlyCost: 8400,
      trend: 'Improving',
      wasteSignals: [],
    },
    {
      serviceId: 'svc-notification-worker',
      serviceName: 'Notification Worker',
      domain: 'Platform',
      team: 'platform-squad',
      efficiency: 'Acceptable',
      monthlyCost: 6200,
      trend: 'Stable',
      wasteSignals: [],
    },
    {
      serviceId: 'svc-reporting-engine',
      serviceName: 'Reporting Engine',
      domain: 'Analytics',
      team: 'data-squad',
      efficiency: 'Efficient',
      monthlyCost: 4800,
      trend: 'Improving',
      wasteSignals: [],
    },
  ],
  generatedAt: new Date().toISOString(),
};

type EfficiencyFilter = 'all' | CostEfficiencyType;

const efficiencyBadgeVariant = (eff: CostEfficiencyType): 'success' | 'warning' | 'danger' | 'default' => {
  switch (eff) {
    case 'Efficient': return 'success';
    case 'Acceptable': return 'default';
    case 'Inefficient': return 'warning';
    case 'Wasteful': return 'danger';
    default: return 'default';
  }
};

const efficiencyIcon = (eff: CostEfficiencyType) => {
  switch (eff) {
    case 'Efficient': return <CheckCircle size={14} className="text-emerald-400" />;
    case 'Acceptable': return <AlertCircle size={14} className="text-muted" />;
    case 'Inefficient': return <AlertTriangle size={14} className="text-orange-400" />;
    case 'Wasteful': return <XCircle size={14} className="text-critical" />;
    default: return null;
  }
};

const trendIcon = (dir: string) => {
  switch (dir) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    default: return <Minus size={14} className="text-muted" />;
  }
};

/**
 * Página de FinOps — otimização de custos contextual por serviço, equipa, domínio e operação.
 * Parte do módulo Governance do NexTraceOne.
 */
export function FinOpsPage() {
  const { t, i18n } = useTranslation();
  const [filter, setFilter] = useState<EfficiencyFilter>('all');
  const [search, setSearch] = useState('');
  const fmt = (v: number) => formatCurrency(v, i18n.language);

  const d = mockFinOps;

  const filtered = d.services.filter(svc => {
    if (filter !== 'all' && svc.efficiency !== filter) return false;
    if (search) {
      const q = search.toLowerCase();
      return svc.serviceName.toLowerCase().includes(q)
        || svc.domain.toLowerCase().includes(q)
        || svc.team.toLowerCase().includes(q);
    }
    return true;
  });

  const allWasteSignals = d.services.flatMap(svc =>
    svc.wasteSignals.map(ws => ({ ...ws, serviceName: svc.serviceName })),
  );

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.finopsTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.finopsSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(d.totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard
          title={t('governance.finops.overallEfficiency')}
          value={t(`governance.finops.efficiency.${d.overallEfficiency}`)}
          icon={efficiencyIcon(d.overallEfficiency) ?? <CheckCircle size={20} />}
          color="text-amber-500"
        />
        <StatCard
          title={t('governance.finops.costTrend')}
          value={t(`governance.finops.trend.${d.costTrend}`)}
          icon={trendIcon(d.costTrend)}
          color="text-blue-500"
        />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('governance.finops.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Wasteful', 'Inefficient', 'Acceptable', 'Efficient'] as EfficiencyFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              filter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {f === 'all' ? t('governance.finops.filterAll') : t(`governance.finops.filter${f}`)}
          </button>
        ))}
      </div>

      {/* Service cost list */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <DollarSign size={16} className="text-accent" />
            {t('governance.finops.serviceCosts')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(svc => (
                <div key={svc.serviceId} className="px-4 py-3 hover:bg-hover transition-colors">
                  <div className="flex items-center gap-3">
                    {efficiencyIcon(svc.efficiency)}
                    <span className="text-sm font-medium text-heading flex-1 min-w-0 truncate">{svc.serviceName}</span>
                    <Badge variant={efficiencyBadgeVariant(svc.efficiency)}>
                      {t(`governance.finops.efficiency.${svc.efficiency}`)}
                    </Badge>
                    <div className="hidden md:flex items-center gap-1 text-xs text-muted">
                      {trendIcon(svc.trend)}
                      {t(`governance.finops.trend.${svc.trend}`)}
                    </div>
                    <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(svc.monthlyCost)}</span>
                  </div>
                  <div className="hidden md:flex items-center gap-3 ml-7 mt-1 text-xs text-muted">
                    <span>{svc.domain}</span>
                    <span>•</span>
                    <span>{svc.team}</span>
                    {svc.wasteSignals.length > 0 && (
                      <>
                        <span>•</span>
                        <Badge variant="danger" className="text-[10px]">
                          {t('governance.finops.wasteAmount')}: {fmt(svc.wasteSignals.reduce((sum, ws) => sum + ws.estimatedWaste, 0))}
                        </Badge>
                      </>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </CardBody>
      </Card>

      {/* Waste signals */}
      {allWasteSignals.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-critical" />
              {t('governance.finops.wasteSignals')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {allWasteSignals.map((ws, i) => (
                <div key={i} className="px-4 py-3 hover:bg-hover transition-colors">
                  <div className="flex items-center gap-3">
                    <XCircle size={14} className="text-critical shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{ws.description}</p>
                      <p className="text-xs text-muted">{ws.serviceName} — {ws.pattern}</p>
                    </div>
                    <span className="text-sm font-mono font-medium text-critical shrink-0">{fmt(ws.estimatedWaste)}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}
    </div>
  );
}
