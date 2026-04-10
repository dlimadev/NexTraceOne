/**
 * LogExplorerPage — Structured log search with trace correlation.
 *
 * Provides real-time log search with filtering by service, level,
 * message and trace ID. Clicking a trace ID navigates to the trace detail.
 * Connected to /api/v1/telemetry/logs.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  AlertTriangle,
  FileText,
  Info,
  Search,
  XCircle,
  ChevronDown,
  ChevronUp,
  ExternalLink,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { queryLogs, type LogEntry } from '../api/telemetry';

// ── Helpers ──────────────────────────────────────────────────────────────────

function formatTimestamp(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    fractionalSecondDigits: 3,
  });
}

function levelVariant(level: string): 'danger' | 'warning' | 'info' | 'success' | 'default' {
  switch (level?.toLowerCase()) {
    case 'error':
    case 'fatal':
    case 'critical':
      return 'danger';
    case 'warning':
    case 'warn':
      return 'warning';
    case 'information':
    case 'info':
      return 'info';
    case 'debug':
    case 'trace':
    case 'verbose':
      return 'default';
    default:
      return 'default';
  }
}

function getDefaultTimeRange(): { from: string; until: string } {
  const now = new Date();
  const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
  return {
    from: oneHourAgo.toISOString(),
    until: now.toISOString(),
  };
}

// ── Component ────────────────────────────────────────────────────────────────

export function LogExplorerPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const defaults = getDefaultTimeRange();

  // Filter state
  const [environment, setEnvironment] = useState('production');
  const [serviceName, setServiceName] = useState('');
  const [level, setLevel] = useState('');
  const [messageContains, setMessageContains] = useState('');
  const [traceIdFilter, setTraceIdFilter] = useState('');
  const [from, setFrom] = useState(defaults.from);
  const [until, setUntil] = useState(defaults.until);
  const [limit, setLimit] = useState(100);

  // Expanded exception row
  const [expandedRow, setExpandedRow] = useState<number | null>(null);

  // Query
  const logsQuery = useQuery({
    queryKey: ['telemetry', 'logs', environment, from, until, serviceName, level, messageContains, traceIdFilter, limit],
    queryFn: () =>
      queryLogs({
        environment,
        from,
        until,
        serviceName: serviceName || undefined,
        level: level || undefined,
        messageContains: messageContains || undefined,
        traceId: traceIdFilter || undefined,
        limit,
      }),
    enabled: !!environment && !!from && !!until,
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('telemetryExplorer.logs.title')}
        subtitle={t('telemetryExplorer.logs.subtitle')}
        icon={<FileText className="w-6 h-6 text-primary" />}
      />

      {/* Filters */}
      <PageSection>
        <Card>
          <CardBody className="p-4">
            <div className="flex flex-wrap items-end gap-3">
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.filters.environment')}
                </label>
                <select
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm"
                  value={environment}
                  onChange={(e) => setEnvironment(e.target.value)}
                >
                  <option value="production">Production</option>
                  <option value="staging">Staging</option>
                  <option value="development">Development</option>
                </select>
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.logs.service')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-40"
                  placeholder={t('telemetryExplorer.logs.service')}
                  value={serviceName}
                  onChange={(e) => setServiceName(e.target.value)}
                />
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.logs.level')}
                </label>
                <select
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm"
                  value={level}
                  onChange={(e) => setLevel(e.target.value)}
                >
                  <option value="">{t('telemetryExplorer.logs.allLevels')}</option>
                  <option value="Error">Error</option>
                  <option value="Warning">Warning</option>
                  <option value="Information">Information</option>
                  <option value="Debug">Debug</option>
                </select>
              </div>
              <div className="flex-1 min-w-[200px]">
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.logs.message')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-full"
                  placeholder={t('telemetryExplorer.logs.searchPlaceholder')}
                  value={messageContains}
                  onChange={(e) => setMessageContains(e.target.value)}
                />
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground block mb-1">
                  {t('telemetryExplorer.logs.traceId')}
                </label>
                <input
                  className="h-9 rounded-md border border-input bg-background px-3 text-sm w-40 font-mono"
                  placeholder={t('operations.logExplorer.traceIdPlaceholder', 'Trace ID')}
                  value={traceIdFilter}
                  onChange={(e) => setTraceIdFilter(e.target.value)}
                />
              </div>
              <Button size="sm" onClick={() => logsQuery.refetch()}>
                <Search className="w-4 h-4 mr-1" />
                {t('telemetryExplorer.filters.apply')}
              </Button>
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Results */}
      <PageSection>
        {logsQuery.isLoading && <PageLoadingState message={t('telemetryExplorer.loading')} />}
        {logsQuery.isError && <PageErrorState message={t('telemetryExplorer.error')} />}

        {logsQuery.data && (
          <Card>
            <CardBody className="p-0">
              {logsQuery.data.length === 0 ? (
                <div className="text-center py-12">
                  <FileText className="w-10 h-10 text-muted-foreground mx-auto mb-3" />
                  <p className="text-sm font-medium">{t('telemetryExplorer.logs.noLogs')}</p>
                  <p className="text-xs text-muted-foreground mt-1">{t('telemetryExplorer.logs.noLogsDescription')}</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-border bg-muted/30">
                        <th className="text-left px-4 py-2 font-medium w-[180px]">{t('telemetryExplorer.logs.timestamp')}</th>
                        <th className="text-left px-4 py-2 font-medium w-[80px]">{t('telemetryExplorer.logs.level')}</th>
                        <th className="text-left px-4 py-2 font-medium w-[140px]">{t('telemetryExplorer.logs.service')}</th>
                        <th className="text-left px-4 py-2 font-medium">{t('telemetryExplorer.logs.message')}</th>
                        <th className="text-left px-4 py-2 font-medium w-[160px]">{t('telemetryExplorer.logs.traceId')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {logsQuery.data.map((log: LogEntry, index: number) => (
                        <React.Fragment key={`${log.timestamp}-${index}`}>
                          <tr className="hover:bg-muted/50 transition-colors">
                            <td className="px-4 py-2 text-xs text-muted-foreground whitespace-nowrap">
                              {formatTimestamp(log.timestamp)}
                            </td>
                            <td className="px-4 py-2">
                              <Badge variant={levelVariant(log.level)} size="sm">
                                {log.level}
                              </Badge>
                            </td>
                            <td className="px-4 py-2 truncate max-w-[140px]">{log.serviceName}</td>
                            <td className="px-4 py-2">
                              <div className="flex items-center gap-2">
                                <span className="truncate max-w-[400px]">{log.message}</span>
                                {log.exception && (
                                  <button
                                    type="button"
                                    className="text-muted-foreground hover:text-foreground flex-shrink-0"
                                    onClick={() => setExpandedRow(expandedRow === index ? null : index)}
                                  >
                                    {expandedRow === index ? (
                                      <ChevronUp className="w-4 h-4" />
                                    ) : (
                                      <ChevronDown className="w-4 h-4" />
                                    )}
                                  </button>
                                )}
                              </div>
                            </td>
                            <td className="px-4 py-2">
                              {log.traceId && (
                                <button
                                  type="button"
                                  className="font-mono text-xs text-primary hover:underline truncate max-w-[150px] flex items-center gap-1"
                                  onClick={() => navigate(`/operations/telemetry/traces?traceId=${log.traceId}`)}
                                >
                                  {log.traceId.substring(0, 16)}…
                                  <ExternalLink className="w-3 h-3 flex-shrink-0" />
                                </button>
                              )}
                            </td>
                          </tr>
                          {/* Exception detail expanded row */}
                          {expandedRow === index && log.exception && (
                            <tr>
                              <td colSpan={5} className="px-4 py-3 bg-destructive/5">
                                <div className="flex items-start gap-2">
                                  <XCircle className="w-4 h-4 text-destructive flex-shrink-0 mt-0.5" />
                                  <pre className="text-xs font-mono whitespace-pre-wrap text-destructive/90 overflow-x-auto max-w-full">
                                    {log.exception}
                                  </pre>
                                </div>
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>
        )}
      </PageSection>
    </PageContainer>
  );
}
