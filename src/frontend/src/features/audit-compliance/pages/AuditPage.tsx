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
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['audit', 'events', page, eventTypeFilter],
    queryFn: () =>
      auditApi.listEvents({
        page,
        pageSize: 20,
        eventType: eventTypeFilter || undefined,
      }),
    staleTime: 10_000,
  });

  const { data: integrity, refetch: verifyIntegrity, isFetching: verifying } = useQuery({
    queryKey: ['audit', 'integrity'],
    queryFn: () => auditApi.verifyIntegrity(),
    enabled: false,
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('audit.title')}
        subtitle={t('audit.subtitle')}
        actions={
          <Button
            variant="secondary"
            onClick={() => verifyIntegrity()}
            loading={verifying}
          >
            <Shield size={16} />
            {t('audit.verifyIntegrity')}
          </Button>
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
            {integrity.message}
          </p>
        </div>
      )}

      {/* Filter + Table */}
      <PageSection>
        <Card className="mb-6">
          <CardBody>
            <div className="flex gap-3 items-center">
              <Search size={16} className="text-muted shrink-0" />
              <input
                type="text"
                value={eventTypeFilter}
                onChange={(e) => {
                  setEventTypeFilter(e.target.value);
                  setPage(1);
                }}
                placeholder={t('audit.filterPlaceholder')}
                className="flex-1 text-sm bg-transparent text-heading placeholder:text-muted focus:outline-none"
              />
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
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {data.items.map((e) => {
                  const sourceModule = (e as { payload?: { sourceModule?: string } }).payload?.sourceModule;

                  return (
                    <tr key={e.id} className="hover:bg-hover transition-colors">
                      <td className="px-6 py-3 font-medium text-heading">{e.eventType}</td>
                      <td className="px-6 py-3 text-body">{e.actorEmail}</td>
                      <td className="px-6 py-3 text-body">{e.aggregateType}</td>
                      <td className="px-6 py-3 text-xs text-muted">
                        {new Date(e.occurredAt).toLocaleString()}
                      </td>
                      <td className="px-6 py-3 text-xs text-faded">
                        {typeof sourceModule === 'string' ? sourceModule : '—'}
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
