/**
 * ContractCoverageWidget — cobertura de contratos por tipo (REST/SOAP/Event).
 * Reforça o pilar de Contract Governance do NexTraceOne — fonte de verdade dos contratos.
 * Dados via GET /governance/contracts/coverage.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { FileCheck } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface ContractCoverageResponse {
  totalServices: number;
  coveredServices: number;
  coveragePercent: number;
  rest: number;
  soap: number;
  event: number;
}

// ── Helpers ────────────────────────────────────────────────────────────────

function coverageColour(pct: number): string {
  if (pct >= 80) return 'text-emerald-600 dark:text-emerald-400';
  if (pct >= 50) return 'text-amber-600 dark:text-amber-400';
  return 'text-red-600 dark:text-red-400';
}

function coverageBarColour(pct: number): string {
  if (pct >= 80) return 'bg-emerald-500';
  if (pct >= 50) return 'bg-amber-500';
  return 'bg-red-500';
}

// ── Component ──────────────────────────────────────────────────────────────

export function ContractCoverageWidget({ config, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.contractCoverage', 'Contract Coverage');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-contract-coverage', config.teamId, config.serviceId],
    queryFn: () =>
      client
        .get<ContractCoverageResponse>('/governance/contracts/coverage', {
          params: {
            teamId: config.teamId ?? undefined,
            serviceId: config.serviceId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const pct = Math.min(Math.max(data.coveragePercent ?? 0, 0), 100);

  const contractTypes = [
    {
      label: 'REST',
      value: data.rest,
      cls: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300',
    },
    {
      label: 'SOAP',
      value: data.soap,
      cls: 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300',
    },
    {
      label: t('governance.customDashboards.contractCoverage.event', 'Event'),
      value: data.event,
      cls: 'bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300',
    },
  ];

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      {/* Header */}
      <div className="flex items-center gap-2">
        <FileCheck size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        <span className={`ml-auto text-lg font-bold tabular-nums ${coverageColour(pct)}`}>
          {pct.toFixed(0)}%
        </span>
      </div>

      {/* Coverage progress bar */}
      <div
        className="w-full h-2 rounded-full bg-gray-200 dark:bg-gray-700 overflow-hidden"
        role="presentation"
      >
        <div
          className={`h-full rounded-full transition-all duration-500 ${coverageBarColour(pct)}`}
          style={{ width: `${pct}%` }}
          role="progressbar"
          aria-valuenow={pct}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={t('governance.customDashboards.contractCoverage.progressLabel', 'Contract coverage')}
        />
      </div>

      <p className="text-[10px] text-gray-400 text-center">
        {data.coveredServices} / {data.totalServices}{' '}
        {t('governance.customDashboards.contractCoverage.servicesWithContracts', 'services with contracts')}
      </p>

      {/* Breakdown by contract type */}
      <div className="grid grid-cols-3 gap-1 mt-auto">
        {contractTypes.map(({ label, value, cls }) => (
          <div
            key={label}
            className={`rounded p-1 flex flex-col items-center justify-center ${cls}`}
          >
            <span className="text-sm font-bold tabular-nums">{value}</span>
            <span className="text-[9px] font-medium">{label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
