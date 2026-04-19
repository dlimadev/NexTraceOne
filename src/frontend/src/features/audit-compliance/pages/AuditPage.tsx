import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Shield, RefreshCw, Search, CheckCircle, XCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { auditApi } from '../api';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

export function AuditPage() {
  const { t } = useTranslation();
  const [eventTypeFilter, setEventTypeFilter] = useState('');
  const [sourceModuleFilter, setSourceModuleFilter] = useState('');
  const [correlationFilter, setCorrelationFilter] = useState('');
  const [resourceTypeFilter, setResourceTypeFilter] = useState('');
  const [resourceIdFilter, setResourceIdFilter] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [isExporting, setIsExporting] = useState(false);
  const [page, setPage] = useState(1);

  const fromIso = fromDate ? new Date(fromDate).toISOString() : undefined;
  const toIso = toDate ? new Date(toDate).toISOString() : undefined;

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['audit', 'events', page, eventTypeFilter, sourceModuleFilter, correlationFilter, resourceTypeFilter, resourceIdFilter, fromDate, toDate],
    queryFn: () =>
      auditApi.listEvents({
        page,
        pageSize: 20,
        eventType: eventTypeFilter || undefined,
        sourceModule: sourceModuleFilter || undefined,
        correlationId: correlationFilter || undefined,
        resourceType: resourceTypeFilter || undefined,
        resourceId: resourceIdFilter || undefined,
        from: fromIso,
        to: toIso,
      }),
    staleTime: 10_000,
  });

  const { data: integrity, refetch: verifyIntegrity, isFetching: verifying } = useQuery({
    queryKey: ['audit', 'integrity'],
    queryFn: () => auditApi.verifyIntegrity(),
    enabled: false,
  });

  // Build integrity message from structured data using i18n keys
  const integrityMessage = integrity
    ? integrity.valid
      ? t('audit.integrityValid', {
          count: integrity.totalLinks,
          defaultValue: `Hash chain is valid. All ${integrity.totalLinks} events verified.`,
        }) + (integrity.isTruncated
          ? ` ${t('audit.integrityTruncated', { seq: integrity.truncatedAtSequence ?? t('common.unknown'), defaultValue: `(truncated at sequence ${integrity.truncatedAtSequence ?? 'unknown'})` })}`
          : '')
      : t('audit.integrityViolation', {
          count: integrity.violations.length,
          defaultValue: `Integrity violation detected. ${integrity.violations.length} issue(s) found.`,
        })
    : null;

  const handleExport = async () => {
    const now = new Date();
    const resolvedFrom = fromDate ? new Date(fromDate) : new Date(now);
    if (!fromDate) {
      resolvedFrom.setDate(now.getDate() - 7);
    }
    const resolvedTo = toDate ? new Date(toDate) : now;

    setIsExporting(true);
    try {
      const blob = await auditApi.exportReport(resolvedFrom.toISOString(), resolvedTo.toISOString());
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `audit-report-${resolvedFrom.toISOString().slice(0, 10)}-${resolvedTo.toISOString().slice(0, 10)}.json`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('audit.title')}
        subtitle={t('audit.subtitle')}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button
              variant="secondary"
              onClick={() => verifyIntegrity()}
              loading={verifying}
            >
              <Shield size={16} />
              {t('audit.verifyIntegrity')}
            </Button>
            <Button
              variant="secondary"
              onClick={handleExport}
              loading={isExporting}
            >
              {t('audit.exportReport')}
            </Button>
          </div>
        }
      />

      {/* Integrity result */}
      {integrity && (
        <div
          className={`mb-4 rounded-lg border px-4 py-3 flex items-center gap-3 ${
            integrity.valid
              ? 'border-success/30 bg-success/10'
              : 'border-critical/30 bg-critical/10'
          }`}
        >
          {integrity.valid ? (
            <CheckCircle size={16} className="text-success shrink-0" />
          ) : (
            <XCircle size={16} className="text-critical shrink-0" />
          )}
          <p className={`text-sm ${integrity.valid ? 'text-success' : 'text-critical'}`}>
            {integrityMessage}
          </p>
        </div>
      )}

      {/* Filter + Table */}
      <PageSection>
        <Card className="mb-6">
          <CardBody>
            <div className="flex flex-wrap gap-3 items-center">
              <Search size={16} className="text-muted shrink-0" />
              <input
                type="text"
                value={eventTypeFilter}
                onChange={(e) => {
                  setEventTypeFilter(e.target.value);
                  setPage(1);
                }}
                placeholder={t('audit.filterPlaceholder')}
                aria-label={t('audit.filterPlaceholder')}
                className="min-w-[180px] flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
              />
              <input
                type="text"
                value={sourceModuleFilter}
                onChange={(e) => {
                  setSourceModuleFilter(e.target.value);
                  setPage(1);
                }}
                placeholder={t('audit.sourceModulePlaceholder')}
                aria-label={t('audit.sourceModulePlaceholder')}
                className="min-w-[180px] flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
              />
              <input
                type="text"
                value={correlationFilter}
                onChange={(e) => {
                  setCorrelationFilter(e.target.value);
                  setPage(1);
                }}
                placeholder={t('audit.correlationIdPlaceholder')}
                aria-label={t('audit.correlationIdPlaceholder')}
                className="min-w-[180px] flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
              />
              <input
                type="text"
                value={resourceTypeFilter}
                onChange={(e) => {
                  setResourceTypeFilter(e.target.value);
                  setPage(1);
                }}
                placeholder={t('audit.resourceTypePlaceholder')}
                aria-label={t('audit.resourceTypePlaceholder')}
                className="min-w-[150px] flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
              />
              <input
                type="text"
                value={resourceIdFilter}
                onChange={(e) => {
                  setResourceIdFilter(e.target.value);
                  setPage(1);
                }}
                placeholder={t('audit.resourceIdPlaceholder')}
                aria-label={t('audit.resourceIdPlaceholder')}
                className="min-w-[150px] flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
              />
              <div className="flex items-center gap-2">
                <span className="text-xs text-muted">{t('audit.fromDate')}</span>
                <input
                  type="date"
                  value={fromDate}
                  onChange={(e) => {
                    setFromDate(e.target.value);
                    setPage(1);
                  }}
                  aria-label={t('audit.fromDate')}
                  className="text-sm bg-transparent text-heading focus:outline-none"
                />
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs text-muted">{t('audit.toDate')}</span>
                <input
                  type="date"
                  value={toDate}
                  onChange={(e) => {
                    setToDate(e.target.value);
                    setPage(1);
                  }}
                  aria-label={t('audit.toDate')}
                  className="text-sm bg-transparent text-heading focus:outline-none"
                />
              </div>
              <Button variant="secondary" onClick={() => refetch()} loading={isFetching}>
                <RefreshCw size={14} />
                {t('common.refresh')}
              </Button>
            </div>
          </CardBody>
        </Card>

        <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Shield size={16} className="text-muted" />
              <h2 className="font-semibold text-heading">{t('audit.auditEvents')}</h2>
            </div>
            {data && (
              <span className="text-sm text-muted">{data.totalCount} total</span>
            )}
          </div>
        </CardHeader>
        <div className="overflow-x-auto">
          {isLoading ? (
            <PageLoadingState />
          ) : isError ? (
            <PageErrorState message={t('audit.loadFailed')} />
          ) : !data?.items?.length ? (
            <p className="px-6 py-12 text-sm text-muted text-center">
              {t('audit.noEvents')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-6 py-3 font-medium text-muted">{t('audit.eventType')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('audit.actor')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('audit.aggregate')}</th>
                  <th className="px-6 py-3 font-medium text-muted">{t('audit.timestamp')}</th>
                  <th className="px-6 py-3 font-medium text-muted">
                    {t('audit.sourceModule', { defaultValue: 'Source module' })}
                  </th>
                  <th className="px-6 py-3 font-medium text-muted">
                    {t('audit.correlationId')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {data.items.map((e) => {
                  return (
                    <tr key={e.id} className="hover:bg-hover transition-colors">
                      <td className="px-6 py-3 font-medium text-heading">{e.eventType}</td>
                      <td className="px-6 py-3 text-body">{e.actorEmail}</td>
                      <td className="px-6 py-3 text-body">{e.aggregateType}</td>
                      <td className="px-6 py-3 text-xs text-muted">
                        {new Date(e.occurredAt).toLocaleString()}
                      </td>
                      <td className="px-6 py-3 text-xs text-faded">
                        {e.sourceModule ?? '—'}
                      </td>
                      <td className="px-6 py-3 text-xs text-faded font-mono">
                        {e.correlationId ?? '—'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="px-6 py-4 flex items-center justify-between border-t border-edge">
            <Button
              variant="secondary"
              disabled={page === 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              {t('audit.previous')}
            </Button>
            <span className="text-sm text-muted">
              {t('audit.pageOf', { page: data.page, totalPages: data.totalPages })}
            </span>
            <Button
              variant="secondary"
              disabled={page >= data.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              {t('common.next')}
            </Button>
          </div>
        )}
        </Card>
      </PageSection>
    </PageContainer>
  );
}
