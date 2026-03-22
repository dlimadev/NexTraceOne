import * as React from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  AlertTriangle,
  ArrowDown,
  ArrowUp,
  BarChart3,
  CheckCircle2,
  Clock,
  Info,
  Minus,
  RefreshCw,
  Search,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { runtimeIntelligenceApi } from '../api/runtimeIntelligence';

// ── Helpers ──────────────────────────────────────────────────────────────────

function deltaVariant(delta: number): 'success' | 'warning' | 'danger' | 'default' {
  if (Math.abs(delta) < 5) return 'default';
  return delta > 0 ? 'danger' : 'success';
}

function severityVariant(severity: string): 'danger' | 'warning' | 'info' | 'default' {
  switch (severity?.toUpperCase()) {
    case 'CRITICAL':
    case 'HIGH':
      return 'danger';
    case 'MEDIUM':
      return 'warning';
    case 'LOW':
      return 'info';
    default:
      return 'default';
  }
}

function gradeColor(grade: string): string {
  switch (grade?.toUpperCase()) {
    case 'A':
      return 'text-green-400';
    case 'B':
      return 'text-lime-400';
    case 'C':
      return 'text-yellow-400';
    case 'D':
      return 'text-orange-400';
    default:
      return 'text-red-400';
  }
}

function DeltaIcon({ delta }: { delta: number }) {
  if (Math.abs(delta) < 0.5) return <Minus size={14} className="text-slate-400" />;
  return delta > 0 ? (
    <ArrowUp size={14} className="text-red-400" />
  ) : (
    <ArrowDown size={14} className="text-green-400" />
  );
}

// ── Constants ────────────────────────────────────────────────────────────────

const MILLIS_PER_HOUR = 3_600_000;

// ── Types ────────────────────────────────────────────────────────────────────

interface CompareForm {
  serviceName: string;
  environment: string;
  beforeStart: string;
  beforeEnd: string;
  afterStart: string;
  afterEnd: string;
}

const defaultForm: CompareForm = {
  serviceName: '',
  environment: 'production',
  beforeStart: new Date(Date.now() - 14 * 24 * MILLIS_PER_HOUR).toISOString().slice(0, 16),
  beforeEnd: new Date(Date.now() - 7 * 24 * MILLIS_PER_HOUR).toISOString().slice(0, 16),
  afterStart: new Date(Date.now() - 7 * 24 * MILLIS_PER_HOUR).toISOString().slice(0, 16),
  afterEnd: new Date().toISOString().slice(0, 16),
};

// ── Page ─────────────────────────────────────────────────────────────────────

