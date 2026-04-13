/**
 * ContractHealthDashboardPage — dashboard de saúde dos contratos.
 * Mostra métricas agregadas: score de saúde, percentagem com exemplos,
 * entidades canónicas linkadas, contratos deprecated e top violações.
 * Pilar: Contract Governance + Source of Truth.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { ShieldCheck, AlertTriangle, CheckCircle2, XCircle, BarChart2, TrendingUp } from 'lucide-react';

type HealthDashboardData = {
  totalContractVersions: number;
  distinctContracts: number;
  deprecatedVersions: number;
  filteredCount: number;
  percentWithExamples: number;
  percentWithCanonicalEntities: number;
  topViolations: Array<{
    contractVersionId: string;
    semVer: string;
    violationCount: number;
    topRuleIds: string[];
  }>;
  healthScore: number;
};

function ScoreRing({ score }: { score: number }) {
  const color = score >= 80 ? 'text-success' : score >= 50 ? 'text-warning' : 'text-critical';
  return (
    <div className="flex flex-col items-center gap-1">
      <div className={`text-5xl font-bold tabular-nums ${color}`}>{score}</div>
      <div className="text-xs text-muted uppercase tracking-wider">/100</div>
    </div>
  );
}

function MetricCard({ label, value, icon: Icon, variant = 'default' }: {
  label: string;
  value: string | number;
  icon: React.ElementType;
  variant?: 'default' | 'success' | 'warning' | 'danger';
}) {
  const colors: Record<string, string> = {
    default: 'text-accent',
    success: 'text-success',
    warning: 'text-warning',
    danger: 'text-critical',
  };
  return (
    <div className="bg-panel rounded-lg border border-edge p-4 flex flex-col gap-2">
      <div className="flex items-center gap-2 text-muted">
        <Icon size={14} className={colors[variant]} />
        <span className="text-xs uppercase tracking-wide">{label}</span>
      </div>
      <div className={`text-2xl font-semibold tabular-nums ${colors[variant]}`}>{value}</div>
    </div>
  );
}

export function ContractHealthDashboardPage() {
  const { t } = useTranslation();

  const { data, isLoading, error } = useQuery<HealthDashboardData>({
    queryKey: ['contract-health-dashboard'],
    queryFn: () => contractsApi.getHealthDashboard({ page: 1, pageSize: 50 }) as Promise<HealthDashboardData>,
    staleTime: 30_000,
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-heading flex items-center gap-2">
            <ShieldCheck size={24} className="text-accent" />
            {t('contracts.healthDashboard.title', 'Contract Health Dashboard')}
          </h1>
          <p className="text-muted text-sm mt-1">
            {t('contracts.healthDashboard.subtitle', 'Aggregated quality and governance metrics across all contracts')}
          </p>
        </div>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-muted">
          {t('common.loading', 'Loading...')}
        </div>
      )}

      {error && (
        <div className="bg-critical-muted border border-critical/30 rounded-lg p-4 text-critical text-sm">
          {t('common.error', 'Failed to load data')}
        </div>
      )}

      {data && (
        <>
          {/* Score + KPIs */}
          <div className="grid grid-cols-1 lg:grid-cols-5 gap-4">
            <div className="bg-panel rounded-lg border border-edge p-6 flex flex-col items-center justify-center gap-2 lg:col-span-1">
              <div className="text-xs text-muted uppercase tracking-wide mb-1">
                {t('contracts.healthDashboard.healthScore', 'Health Score')}
              </div>
              <ScoreRing score={data.healthScore} />
            </div>

            <div className="lg:col-span-4 grid grid-cols-2 md:grid-cols-4 gap-4">
              <MetricCard
                label={t('contracts.healthDashboard.totalContracts', 'Total Contracts')}
                value={data.distinctContracts}
                icon={BarChart2}
              />
              <MetricCard
                label={t('contracts.healthDashboard.withExamples', 'With Examples')}
                value={`${data.percentWithExamples}%`}
                icon={CheckCircle2}
                variant={data.percentWithExamples >= 70 ? 'success' : 'warning'}
              />
              <MetricCard
                label={t('contracts.healthDashboard.withCanonical', 'With Canonical Entities')}
                value={`${data.percentWithCanonicalEntities}%`}
                icon={TrendingUp}
                variant={data.percentWithCanonicalEntities >= 50 ? 'success' : 'warning'}
              />
              <MetricCard
                label={t('contracts.healthDashboard.deprecated', 'Deprecated Versions')}
                value={data.deprecatedVersions}
                icon={XCircle}
                variant={data.deprecatedVersions > 0 ? 'danger' : 'success'}
              />
            </div>
          </div>

          {/* Top Violations */}
          {data.topViolations.length > 0 && (
            <div className="bg-panel rounded-lg border border-edge p-4">
              <h2 className="text-sm font-medium text-body mb-3 flex items-center gap-2">
                <AlertTriangle size={14} className="text-warning" />
                {t('contracts.healthDashboard.topViolations', 'Contracts with Most Violations')}
              </h2>
              <div className="space-y-2">
                {data.topViolations.map((v) => (
                  <div
                    key={v.contractVersionId}
                    className="flex items-center justify-between py-2 px-3 rounded bg-elevated text-sm"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-body font-mono text-xs">{v.semVer}</span>
                      <span className="text-faded text-xs hidden sm:block">
                        {v.topRuleIds.join(', ')}
                      </span>
                    </div>
                    <span className="text-critical font-semibold tabular-nums">
                      {v.violationCount} {t('contracts.healthDashboard.violations', 'violations')}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {data.topViolations.length === 0 && (
            <div className="bg-success-muted border border-success/30 rounded-lg p-4 text-success text-sm flex items-center gap-2">
              <CheckCircle2 size={14} />
              {t('contracts.healthDashboard.noViolations', 'No rule violations detected — contracts are healthy!')}
            </div>
          )}
        </>
      )}
    </div>
  );
}
