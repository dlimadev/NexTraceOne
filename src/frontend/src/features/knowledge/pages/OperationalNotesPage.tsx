import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  StickyNote,
  AlertTriangle,
  Info,
  AlertOctagon,
  CheckCircle,
  Clock,
  Filter,
  Tag,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { useOperationalNotes } from '../hooks';
import type { NoteSeverity } from '../../../types';

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

export function OperationalNotesPage() {
  const { t } = useTranslation();
  const [severityFilter, setSeverityFilter] = useState<NoteSeverity | ''>('');
  const [resolvedFilter, setResolvedFilter] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useOperationalNotes({
    severity: severityFilter || undefined,
    isResolved: resolvedFilter,
    page,
    pageSize: PAGE_SIZE,
  });

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 1;

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState />;

  const severities: NoteSeverity[] = ['Critical', 'Warning', 'Info'];

  return (
    <PageContainer>
      <PageHeader
        title={t('operationalNotes.title')}
        subtitle={t('operationalNotes.subtitle')}
      />

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <div className="flex items-center gap-1.5">
          <Filter size={14} className="text-content-tertiary" />
          <span className="text-xs text-content-secondary font-medium">{t('operationalNotes.filterSeverity')}</span>
        </div>
        <button
          onClick={() => { setSeverityFilter(''); setPage(1); }}
          className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
            severityFilter === ''
              ? 'bg-accent text-white'
              : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
          }`}
        >
          {t('operationalNotes.filterAll')}
        </button>
        {severities.map(s => (
          <button
            key={s}
            onClick={() => { setSeverityFilter(s === severityFilter ? '' : s); setPage(1); }}
            className={`flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              severityFilter === s
                ? 'bg-accent text-white'
                : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
            }`}
          >
            {SEVERITY_ICON[s]}
            {t(`knowledgeHub.severity.${s}`)}
          </button>
        ))}

        <div className="ml-auto flex items-center gap-2">
          <button
            onClick={() => { setResolvedFilter(undefined); setPage(1); }}
            className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              resolvedFilter === undefined
                ? 'bg-accent text-white'
                : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
            }`}
          >
            {t('operationalNotes.filterStatusAll')}
          </button>
          <button
            onClick={() => { setResolvedFilter(false); setPage(1); }}
            className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              resolvedFilter === false
                ? 'bg-warning text-white'
                : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
            }`}
          >
            {t('operationalNotes.filterOpen')}
          </button>
          <button
            onClick={() => { setResolvedFilter(true); setPage(1); }}
            className={`flex items-center gap-1 px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              resolvedFilter === true
                ? 'bg-success text-white'
                : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
            }`}
          >
            <CheckCircle size={12} />
            {t('operationalNotes.filterResolved')}
          </button>
        </div>
      </div>

      <PageSection>
        {data && data.items.length === 0 ? (
          <Card>
            <CardBody>
              <div className="text-center py-10 text-content-secondary">
                <StickyNote size={32} className="mx-auto mb-3 opacity-40" />
                <p className="text-sm">{t('operationalNotes.noNotes')}</p>
              </div>
            </CardBody>
          </Card>
        ) : (
          <div className="space-y-2">
            {data?.items.map(note => (
              <Card key={note.noteId}>
                <CardBody>
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex items-start gap-3 min-w-0">
                      <div className="mt-0.5 shrink-0">{SEVERITY_ICON[note.severity]}</div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="font-medium text-sm text-content-primary truncate">{note.title}</p>
                          {note.isResolved && (
                            <CheckCircle size={13} className="text-success shrink-0" />
                          )}
                        </div>
                        <p className="text-xs text-content-secondary mt-0.5 line-clamp-2">{note.content}</p>
                        {note.tags.length > 0 && (
                          <div className="flex items-center gap-1 mt-1.5 flex-wrap">
                            <Tag size={11} className="text-content-tertiary" />
                            {note.tags.slice(0, 4).map(tag => (
                              <span key={tag} className="text-xs text-content-tertiary bg-surface-raised px-1.5 py-0.5 rounded">
                                {tag}
                              </span>
                            ))}
                          </div>
                        )}
                      </div>
                    </div>
                    <div className="flex flex-col items-end gap-1.5 shrink-0">
                      <Badge variant={SEVERITY_VARIANT[note.severity]} size="sm">
                        {t(`knowledgeHub.severity.${note.severity}`)}
                      </Badge>
                      <Badge variant="default" size="sm">
                        {t(`operationalNotes.noteType.${note.noteType}`)}
                      </Badge>
                      {note.createdAt && (
                        <span className="flex items-center gap-1 text-xs text-content-tertiary">
                          <Clock size={11} />
                          {new Date(note.createdAt).toLocaleDateString()}
                        </span>
                      )}
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-1 py-2">
            <span className="text-xs text-content-secondary">
              {data?.totalCount ?? 0} {t('common.total')}
            </span>
            <div className="flex items-center gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
                className="p-1.5 rounded-md bg-surface-raised border border-edge text-content-secondary hover:text-content-primary disabled:opacity-40 transition-colors"
              >
                <ChevronLeft size={16} />
              </button>
              <span className="text-xs text-content-secondary">{page} / {totalPages}</span>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                className="p-1.5 rounded-md bg-surface-raised border border-edge text-content-secondary hover:text-content-primary disabled:opacity-40 transition-colors"
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
