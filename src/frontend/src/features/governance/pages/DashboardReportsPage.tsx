import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  CalendarClock,
  Plus,
  ToggleLeft,
  ToggleRight,
  Users,
  CheckCircle,
  XCircle,
  Clock,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { reportsApi } from '../api/reports';
import type { ReportFormat, ScheduledReport } from '../api/reports';

// ── Simulated data ────────────────────────────────────────────────────────────

const SIMULATED_REPORTS: ScheduledReport[] = [
  {
    id: 'sr-001',
    dashboardId: 'db-engineer-home',
    dashboardName: 'Engineer Home',
    cronExpression: '0 8 * * 1',
    format: 'PDF',
    recipients: ['eng-team@acme.com', 'platform@acme.com'],
    retentionDays: 30,
    isActive: true,
    lastRunAt: new Date(Date.now() - 7 * 86_400_000).toISOString(),
    nextRunAt: new Date(Date.now() + 1 * 86_400_000).toISOString(),
    successCount: 12,
    failureCount: 0,
  },
  {
    id: 'sr-002',
    dashboardId: 'db-exec-overview',
    dashboardName: 'Executive Overview',
    cronExpression: '0 7 * * *',
    format: 'PDF',
    recipients: ['cto@acme.com', 'cpo@acme.com', 'cfo@acme.com'],
    retentionDays: 90,
    isActive: true,
    lastRunAt: new Date(Date.now() - 86_400_000).toISOString(),
    nextRunAt: new Date(Date.now() + 3_600_000).toISOString(),
    successCount: 45,
    failureCount: 1,
  },
  {
    id: 'sr-003',
    dashboardId: 'db-finops',
    dashboardName: 'FinOps Budget Tracker',
    cronExpression: '0 9 1 * *',
    format: 'PNG',
    recipients: ['finops@acme.com'],
    retentionDays: 365,
    isActive: false,
    lastRunAt: new Date(Date.now() - 35 * 86_400_000).toISOString(),
    nextRunAt: null,
    successCount: 3,
    failureCount: 2,
  },
];

// ── Hooks ─────────────────────────────────────────────────────────────────────

const QUERY_KEY = ['dashboard-scheduled-reports'] as const;

const useDashboardReports = () =>
  useQuery({
    queryKey: QUERY_KEY,
    queryFn: () => reportsApi.getUsageAnalytics('default', 30),
    // returns simulated list via the analytics endpoint; in production this
    // would be a dedicated GET /api/v1/governance/dashboards/scheduled-reports
    select: () => SIMULATED_REPORTS,
    staleTime: 60_000,
  });

