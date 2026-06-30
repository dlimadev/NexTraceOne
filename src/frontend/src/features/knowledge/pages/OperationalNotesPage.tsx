import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  StickyNote,
  AlertTriangle,
  Info,
  AlertOctagon,
  CheckCircle2,
  Clock,
  Filter,
  Tag,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Tabs } from '../../../components/Tabs';
import { IconButton } from '../../../components/IconButton';
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
          <Filter size={14} className="text-muted" />
          <span className="text-xs text-body font-medium">{t('operationalNotes.filterSeverity')}</span>
        </div>
        <Tabs
          variant="pill"
          size="sm"
          items={[
            { id: '', label: t('operationalNotes.filterAll') },
            ...severities.map(s => ({ id: s, label: t(`knowledgeHub.severity.${s}`), icon: SEVERITY_ICON[s] })),
          ]}
          activeId={severityFilter}
          onChange={(id) => { setSeverityFilter(id as NoteSeverity | ''); setPage(1); }}
        />

        <div className="ml-auto">
          <Tabs
            variant="pill"
            size="sm"
            items={[
              { id: 'all', label: t('operationalNotes.filterStatusAll') },
              { id: 'open', label: t('operationalNotes.filterOpen') },
              { id: 'resolved', label: t('operationalNotes.filterResolved'), icon: <CheckCircle2 size={12} /> },
            ]}
            activeId={resolvedFilter === undefined ? 'all' : resolvedFilter ? 'resolved' : 'open'}
            onChange={(id) => { setResolvedFilter(id === 'all' ? undefined : id === 'resolved'); setPage(1); }}
          />
        </div>
      </div>

      <PageSection>
        {data && data.items.length === 0 ? (
          <Card>
            <CardBody>
              <div className="text-center py-10 text-body">
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
                          <p className="font-medium text-sm text-heading truncate">{note.title}</p>
                          {note.isResolved && (
                            <CheckCircle2 size={13} className="text-success shrink-0" />
                          )}
                        </div>
                        <p className="text-xs text-body mt-0.5 line-clamp-2">{note.content}</p>
                        {note.tags.length > 0 && (
                          <div className="flex items-center gap-1 mt-1.5 flex-wrap">
                            <Tag size={11} className="text-muted" />
                            {note.tags.slice(0, 4).map(tag => (
                              <span key={tag} className="text-xs text-muted bg-elevated px-1.5 py-0.5 rounded">
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
                        <span className="flex items-center gap-1 text-xs text-muted">
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
            <span className="text-xs text-body">
              {data?.totalCount ?? 0} {t('common.total')}
            </span>
            <div className="flex items-center gap-2">
              <IconButton
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
                label={t('common.previous', 'Previous')}
                icon={<ChevronLeft size={16} />}
              />
              <span className="text-xs text-body">{page} / {totalPages}</span>
              <IconButton
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                label={t('common.next', 'Next')}
                icon={<ChevronRight size={16} />}
              />
            </div>
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
