import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart3,
  Eye,
  Users,
  Download,
  Clock,
  CalendarDays,
  Activity,
  FlaskConical,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageHeader } from '../../../components/PageHeader';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { StatCard } from '../../../components/StatCard';
import { reportsApi } from '../api/reports';
import type { DashboardUsageSummary } from '../api/reports';

// ── Constants ─────────────────────────────────────────────────────────────────

const WINDOW_OPTIONS = [
  { label: '7d', days: 7 },
  { label: '30d', days: 30 },
  { label: '90d', days: 90 },
] as const;

type WindowDays = (typeof WINDOW_OPTIONS)[number]['days'];

const SIMULATED_TENANT_ID = 'default';

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatDuration(seconds: number): string {
  if (seconds < 60) return `${Math.round(seconds)}s`;
  return `${Math.round(seconds / 60)}m`;
}

function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

const PERSONA_VARIANT: Record<string, 'info' | 'success' | 'warning' | 'default'> = {
  Engineer: 'info',
  TechLead: 'success',
  Executive: 'warning',
  Architect: 'info',
  Auditor: 'default',
};

function personaVariant(persona: string): 'info' | 'success' | 'warning' | 'default' {
  return PERSONA_VARIANT[persona] ?? 'default';
}

// ── Row component ─────────────────────────────────────────────────────────────

interface UsageRowProps {
  item: DashboardUsageSummary;
  rank: number;
}

