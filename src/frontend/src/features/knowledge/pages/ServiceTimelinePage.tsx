import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Clock,
  AlertTriangle,
  Info,
  AlertOctagon,
  CheckCircle,
  ChevronLeft,
  ChevronRight,
  Tag,
  Filter,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { useServiceOperationalTimeline } from '../hooks';
import type { NoteSeverity, ServiceTimelineEntryDto } from '../../../types';

const SEVERITY_ICON: Record<NoteSeverity, React.ReactNode> = {
  Info: <Info size={15} className="text-info" />,
  Warning: <AlertTriangle size={15} className="text-warning" />,
  Critical: <AlertOctagon size={15} className="text-critical" />,
};

const SEVERITY_VARIANT: Record<NoteSeverity, 'info' | 'warning' | 'danger'> = {
  Info: 'info',
  Warning: 'warning',
  Critical: 'danger',
};

const PAGE_SIZE = 25;

function TimelineEntryCard({ entry }: { entry: ServiceTimelineEntryDto }) {
  const { t } = useTranslation();

  return (
    <Card className={`border-l-4 ${entry.severity === 'Critical' ? 'border-critical' : entry.severity === 'Warning' ? 'border-warning' : 'border-info'}`}>
      <CardBody>
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-2 min-w-0">
            {SEVERITY_ICON[entry.severity as NoteSeverity]}
            <span className="font-medium text-sm truncate">{entry.title}</span>
          </div>
          <div className="flex items-center gap-2 shrink-0">
            {entry.isResolved && (
              <Badge variant="success">
                <CheckCircle size={12} className="mr-1" />
                {t('knowledge.serviceTimeline.resolved')}
              </Badge>
            )}
            <Badge variant={SEVERITY_VARIANT[entry.severity as NoteSeverity]}>
              {entry.severity}
            </Badge>
          </div>
        </div>

        <p className="text-xs text-secondary mt-2 line-clamp-3">{entry.content}</p>

        <div className="flex items-center justify-between mt-3 text-xs text-muted">
          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1">
              <Clock size={11} />
              {new Date(entry.occurredAt).toLocaleString()}
            </span>
            <span>{t('knowledge.serviceTimeline.origin')}: {entry.origin}</span>
            <span>{t('knowledge.serviceTimeline.type')}: {entry.noteType}</span>
          </div>

          {entry.tags.length > 0 && (
            <div className="flex items-center gap-1">
              <Tag size={11} />
              {entry.tags.slice(0, 3).map((tag) => (
                <span key={tag} className="bg-surface-raised px-1.5 py-0.5 rounded text-xs">{tag}</span>
              ))}
              {entry.tags.length > 3 && (
                <span className="text-muted">+{entry.tags.length - 3}</span>
              )}
            </div>
          )}
        </div>
      </CardBody>
    </Card>
  );
}

export function ServiceTimelinePage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string }>();
  const navigate = useNavigate();
  const [severityFilter, setSeverityFilter] = useState<NoteSeverity | ''>('');
  const [resolvedFilter, setResolvedFilter] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useServiceOperationalTimeline({
    serviceId,
    severity: severityFilter || undefined,
    isResolved: resolvedFilter,
    page,
    pageSize: PAGE_SIZE,
  });

  if (isLoading) return <PageLoadingState />;
  if (isError || !serviceId) return <PageErrorState />;

  const totalPages = data?.totalPages ?? 0;
  const items = data?.items ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('knowledge.serviceTimeline.title')}
        subtitle={t('knowledge.serviceTimeline.subtitle')}
        breadcrumb={[
          { label: t('knowledge.hub.title'), to: '/knowledge' },
          { label: t('knowledge.serviceTimeline.title') },
        ]}
        actions={
          <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
            <ChevronLeft size={14} className="mr-1" />
            {t('common.back')}
          </Button>
        }
      />

      <PageSection>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="flex items-center gap-2 text-sm">
            <Filter size={14} className="text-muted" />
            <span className="text-muted">{t('knowledge.serviceTimeline.filterBySeverity')}:</span>
            {(['', 'Info', 'Warning', 'Critical'] as const).map((s) => (
              <button
                key={s}
                onClick={() => {
                  setSeverityFilter(s);
                  setPage(1);
                }}
                className={`px-2 py-0.5 rounded text-xs border transition-colors ${
                  severityFilter === s
                    ? 'bg-accent border-accent text-white'
                    : 'border-border text-secondary hover:border-accent'
                }`}
              >
                {s === '' ? t('common.all') : s}
              </button>
            ))}
          </div>

          <div className="flex items-center gap-2 text-sm ml-auto">
            <span className="text-muted">{t('knowledge.serviceTimeline.filterByStatus')}:</span>
            <select
              value={resolvedFilter === undefined ? '' : resolvedFilter ? 'resolved' : 'open'}
              onChange={(e) => {
                setResolvedFilter(
                  e.target.value === '' ? undefined : e.target.value === 'resolved'
                );
                setPage(1);
              }}
              className="text-xs border border-border rounded px-2 py-1 bg-surface"
            >
              <option value="">{t('common.all')}</option>
              <option value="open">{t('knowledge.serviceTimeline.open')}</option>
              <option value="resolved">{t('knowledge.serviceTimeline.resolved')}</option>
            </select>
          </div>
        </div>

        {items.length === 0 ? (
          <div className="text-center py-12 text-muted text-sm">
            <Clock size={32} className="mx-auto mb-3 opacity-40" />
            <p>{t('knowledge.serviceTimeline.empty')}</p>
          </div>
        ) : (
          <div className="flex flex-col gap-3">
            {items.map((entry) => (
              <TimelineEntryCard key={entry.noteId} entry={entry} />
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex items-center justify-between mt-6 text-sm">
            <span className="text-muted">
              {t('common.pageOf', { page, totalPages })}
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
              >
                <ChevronLeft size={14} />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                <ChevronRight size={14} />
              </Button>
            </div>
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