const useToggleReport = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ report }: { report: ScheduledReport }) =>
      reportsApi.scheduleReport(report.dashboardId, {
        cronExpression: report.cronExpression,
        format: report.format,
        recipients: report.recipients,
        retentionDays: report.retentionDays,
        isActive: !report.isActive,
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
};

// ── Helpers ───────────────────────────────────────────────────────────────────

const FORMAT_VARIANT: Record<ReportFormat, 'info' | 'warning'> = {
  PDF: 'info',
  PNG: 'warning',
};

function humanCron(cron: string): string {
  const parts = cron.split(' ');
  if (parts.length !== 5) return cron;
  const [, , dom, , dow] = parts;
  if (dom === '*' && dow === '*') return 'Daily';
  if (dom === '*' && dow === '1') return 'Weekly (Mon)';
  if (dom === '1' && dow === '*') return 'Monthly (1st)';
  return cron;
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// ── Schedule form state ───────────────────────────────────────────────────────

interface FormState {
  dashboardId: string;
  cronExpression: string;
  format: ReportFormat;
  recipients: string;
  retentionDays: number;
}

const DEFAULT_FORM: FormState = {
  dashboardId: '',
  cronExpression: '0 8 * * 1',
  format: 'PDF',
  recipients: '',
  retentionDays: 30,
};

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * DashboardReportsPage — manage cron-based scheduled reports for dashboards.
 * V3.6: scheduled reports with PDF/PNG export, recipients, retention.
 */
export function DashboardReportsPage() {
  const { t } = useTranslation();
  const { data: reports = [], isLoading, isError, refetch } = useDashboardReports();
  const toggleReport = useToggleReport();

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<FormState>(DEFAULT_FORM);

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError)
    return (
      <PageContainer>
        <PageErrorState onRetry={() => refetch()} />
      </PageContainer>
    );

  const handleToggle = (report: ScheduledReport) => {
    toggleReport.mutate({ report });
  };

  const handleFormChange = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSave = () => {
    setShowForm(false);
    setForm(DEFAULT_FORM);
  };

  return (
    <PageContainer>
      {/* IsSimulated banner */}
      <div className="mb-4 rounded-lg border border-warning/30 bg-warning/8 px-4 py-2 text-xs text-warning font-medium flex items-center gap-2">
        <CalendarClock size={14} />
        {t('governance.simulated', 'Simulated data — live scheduling via API in production')}
      </div>

      <PageHeader
        title={t('governance.dashboardReports.title', 'Scheduled Reports')}
        subtitle={t(
          'governance.dashboardReports.subtitle',
          'Automate PDF/PNG delivery of dashboards to stakeholders on a cron schedule.',
        )}
        actions={
          <Button size="sm" onClick={() => setShowForm(true)}>
            <Plus size={14} />
            {t('governance.dashboardReports.newSchedule', 'New Schedule')}
          </Button>
        }
      />

      <PageSection>
        {reports.length === 0 ? (
          <EmptyState
            icon={<CalendarClock size={24} />}
            title={t('governance.dashboardReports.empty', 'No scheduled reports')}
            description={t(
              'governance.dashboardReports.emptyDesc',
              'Set up automated report delivery for any dashboard.',
            )}
            action={
              <Button size="sm" onClick={() => setShowForm(true)}>
                {t('governance.dashboardReports.newSchedule', 'New Schedule')}
              </Button>
            }
          />
        ) : (
          <div className="space-y-3">
            {reports.map((report) => (
              <Card key={report.id}>
                <CardBody>
                  <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                    {/* Left: toggle + info */}
                    <div className="flex items-start gap-3 min-w-0">
                      <button
                        onClick={() => handleToggle(report)}
                        className="mt-0.5 shrink-0 text-muted hover:text-accent transition-colors"
                        aria-label={report.isActive ? 'Deactivate schedule' : 'Activate schedule'}
                        disabled={toggleReport.isPending}
                      >
                        {report.isActive ? (
                          <ToggleRight size={22} className="text-success" />
                        ) : (
                          <ToggleLeft size={22} />
                        )}
                      </button>

                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-heading truncate">
                          {report.dashboardName}
                        </p>
                        <div className="flex flex-wrap items-center gap-2 mt-1">
                          <Badge variant="default" size="sm">
                            <Clock size={10} className="mr-0.5" />
                            {humanCron(report.cronExpression)}
                          </Badge>
                          <Badge variant={FORMAT_VARIANT[report.format]} size="sm">
                            {report.format}
                          </Badge>
                          <span className="flex items-center gap-1 text-xs text-muted">
                            <Users size={11} />
                            {report.recipients.length}{' '}
                            {report.recipients.length === 1 ? 'recipient' : 'recipients'}
                          </span>
                        </div>
                        <div className="mt-1.5 flex flex-wrap items-center gap-3 text-xs text-muted">
                          {report.lastRunAt && (
                            <span className="flex items-center gap-1">
                              {report.failureCount > 0 ? (
                                <XCircle size={11} className="text-critical" />
                              ) : (
                                <CheckCircle size={11} className="text-success" />
                              )}
                              {t('governance.dashboardReports.lastRun', 'Last run')}:{' '}
                              {formatDate(report.lastRunAt)}
                            </span>
                          )}
                          {report.nextRunAt && report.isActive && (
                            <span>
                              {t('governance.dashboardReports.nextRun', 'Next')}:{' '}
                              {formatDate(report.nextRunAt)}
                            </span>
                          )}
                          <span className="text-success">
                            {report.successCount} ok
                          </span>
                          {report.failureCount > 0 && (
                            <span className="text-critical">{report.failureCount} failed</span>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Right: active badge */}
                    <div className="shrink-0">
                      <Badge variant={report.isActive ? 'success' : 'neutral'}>
                        {report.isActive
                          ? t('governance.dashboardReports.active', 'Active')
                          : t('governance.dashboardReports.inactive', 'Inactive')}
                      </Badge>
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      {/* New Schedule modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-card rounded-2xl border border-edge shadow-elevated w-full max-w-md p-6">
            <h2 className="text-base font-semibold text-heading mb-4">
              {t('governance.dashboardReports.newSchedule', 'New Schedule')}
            </h2>

            <div className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('governance.dashboardReports.dashboardId', 'Dashboard ID')}
                </label>
                <input
                  type="text"
                  value={form.dashboardId}
                  onChange={(e) => handleFormChange('dashboardId', e.target.value)}
                  placeholder="db-engineer-home"
                  className="w-full rounded-lg border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:border-accent"
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('governance.dashboardReports.cron', 'Cron Expression')}
                </label>
                <input
                  type="text"
                  value={form.cronExpression}
                  onChange={(e) => handleFormChange('cronExpression', e.target.value)}
                  placeholder="0 8 * * 1"
                  className="w-full rounded-lg border border-edge bg-elevated px-3 py-2 text-sm text-body font-mono focus:outline-none focus:border-accent"
                />
                <p className="mt-1 text-xs text-muted">{humanCron(form.cronExpression)}</p>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('governance.dashboardReports.format', 'Format')}
                  </label>
                  <select
                    value={form.format}
                    onChange={(e) => handleFormChange('format', e.target.value as ReportFormat)}
                    className="w-full rounded-lg border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:border-accent"
                  >
                    <option value="PDF">PDF</option>
                    <option value="PNG">PNG</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('governance.dashboardReports.retention', 'Retention (days)')}
                  </label>
                  <input
                    type="number"
                    min={1}
                    max={365}
                    value={form.retentionDays}
                    onChange={(e) =>
                      handleFormChange('retentionDays', parseInt(e.target.value, 10) || 30)
                    }
                    className="w-full rounded-lg border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:border-accent"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('governance.dashboardReports.recipients', 'Recipients (comma-separated emails)')}
                </label>
                <input
                  type="text"
                  value={form.recipients}
                  onChange={(e) => handleFormChange('recipients', e.target.value)}
                  placeholder="team@example.com, cto@example.com"
                  className="w-full rounded-lg border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:border-accent"
                />
              </div>
            </div>

            <div className="mt-6 flex items-center justify-end gap-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setShowForm(false);
                  setForm(DEFAULT_FORM);
                }}
              >
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button
                size="sm"
                disabled={!form.dashboardId.trim() || !form.cronExpression.trim()}
                onClick={handleSave}
              >
                {t('common.save', 'Save Schedule')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
