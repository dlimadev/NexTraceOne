import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  CheckCircle2,
  XCircle,
  RefreshCw,
  Server,
  Shield,
  HardDrive,
  Cpu,
  Database,
  BrainCircuit,
  Mail,
  Gauge,
  Globe,
  Activity,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { platformAdminApi } from '../api/platformAdmin';
import type { PreflightCheckStatus } from '../api/platformAdmin';

// ─── Helpers ─────────────────────────────────────────────────────────────────

const statusIcon = (status: PreflightCheckStatus, size = 16) => {
  switch (status) {
    case 'Ok':
      return <CheckCircle2 size={size} className="text-success shrink-0" />;
    case 'Warning':
      return <AlertTriangle size={size} className="text-warning shrink-0" />;
    case 'Error':
      return <XCircle size={size} className="text-critical shrink-0" />;
  }
};

const statusBadge = (status: PreflightCheckStatus): 'success' | 'warning' | 'danger' => {
  switch (status) {
    case 'Ok':
      return 'success';
    case 'Warning':
      return 'warning';
    case 'Error':
      return 'danger';
  }
};

const checkIcon = (name: string) => {
  const lower = name.toLowerCase();
  if (lower.includes('postgresql') || lower.includes('database')) return <Database size={15} className="text-accent" />;
  if (lower.includes('jwt') || lower.includes('secret') || lower.includes('encryption')) return <Shield size={15} className="text-accent" />;
  if (lower.includes('disk')) return <HardDrive size={15} className="text-accent" />;
  if (lower.includes('ram') || lower.includes('cpu')) return <Cpu size={15} className="text-accent" />;
  if (lower.includes('ollama') || lower.includes('ai')) return <BrainCircuit size={15} className="text-accent" />;
  if (lower.includes('smtp') || lower.includes('mail')) return <Mail size={15} className="text-accent" />;
  if (lower.includes('otel') || lower.includes('telemetry')) return <Activity size={15} className="text-accent" />;
  if (lower.includes('cors') || lower.includes('origin')) return <Globe size={15} className="text-accent" />;
  if (lower.includes('port')) return <Gauge size={15} className="text-accent" />;
  if (lower.includes('connection')) return <Server size={15} className="text-accent" />;
  return <Server size={15} className="text-accent" />;
};

// ─── Page ─────────────────────────────────────────────────────────────────────

export function PreflightPage() {
  const { t } = useTranslation();

  const query = useQuery({
    queryKey: ['preflight'],
    queryFn: () => platformAdminApi.getPreflight(),
    staleTime: 30_000,
    retry: 2,
  });

  const report = query.data;

  const overallBg = !report
    ? 'bg-surface-muted'
    : report.overallStatus === 'Ok'
      ? 'bg-success/10 border border-success/20'
      : report.overallStatus === 'Warning'
        ? 'bg-warning/10 border border-warning/20'
        : 'bg-critical/10 border border-critical/20';

  return (
    <div className="min-h-screen bg-background flex flex-col items-center justify-center py-12 px-4">
      {/* Logo / Title area */}
      <div className="mb-8 text-center">
        <div className="inline-flex items-center justify-center w-14 h-14 bg-accent/10 rounded-2xl mb-4">
          <Server size={28} className="text-accent" />
        </div>
        <h1 className="text-2xl font-bold text-heading">{t('preflight.title')}</h1>
        <p className="text-muted text-sm mt-1">{t('preflight.subtitle')}</p>
      </div>

      <div className="w-full max-w-2xl space-y-4">
        {/* Loading state */}
        {query.isLoading && (
          <PageLoadingState message={t('preflight.running')} />
        )}

        {/* Error state */}
        {query.isError && (
          <PageErrorState
            message={t('preflight.fetchError')}
            onRetry={() => query.refetch()}
          />
        )}

        {/* Results */}
        {report && (
          <>
            {/* Overall status banner */}
            <div className={`rounded-xl p-4 ${overallBg}`}>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  {statusIcon(report.overallStatus, 22)}
                  <div>
                    <p className="font-semibold text-heading text-sm">
                      {report.isReadyToStart
                        ? t('preflight.readyToStart')
                        : t('preflight.notReady')}
                    </p>
                    <p className="text-xs text-muted mt-0.5">
                      {t('preflight.version', { version: report.version })} &middot;{' '}
                      {t('preflight.checkedAt', { time: new Date(report.checkedAt).toLocaleTimeString() })}
                    </p>
                  </div>
                </div>
                <Badge variant={statusBadge(report.overallStatus)}>
                  {t(`preflight.status.${report.overallStatus.toLowerCase()}`)}
                </Badge>
              </div>
            </div>

            {/* Individual checks */}
            <Card>
              <CardHeader>
                <span className="text-sm font-semibold text-heading">
                  {t('preflight.checksTitle', { count: report.checks.length })}
                </span>
              </CardHeader>
              <CardBody className="p-0">
                <ul className="divide-y divide-border">
                  {report.checks.map((check) => (
                    <li key={check.name} className="px-4 py-3">
                      <div className="flex items-start gap-3">
                        <div className="mt-0.5">{checkIcon(check.name)}</div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-sm font-medium text-heading truncate">
                              {check.name}
                            </span>
                            <div className="shrink-0">{statusIcon(check.status)}</div>
                          </div>
                          <p className="text-xs text-muted mt-0.5">{check.message}</p>
                          {check.suggestion && (
                            <p className="text-xs text-warning mt-1 flex items-start gap-1">
                              <AlertTriangle size={11} className="mt-0.5 shrink-0" />
                              {check.suggestion}
                            </p>
                          )}
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>
              </CardBody>
            </Card>

            {/* Actions */}
            <div className="flex items-center justify-between pt-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => query.refetch()}
                disabled={query.isFetching}
              >
                <RefreshCw size={14} className={query.isFetching ? 'animate-spin' : ''} />
                {t('preflight.reRun')}
              </Button>
              {report.isReadyToStart && (
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => { window.location.href = '/'; }}
                >
                  {t('preflight.goToApp')}
                </Button>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
