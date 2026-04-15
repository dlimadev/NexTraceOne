import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  RefreshCw,
  Server,
  Package,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { platformAdminApi } from '../api/platformAdmin';
import type { StartupReportEntry } from '../api/platformAdmin';

// ─── Report card ─────────────────────────────────────────────────────────────

function ReportCard({ report, index }: { report: StartupReportEntry; index: number }) {
  const { t } = useTranslation();
  const isLatest = index === 0;

  return (
    <Card>
      <CardHeader className="flex items-center gap-2">
        <Server size={16} className="text-muted shrink-0" />
        <span className="font-medium text-sm">
          {report.version} — {report.build}
        </span>
        {isLatest && (
          <Badge variant="success" className="ml-auto">
            {t('startupReport.latest')}
          </Badge>
        )}
        <span className={`text-xs text-muted ${isLatest ? '' : 'ml-auto'}`}>
          {new Date(report.startedAt).toLocaleString()}
        </span>
      </CardHeader>
      <CardBody className="space-y-4">
        {/* Metadata row */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div>
            <p className="text-xs text-muted mb-0.5">{t('startupReport.environment')}</p>
            <p className="text-sm font-medium">{report.environment}</p>
          </div>
          <div>
            <p className="text-xs text-muted mb-0.5">{t('startupReport.hostname')}</p>
            <p className="text-sm font-medium font-mono truncate">{report.hostname}</p>
          </div>
          <div>
            <p className="text-xs text-muted mb-0.5">{t('startupReport.migrations')}</p>
            <p className="text-sm font-medium">
              {report.migrationsApplied}/{report.migrationsTotal}
            </p>
          </div>
          <div>
            <p className="text-xs text-muted mb-0.5">{t('startupReport.modules')}</p>
            <p className="text-sm font-medium">{report.modulesRegistered}</p>
          </div>
        </div>

        {/* Config flags */}
        <div>
          <p className="text-xs text-muted mb-2">{t('startupReport.configTitle')}</p>
          <div className="flex flex-wrap gap-2">
            {[
              { key: 'smtp', value: report.configuration.smtpConfigured },
              { key: 'ollama', value: report.configuration.ollamaConfigured },
              { key: 'elasticsearch', value: report.configuration.elasticsearchConfigured },
            ].map(({ key, value }) => (
              <span
                key={key}
                className={`inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full border ${
                  value
                    ? 'bg-success/10 border-success/30 text-success'
                    : 'bg-muted/20 border-border text-muted'
                }`}
              >
                <CheckCircle2 size={10} />
                {t(`startupReport.config.${key}`)}
              </span>
            ))}
          </div>
        </div>

        {/* Warnings */}
        {report.warnings.length > 0 && (
          <div>
            <p className="text-xs text-muted mb-2">{t('startupReport.warnings')}</p>
            <ul className="space-y-1">
              {report.warnings.map((w, i) => (
                <li key={i} className="flex items-start gap-2 text-sm text-warning">
                  <AlertTriangle size={14} className="shrink-0 mt-0.5" />
                  {w}
                </li>
              ))}
            </ul>
          </div>
        )}

        {/* CORS origins */}
        {report.configuration.corsOrigins.length > 0 && (
          <div>
            <p className="text-xs text-muted mb-2">{t('startupReport.corsOrigins')}</p>
            <div className="flex flex-wrap gap-1">
              {report.configuration.corsOrigins.map((o) => (
                <code key={o} className="text-xs bg-surface px-1.5 py-0.5 rounded border border-border">
                  {o}
                </code>
              ))}
            </div>
          </div>
        )}
      </CardBody>
    </Card>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function StartupReportPage() {
  const { t } = useTranslation();

  const query = useQuery({
    queryKey: ['startup-reports'],
    queryFn: () => platformAdminApi.getStartupReports(),
    staleTime: 60_000,
  });

  return (
    <PageContainer>
      <PageHeader
        icon={<Activity size={22} />}
        title={t('startupReport.title')}
        subtitle={t('startupReport.subtitle')}
        actions={
          <Button variant="secondary" onClick={() => query.refetch()} size="sm">
            <RefreshCw size={14} className="mr-1.5" />
            {t('startupReport.refresh')}
          </Button>
        }
      />

      <PageSection>
        {query.isLoading && <PageLoadingState />}
        {query.isError && <PageErrorState message={t('startupReport.loadError')} />}
        {query.isSuccess && (
          <>
            {/* Summary banner */}
            {query.data.reports.length > 0 && (
              <div className="flex items-center gap-2 p-3 rounded-lg bg-surface border border-border mb-4 text-sm">
                <Package size={16} className="text-muted" />
                <span className="text-muted">{t('startupReport.showing', { count: query.data.reports.length })}</span>
              </div>
            )}

            {query.data.reports.length === 0 ? (
              <p className="text-sm text-muted text-center py-12">{t('startupReport.empty')}</p>
            ) : (
              <div className="space-y-4">
                {query.data.reports.map((r, i) => (
                  <ReportCard key={r.id} report={r} index={i} />
                ))}
              </div>
            )}
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