function UsageRow({ item, rank }: UsageRowProps) {
  return (
    <tr className="border-b border-edge/40 hover:bg-elevated/40 transition-colors">
      <td className="px-4 py-3 text-xs text-muted w-8 tabular-nums">{rank}</td>
      <td className="px-4 py-3">
        <p className="text-sm font-medium text-heading">{item.dashboardName}</p>
        <p className="text-xs text-muted font-mono mt-0.5">{item.dashboardId}</p>
      </td>
      <td className="px-4 py-3 text-sm tabular-nums text-body text-right">
        {item.totalViews.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-sm tabular-nums text-body text-right">
        {item.uniqueUsers.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-sm tabular-nums text-body text-right">
        {item.exportCount.toLocaleString()}
      </td>
      <td className="px-4 py-3 text-sm tabular-nums text-body text-right">
        {formatDuration(item.avgDurationSeconds)}
      </td>
      <td className="px-4 py-3 text-xs text-muted text-right whitespace-nowrap">
        {formatDate(item.lastViewedAt)}
      </td>
      <td className="px-4 py-3 text-right">
        {item.topPersona ? (
          <Badge variant={personaVariant(item.topPersona)} size="sm">
            {item.topPersona}
          </Badge>
        ) : (
          <span className="text-xs text-muted">—</span>
        )}
      </td>
    </tr>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * DashboardUsageAnalyticsPage — V3.6 usage analytics for all dashboards.
 *
 * Shows aggregate view/export/embed stats per dashboard, with time window
 * selector and summary KPI cards.
 */
export function DashboardUsageAnalyticsPage() {
  const { t } = useTranslation();
  const [windowDays, setWindowDays] = useState<WindowDays>(30);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['dashboard-usage-analytics', windowDays],
    queryFn: () => reportsApi.getUsageAnalytics(SIMULATED_TENANT_ID, windowDays),
    staleTime: 60_000,
  });

  // ── Aggregate stats ────────────────────────────────────────────────────────

  const items = data?.items ?? [];
  const totalViews = items.reduce((sum, i) => sum + i.totalViews, 0);
  const totalUniqueUsers = items.reduce((sum, i) => sum + i.uniqueUsers, 0);
  const totalExports = items.reduce((sum, i) => sum + i.exportCount, 0);
  const avgDuration =
    items.length > 0
      ? items.reduce((sum, i) => sum + i.avgDurationSeconds, 0) / items.length
      : 0;

  // ── Render ─────────────────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (isError) {
    return (
      <PageContainer>
        <PageErrorState onRetry={() => refetch()} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* IsSimulated banner */}
      <div className="mb-4 rounded-lg border border-warning/30 bg-warning/8 px-4 py-2 text-xs text-warning font-medium flex items-center gap-2">
        <FlaskConical size={14} />
        {t(
          'governance.simulated',
          'Simulated data — live analytics via API in production',
        )}
      </div>

      <PageHeader
        title={t('governance.usageAnalytics.title', 'Dashboard Usage Analytics')}
        subtitle={t(
          'governance.usageAnalytics.subtitle',
          'View engagement metrics across all published dashboards.',
        )}
        actions={
          /* Time window selector */
          <div className="flex items-center gap-1 rounded-lg border border-edge bg-elevated p-0.5">
            {WINDOW_OPTIONS.map((opt) => (
              <button
                key={opt.days}
                onClick={() => setWindowDays(opt.days)}
                className={`
                  px-3 py-1.5 rounded-md text-xs font-semibold transition-all
                  ${
                    windowDays === opt.days
                      ? 'bg-accent text-on-accent shadow-sm'
                      : 'text-muted hover:text-body hover:bg-hover'
                  }
                `}
              >
                {opt.label}
              </button>
            ))}
          </div>
        }
      />

      {/* KPI summary row */}
      <StatsGrid columns={4}>
        <StatCard
          title={t('governance.usageAnalytics.totalViews', 'Total Views')}
          value={totalViews.toLocaleString()}
          icon={<Eye size={18} />}
          color="text-accent"
        />
        <StatCard
          title={t('governance.usageAnalytics.uniqueUsers', 'Unique Users')}
          value={totalUniqueUsers.toLocaleString()}
          icon={<Users size={18} />}
          color="text-info"
        />
        <StatCard
          title={t('governance.usageAnalytics.totalExports', 'Exports')}
          value={totalExports.toLocaleString()}
          icon={<Download size={18} />}
          color="text-success"
        />
        <StatCard
          title={t('governance.usageAnalytics.avgDuration', 'Avg Duration')}
          value={formatDuration(avgDuration)}
          icon={<Clock size={18} />}
          color="text-warning"
        />
      </StatsGrid>

      {/* Window label */}
      {data && (
        <p className="text-xs text-muted mb-4 flex items-center gap-1.5">
          <CalendarDays size={12} />
          {t('governance.usageAnalytics.window', 'Window')}:{' '}
          {new Date(data.windowFrom).toLocaleDateString()} —{' '}
          {new Date(data.windowTo).toLocaleDateString()}
        </p>
      )}

      <PageSection>
        {items.length === 0 ? (
          <EmptyState
            icon={<BarChart3 size={24} />}
            title={t('governance.usageAnalytics.empty', 'No usage data')}
            description={t(
              'governance.usageAnalytics.emptyDesc',
              'Usage events will appear here once dashboards are viewed.',
            )}
          />
        ) : (
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Activity size={14} className="text-muted" />
                <span className="text-xs font-semibold text-heading uppercase tracking-wider">
                  {t('governance.usageAnalytics.tableTitle', 'Dashboard Engagement')}
                </span>
                <Badge variant="neutral" size="sm">
                  {items.length}
                </Badge>
              </div>
            </CardHeader>
            <CardBody className="px-0 py-0">
              <div className="overflow-x-auto">
                <table className="w-full text-left">
                  <thead>
                    <tr className="border-b border-edge bg-elevated/40">
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted w-8">#</th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted">
                        {t('governance.usageAnalytics.colDashboard', 'Dashboard')}
                      </th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted text-right">
                        {t('governance.usageAnalytics.colViews', 'Views')}
                      </th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted text-right">
                        {t('governance.usageAnalytics.colUsers', 'Users')}
                      </th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted text-right">
                        {t('governance.usageAnalytics.colExports', 'Exports')}
                      </th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted text-right">
                        {t('governance.usageAnalytics.colAvgDuration', 'Avg Duration')}
                      </th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted text-right">
                        {t('governance.usageAnalytics.colLastViewed', 'Last Viewed')}
                      </th>
                      <th className="px-4 py-2.5 text-xs font-semibold text-muted text-right">
                        {t('governance.usageAnalytics.colTopPersona', 'Top Persona')}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((item, idx) => (
                      <UsageRow key={item.dashboardId} item={item} rank={idx + 1} />
                    ))}
                  </tbody>
                </table>
              </div>
            </CardBody>
          </Card>
        )}
      </PageSection>

      {/* Export hint */}
      <div className="mt-2 flex justify-end">
        <Button variant="ghost" size="sm">
          <Download size={13} />
          {t('governance.usageAnalytics.exportCsv', 'Export CSV')}
        </Button>
      </div>
    </PageContainer>
  );
}