export const EnvironmentComparisonPage: React.FC = () => {
  const { t } = useTranslation();
  const [form, setForm] = useState<CompareForm>(defaultForm);
  const [submitted, setSubmitted] = useState<CompareForm | null>(null);

  const compareQuery = useQuery({
    queryKey: ['runtime-compare', submitted],
    queryFn: () =>
      submitted
        ? runtimeIntelligenceApi.compareReleaseRuntime({
            serviceName: submitted.serviceName,
            environment: submitted.environment,
            beforeStart: new Date(submitted.beforeStart).toISOString(),
            beforeEnd: new Date(submitted.beforeEnd).toISOString(),
            afterStart: new Date(submitted.afterStart).toISOString(),
            afterEnd: new Date(submitted.afterEnd).toISOString(),
          })
        : Promise.resolve(null),
    enabled: !!submitted,
    staleTime: 30_000,
  });

  const driftQuery = useQuery({
    queryKey: ['runtime-drift', submitted?.serviceName, submitted?.environment],
    queryFn: () =>
      submitted
        ? runtimeIntelligenceApi.getDriftFindings({
            serviceName: submitted.serviceName || undefined,
            environment: submitted.environment || undefined,
            unacknowledgedOnly: true,
            pageSize: 20,
          })
        : Promise.resolve(null),
    enabled: !!submitted,
    staleTime: 30_000,
  });

  const scoreQuery = useQuery({
    queryKey: ['runtime-score', submitted?.serviceName, submitted?.environment],
    queryFn: () =>
      submitted?.serviceName && submitted?.environment
        ? runtimeIntelligenceApi.getObservabilityScore({
            serviceName: submitted.serviceName,
            environment: submitted.environment,
          })
        : Promise.resolve(null),
    enabled: !!(submitted?.serviceName && submitted?.environment),
    staleTime: 30_000,
  });

  const timelineQuery = useQuery({
    queryKey: ['runtime-timeline', submitted?.serviceName, submitted?.environment],
    queryFn: () =>
      submitted?.serviceName && submitted?.environment
        ? runtimeIntelligenceApi.getReleaseHealthTimeline({
            serviceName: submitted.serviceName,
            environment: submitted.environment,
            windowStart: submitted
              ? new Date(submitted.beforeStart).toISOString()
              : new Date(Date.now() - 14 * 24 * MILLIS_PER_HOUR).toISOString(),
            windowEnd: submitted
              ? new Date(submitted.afterEnd).toISOString()
              : new Date().toISOString(),
          })
        : Promise.resolve(null),
    enabled: !!(submitted?.serviceName && submitted?.environment),
    staleTime: 30_000,
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.serviceName.trim()) return;
    setSubmitted({ ...form });
  };

  const handleRefresh = () => {
    if (!submitted) return;
    compareQuery.refetch();
    driftQuery.refetch();
    scoreQuery.refetch();
    timelineQuery.refetch();
  };

  const isLoading = (compareQuery.isLoading || driftQuery.isLoading) && !!submitted;
  const hasError = compareQuery.isError || driftQuery.isError;

  return (
    <PageContainer>
      <PageHeader
        title={t('environmentComparison.title')}
        subtitle={t('environmentComparison.subtitle')}
      />

      {/* ── Filter Form ── */}
      <PageSection>
        <Card>
          <CardHeader>
            <span className="font-semibold text-sm text-text-primary">
              {t('environmentComparison.compareParameters')}
            </span>
          </CardHeader>
          <CardBody>
            <form
              onSubmit={handleSubmit}
              className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
            >
              <div className="flex flex-col gap-1">
                <label htmlFor="ec-service" className="text-xs text-text-muted">
                  {t('environmentComparison.serviceName')}
                </label>
                <input
                  id="ec-service"
                  type="text"
                  value={form.serviceName}
                  onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value }))}
                  placeholder={t('environmentComparison.serviceNamePlaceholder')}
                  className="rounded border border-border bg-surface px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="ec-env" className="text-xs text-text-muted">
                  {t('environmentComparison.environment')}
                </label>
                <select
                  id="ec-env"
                  value={form.environment}
                  onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                  className="rounded border border-border bg-surface px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-accent"
                >
                  {['dev', 'test', 'qa', 'uat', 'staging', 'production'].map((env) => (
                    <option key={env} value={env}>
                      {env}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="ec-before-start" className="text-xs text-text-muted">
                  {t('environmentComparison.beforePeriodStart')}
                </label>
                <input
                  id="ec-before-start"
                  type="datetime-local"
                  value={form.beforeStart}
                  onChange={(e) => setForm((f) => ({ ...f, beforeStart: e.target.value }))}
                  className="rounded border border-border bg-surface px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="ec-before-end" className="text-xs text-text-muted">
                  {t('environmentComparison.beforePeriodEnd')}
                </label>
                <input
                  id="ec-before-end"
                  type="datetime-local"
                  value={form.beforeEnd}
                  onChange={(e) => setForm((f) => ({ ...f, beforeEnd: e.target.value }))}
                  className="rounded border border-border bg-surface px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="ec-after-start" className="text-xs text-text-muted">
                  {t('environmentComparison.afterPeriodStart')}
                </label>
                <input
                  id="ec-after-start"
                  type="datetime-local"
                  value={form.afterStart}
                  onChange={(e) => setForm((f) => ({ ...f, afterStart: e.target.value }))}
                  className="rounded border border-border bg-surface px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div className="flex flex-col gap-1">
                <label htmlFor="ec-after-end" className="text-xs text-text-muted">
                  {t('environmentComparison.afterPeriodEnd')}
                </label>
                <input
                  id="ec-after-end"
                  type="datetime-local"
                  value={form.afterEnd}
                  onChange={(e) => setForm((f) => ({ ...f, afterEnd: e.target.value }))}
                  className="rounded border border-border bg-surface px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div className="flex items-end gap-2 lg:col-span-3">
                <Button type="submit" variant="primary" size="sm">
                  <Search size={14} />
                  {t('environmentComparison.compare')}
                </Button>
                {submitted && (
                  <Button type="button" variant="ghost" size="sm" onClick={handleRefresh}>
                    <RefreshCw size={14} />
                    {t('common.refresh')}
                  </Button>
                )}
              </div>
            </form>
          </CardBody>
        </Card>
      </PageSection>

      {/* ── Loading ── */}
      {isLoading && (
        <PageSection>
          <PageLoadingState />
        </PageSection>
      )}

      {/* ── Error ── */}
      {hasError && !isLoading && (
        <PageSection>
          <PageErrorState
            title={t('environmentComparison.errorTitle')}
            message={t('environmentComparison.errorMessage')}
            action={
              <Button variant="secondary" size="sm" onClick={handleRefresh}>
                <RefreshCw size={14} />
                {t('common.refresh')}
              </Button>
            }
          />
        </PageSection>
      )}

      {/* ── No service selected ── */}
      {!submitted && (
        <PageSection>
          <div className="flex flex-col items-center justify-center py-16 text-center gap-3 text-text-muted">
            <BarChart3 size={40} className="opacity-30" />
            <p className="text-sm">{t('environmentComparison.emptyState')}</p>
          </div>
        </PageSection>
      )}

      {/* ── Results ── */}
      {submitted && !isLoading && !hasError && (
        <>
          {/* Observability Score */}
          {scoreQuery.data && (
            <PageSection title={t('environmentComparison.observabilityScore')}>
              <Card>
                <CardBody>
                  <div className="flex items-center gap-6">
                    <div className="flex flex-col items-center">
                      <span
                        className={`text-5xl font-bold ${gradeColor(scoreQuery.data.grade)}`}
                      >
                        {scoreQuery.data.grade}
                      </span>
                      <span className="text-xs text-text-muted mt-1">
                        {t('environmentComparison.grade')}
                      </span>
                    </div>
                    <div className="flex flex-col gap-1">
                      <span className="text-2xl font-semibold text-text-primary">
                        {scoreQuery.data.score.toFixed(1)}
                        <span className="text-sm text-text-muted"> / 100</span>
                      </span>
                      <span className="text-sm text-text-muted">{scoreQuery.data.level}</span>
                      <span className="text-xs text-text-muted">
                        {scoreQuery.data.serviceName} · {scoreQuery.data.environment}
                      </span>
                    </div>
                    <div className="ml-auto grid grid-cols-2 gap-3 text-xs text-text-muted">
                      {Object.entries(scoreQuery.data.breakdown).map(([key, val]) => (
                        <div key={key} className="flex flex-col items-end">
                          <span className="font-medium text-text-primary">
                            {(val as number).toFixed(1)}
                          </span>
                          <span>
                            {t(`environmentComparison.breakdown.${key}`, {
                              defaultValue: key,
                            })}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </CardBody>
              </Card>
            </PageSection>
          )}

          {/* Metrics Comparison */}
          {compareQuery.data && (
            <PageSection title={t('environmentComparison.metricsComparison')}>
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2 text-sm text-text-muted">
                    <span>{t('environmentComparison.baselineLabel')}</span>
                    <Badge variant="info">{t('environmentComparison.beforePeriod')}</Badge>
                    <span>{t('common.vs')}</span>
                    <Badge variant="warning">{t('environmentComparison.afterPeriod')}</Badge>
                    <span className="ml-auto text-xs">
                      {compareQuery.data.beforeDataPoints} / {compareQuery.data.afterDataPoints}{' '}
                      {t('environmentComparison.dataPoints')}
                    </span>
                  </div>
                </CardHeader>
                <CardBody>
                  {compareQuery.data.beforeDataPoints === 0 &&
                  compareQuery.data.afterDataPoints === 0 ? (
                    <div className="flex items-center gap-2 text-sm text-text-muted py-4">
                      <Info size={16} />
                      {t('environmentComparison.noDataForPeriod')}
                    </div>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="text-left text-xs text-text-muted border-b border-border">
                            <th className="pb-2 pr-4">{t('environmentComparison.metric')}</th>
                            <th className="pb-2 pr-4">{t('environmentComparison.before')}</th>
                            <th className="pb-2 pr-4">{t('environmentComparison.after')}</th>
                            <th className="pb-2">{t('environmentComparison.delta')}</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                          {[
                            {
                              label: t('environmentComparison.metrics.avgLatency'),
                              before: compareQuery.data.beforeMetrics.avgLatencyMs,
                              after: compareQuery.data.afterMetrics.avgLatencyMs,
                              unit: 'ms',
                              delta: compareQuery.data.latencyDeltaPercent,
                            },
                            {
                              label: t('environmentComparison.metrics.p99Latency'),
                              before: compareQuery.data.beforeMetrics.p99LatencyMs,
                              after: compareQuery.data.afterMetrics.p99LatencyMs,
                              unit: 'ms',
                              delta: compareQuery.data.beforeMetrics.p99LatencyMs > 0
                                ? Math.round(((compareQuery.data.afterMetrics.p99LatencyMs - compareQuery.data.beforeMetrics.p99LatencyMs) / compareQuery.data.beforeMetrics.p99LatencyMs) * 100 * 100) / 100
                                : 0,
                            },
                            {
                              label: t('environmentComparison.metrics.errorRate'),
                              before: compareQuery.data.beforeMetrics.errorRate * 100,
                              after: compareQuery.data.afterMetrics.errorRate * 100,
                              unit: '%',
                              delta: compareQuery.data.errorRateDeltaPercent,
                            },
                            {
                              label: t('environmentComparison.metrics.throughput'),
                              before: compareQuery.data.beforeMetrics.requestsPerSecond,
                              after: compareQuery.data.afterMetrics.requestsPerSecond,
                              unit: 'rps',
                              delta: compareQuery.data.throughputDeltaPercent,
                            },
                            {
                              label: t('environmentComparison.metrics.cpu'),
                              before: compareQuery.data.beforeMetrics.cpuUsagePercent,
                              after: compareQuery.data.afterMetrics.cpuUsagePercent,
                              unit: '%',
                              // API does not provide a pre-computed delta for resource metrics
                              delta: null as number | null,
                            },
                            {
                              label: t('environmentComparison.metrics.memory'),
                              before: compareQuery.data.beforeMetrics.memoryUsageMb,
                              after: compareQuery.data.afterMetrics.memoryUsageMb,
                              unit: 'MB',
                              delta: null as number | null,
                            },
                          ].map((row) => (
                            <tr key={row.label} className="text-text-primary">
                              <td className="py-2 pr-4 text-text-muted">{row.label}</td>
                              <td className="py-2 pr-4">
                                {row.before.toFixed(2)} {row.unit}
                              </td>
                              <td className="py-2 pr-4">
                                {row.after.toFixed(2)} {row.unit}
                              </td>
                              <td className="py-2">
                                {row.delta !== null && row.delta !== 0 ? (
                                  <div className="flex items-center gap-1">
                                    <DeltaIcon delta={row.delta} />
                                    <Badge variant={deltaVariant(row.delta)}>
                                      {row.delta > 0 ? '+' : ''}
                                      {row.delta.toFixed(1)}%
                                    </Badge>
                                  </div>
                                ) : (
                                  <span className="text-text-muted">—</span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </CardBody>
              </Card>
            </PageSection>
          )}

          {/* Drift Findings */}
          <PageSection title={t('environmentComparison.driftFindings')}>
            {driftQuery.isLoading ? (
              <PageLoadingState />
            ) : driftQuery.data?.items.length === 0 ? (
              <Card>
                <CardBody>
                  <div className="flex items-center gap-2 text-sm text-green-400 py-2">
                    <CheckCircle2 size={16} />
                    {t('environmentComparison.noDriftFindings')}
                  </div>
                </CardBody>
              </Card>
            ) : (
              <div className="flex flex-col gap-3">
                {(driftQuery.data?.items ?? []).map((finding) => (
                  <Card key={finding.id}>
                    <CardBody>
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex items-center gap-2">
                          <AlertTriangle
                            size={16}
                            className={
                              finding.severity === 'Critical' || finding.severity === 'High'
                                ? 'text-red-400'
                                : 'text-yellow-400'
                            }
                          />
                          <div className="flex flex-col gap-0.5">
                            <span className="text-sm font-medium text-text-primary">
                              {finding.metricName}
                            </span>
                            <span className="text-xs text-text-muted">
                              {finding.serviceName} · {finding.environment}
                            </span>
                          </div>
                        </div>
                        <Badge variant={severityVariant(finding.severity)}>
                          {finding.severity}
                        </Badge>
                      </div>
                      <div className="mt-3 grid grid-cols-3 gap-4 text-xs text-text-muted">
                        <div>
                          <span className="block text-text-muted">
                            {t('environmentComparison.expected')}
                          </span>
                          <span className="text-text-primary font-medium">
                            {finding.expectedValue.toFixed(4)}
                          </span>
                        </div>
                        <div>
                          <span className="block text-text-muted">
                            {t('environmentComparison.actual')}
                          </span>
                          <span className="text-text-primary font-medium">
                            {finding.actualValue.toFixed(4)}
                          </span>
                        </div>
                        <div>
                          <span className="block text-text-muted">
                            {t('environmentComparison.deviation')}
                          </span>
                          <span
                            className={`font-medium ${
                              finding.deviationPercent > 20 ? 'text-red-400' : 'text-yellow-400'
                            }`}
                          >
                            {finding.deviationPercent.toFixed(1)}%
                          </span>
                        </div>
                      </div>
                    </CardBody>
                  </Card>
                ))}
              </div>
            )}
          </PageSection>

          {/* Release Health Timeline */}
          {timelineQuery.data && timelineQuery.data.points.length > 0 && (
            <PageSection title={t('environmentComparison.releaseTimeline')}>
              <Card>
                <CardBody>
                  <div className="flex flex-col gap-2">
                    {timelineQuery.data.points.map((point, idx) => (
                      <div
                        key={idx}
                        className="flex items-center justify-between gap-4 py-2 border-b border-border last:border-0 text-sm"
                      >
                        <div className="flex items-center gap-2 text-text-muted">
                          <Clock size={14} />
                          <span>{new Date(point.periodStart).toLocaleDateString()}</span>
                          {point.releaseName && (
                            <Badge variant="info">{point.releaseName}</Badge>
                          )}
                        </div>
                        <div className="flex items-center gap-4 text-xs text-text-muted">
                          <span>
                            {t('environmentComparison.metrics.avgLatency')}:{' '}
                            <span className="text-text-primary">
                              {point.avgLatencyMs.toFixed(1)}ms
                            </span>
                          </span>
                          <span>
                            {t('environmentComparison.metrics.errorRate')}:{' '}
                            <span
                              className={
                                point.errorRate > 0.05 ? 'text-red-400' : 'text-green-400'
                              }
                            >
                              {(point.errorRate * 100).toFixed(2)}%
                            </span>
                          </span>
                          <span>
                            {t('environmentComparison.snapshots')}:{' '}
                            <span className="text-text-primary">{point.snapshotCount}</span>
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardBody>
              </Card>
            </PageSection>
          )}
        </>
      )}
    </PageContainer>
  );
};
