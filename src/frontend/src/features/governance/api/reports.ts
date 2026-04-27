import client from '../../../api/client';

// ── Types ─────────────────────────────────────────────────────────────────────

export type ReportFormat = 'PDF' | 'PNG';

export interface ScheduledReport {
  id: string;
  dashboardId: string;
  dashboardName: string;
  cronExpression: string;
  format: ReportFormat;
  recipients: string[];
  retentionDays: number;
  isActive: boolean;
  lastRunAt?: string | null;
  nextRunAt?: string | null;
  successCount: number;
  failureCount: number;
}

export interface DashboardUsageSummary {
  dashboardId: string;
  dashboardName: string;
  totalViews: number;
  uniqueUsers: number;
  exportCount: number;
  embedCount: number;
  avgDurationSeconds: number;
  lastViewedAt?: string | null;
  topPersona?: string | null;
}

export interface UsageAnalyticsResponse {
  items: DashboardUsageSummary[];
  windowFrom: string;
  windowTo: string;
}

export interface ScheduleReportRequest {
  cronExpression: string;
  format: ReportFormat;
  recipients: string[];
  retentionDays?: number;
  isActive?: boolean;
}

export interface PublishDashboardRequest {
  tenantId: string;
  publishedByUserId: string;
  publishNote?: string | null;
}

export interface DeprecateDashboardRequest {
  tenantId: string;
  deprecatedByUserId: string;
  reason?: string | null;
}

export interface RecordUsageRequest {
  userId: string;
  tenantId: string;
  eventType: 'View' | 'Export' | 'Embed';
  durationSeconds?: number;
  persona?: string | null;
}

// ── API client ────────────────────────────────────────────────────────────────

export const reportsApi = {
  /**
   * POST /api/v1/governance/dashboards/{id}/schedule-report
   * Creates or updates a scheduled report for a dashboard.
   */
  scheduleReport: (dashboardId: string, body: ScheduleReportRequest) =>
    client
      .post<ScheduledReport>(
        `/governance/dashboards/${dashboardId}/schedule-report`,
        body,
      )
      .then((r) => r.data),

  /**
   * GET /api/v1/governance/dashboards/{id}/export-yaml
   * Exports a dashboard definition as YAML (for GitOps / IaC workflows).
   */
  exportYaml: (dashboardId: string, tenantId: string) =>
    client
      .get<{ yaml: string; dashboardId: string; exportedAt: string }>(
        `/governance/dashboards/${dashboardId}/export-yaml`,
        { params: { tenantId } },
      )
      .then((r) => r.data),

  /**
   * POST /api/v1/governance/dashboards/{id}/publish
   * Transitions a dashboard to Published state.
   */
  publish: (dashboardId: string, body: PublishDashboardRequest) =>
    client
      .post<{ dashboardId: string; status: string; publishedAt: string }>(
        `/governance/dashboards/${dashboardId}/publish`,
        body,
      )
      .then((r) => r.data),

  /**
   * POST /api/v1/governance/dashboards/{id}/deprecate
   * Marks a dashboard as deprecated.
   */
  deprecate: (dashboardId: string, body: DeprecateDashboardRequest) =>
    client
      .post<{ dashboardId: string; status: string; deprecatedAt: string }>(
        `/governance/dashboards/${dashboardId}/deprecate`,
        body,
      )
      .then((r) => r.data),

  /**
   * POST /api/v1/governance/dashboards/{id}/record-usage
   * Records a usage event (view, export, embed) for analytics.
   */
  recordUsage: (dashboardId: string, body: RecordUsageRequest) =>
    client
      .post<{ recorded: boolean }>(
        `/governance/dashboards/${dashboardId}/record-usage`,
        body,
      )
      .then((r) => r.data),

  /**
   * GET /api/v1/governance/dashboards/usage-analytics
   * Returns aggregated usage metrics for all dashboards within a time window.
   */
  getUsageAnalytics: (tenantId: string, windowDays = 30) =>
    client
      .get<UsageAnalyticsResponse>('/governance/dashboards/usage-analytics', {
        params: { tenantId, windowDays },
      })
      .then((r) => r.data),
};
