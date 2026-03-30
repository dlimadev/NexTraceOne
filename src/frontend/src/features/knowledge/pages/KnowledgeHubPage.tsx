import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  Search,
  Plus,
  FileText,
  Wrench,
  AlertTriangle,
  Building,
  CheckSquare,
  FileBadge,
  BookMarked,
  StickyNote,
  ChevronRight,
  Tag,
  Clock,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import {
  useKnowledgeDocuments,
  useOperationalNotes,
  useKnowledgeSearch,
} from '../hooks';
import type { DocumentCategory, DocumentStatus } from '../../../types';

const CATEGORY_ICONS: Record<DocumentCategory, React.ReactNode> = {
  General: <BookOpen size={16} />,
  Runbook: <Wrench size={16} />,
  Troubleshooting: <AlertTriangle size={16} />,
  Architecture: <Building size={16} />,
  Procedure: <CheckSquare size={16} />,
  PostMortem: <FileBadge size={16} />,
  Reference: <BookMarked size={16} />,
};

const STATUS_VARIANT: Record<DocumentStatus, 'success' | 'info' | 'warning' | 'default'> = {
  Published: 'success',
  Draft: 'info',
  Archived: 'warning',
  Deprecated: 'default',
};

const SEARCH_DEBOUNCE_MS = 350;

export function KnowledgeHubPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<DocumentCategory | ''>('');

  const debounceRef = React.useRef<ReturnType<typeof setTimeout>>();
  const handleSearchChange = (value: string) => {
    setSearchTerm(value);
    clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => setDebouncedSearch(value), SEARCH_DEBOUNCE_MS);
  };

  const { data: documents, isLoading, isError } = useKnowledgeDocuments({
    category: categoryFilter || undefined,
    status: 'Published',
    page: 1,
    pageSize: 25,
  });

  const { data: recentNotes } = useOperationalNotes({
    isResolved: false,
    page: 1,
    pageSize: 5,
  });

  const { data: searchResults, isLoading: isSearching } = useKnowledgeSearch(debouncedSearch);

  const categories: DocumentCategory[] = [
    'Runbook', 'Troubleshooting', 'Architecture', 'Procedure', 'PostMortem', 'Reference', 'General',
  ];

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState />;

  const displayItems = debouncedSearch.trim().length >= 2 && searchResults
    ? searchResults.items.filter(i => i.entityType === 'KnowledgeDocument')
    : (documents?.items ?? []);

  const totalDocs = documents?.totalCount ?? 0;
  const openNotes = recentNotes?.totalCount ?? 0;

  return (
    <PageContainer>
      <PageHeader
        title={t('knowledgeHub.title')}
        subtitle={t('knowledgeHub.subtitle')}
        actions={
          <Button
            variant="primary"
            size="sm"
            onClick={() => navigate('/knowledge/documents/new')}
          >
            <Plus size={16} />
            {t('knowledgeHub.newDocument')}
          </Button>
        }
      />

      <StatsGrid>
        <Card>
          <CardBody>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-blue-500/10 text-blue-400">
                <FileText size={20} />
              </div>
              <div>
                <p className="text-2xl font-semibold text-content-primary">{totalDocs}</p>
                <p className="text-sm text-content-secondary">{t('knowledgeHub.statsDocuments')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
        <Card>
          <CardBody>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-amber-500/10 text-amber-400">
                <StickyNote size={20} />
              </div>
              <div>
                <p className="text-2xl font-semibold text-content-primary">{openNotes}</p>
                <p className="text-sm text-content-secondary">{t('knowledgeHub.statsOpenNotes')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
      </StatsGrid>

      {/* Search bar */}
      <div className="relative">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-content-tertiary" />
        <input
          type="text"
          value={searchTerm}
          onChange={e => handleSearchChange(e.target.value)}
          placeholder={t('knowledgeHub.searchPlaceholder')}
          className="w-full pl-9 pr-4 py-2 rounded-lg bg-surface-raised border border-edge text-sm text-content-primary placeholder:text-content-tertiary focus:outline-none focus:ring-2 focus:ring-accent/50"
        />
      </div>

      {/* Category filters */}
      <div className="flex items-center gap-2 flex-wrap">
        <button
          onClick={() => setCategoryFilter('')}
          className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
            categoryFilter === ''
              ? 'bg-accent text-white'
              : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
          }`}
        >
          {t('knowledgeHub.filterAll')}
        </button>
        {categories.map(cat => (
          <button
            key={cat}
            onClick={() => setCategoryFilter(cat === categoryFilter ? '' : cat)}
            className={`flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              categoryFilter === cat
                ? 'bg-accent text-white'
                : 'bg-surface-raised text-content-secondary hover:bg-surface-hover'
            }`}
          >
            {CATEGORY_ICONS[cat]}
            {t(`knowledgeHub.category.${cat}`)}
          </button>
        ))}
      </div>

      <PageSection>
        {/* Document list */}
        {isSearching && debouncedSearch.trim().length >= 2 ? (
          <PageLoadingState />
        ) : displayItems.length === 0 ? (
          <Card>
            <CardBody>
              <div className="text-center py-10 text-content-secondary">
                <BookOpen size={32} className="mx-auto mb-3 opacity-40" />
                <p className="text-sm">{t('knowledgeHub.noDocuments')}</p>
              </div>
            </CardBody>
          </Card>
        ) : (
          <div className="space-y-2">
            {displayItems.map((item) => {
              const isSearchItem = 'entityId' in item;
              const id = isSearchItem ? item.entityId : (item as { documentId: string }).documentId;
              const title = item.title;
              const category = isSearchItem ? null : (item as { category: DocumentCategory }).category;
              const status = isSearchItem ? null : (item as { status: DocumentStatus }).status;
              const tags = isSearchItem ? [] : (item as { tags: string[] }).tags ?? [];
              const updatedAt = isSearchItem ? null : (item as { updatedAt: string | null }).updatedAt;

              return (
                <Card
                  key={id}
                  className="cursor-pointer hover:border-accent/50 transition-colors"
                  onClick={() => navigate(`/knowledge/documents/${id}`)}
                >
                  <CardBody>
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex items-start gap-3 min-w-0">
                        <div className="p-1.5 rounded bg-surface-raised text-content-secondary mt-0.5 shrink-0">
                          {category ? CATEGORY_ICONS[category] : <FileText size={16} />}
                        </div>
                        <div className="min-w-0">
                          <p className="font-medium text-content-primary text-sm truncate">{title}</p>
                          {!isSearchItem && (item as { summary: string | null }).summary && (
                            <p className="text-xs text-content-secondary mt-0.5 line-clamp-2">
                              {(item as { summary: string | null }).summary}
                            </p>
                          )}
                          {tags.length > 0 && (
                            <div className="flex items-center gap-1 mt-1.5 flex-wrap">
                              <Tag size={12} className="text-content-tertiary" />
                              {tags.slice(0, 4).map(tag => (
                                <span key={tag} className="text-xs text-content-tertiary bg-surface-raised px-1.5 py-0.5 rounded">
                                  {tag}
                                </span>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-2 shrink-0">
                        {category && (
                          <Badge variant="default" size="sm">{t(`knowledgeHub.category.${category}`)}</Badge>
                        )}
                        {status && (
                          <Badge variant={STATUS_VARIANT[status]} size="sm">{t(`knowledgeHub.status.${status}`)}</Badge>
                        )}
                        {updatedAt && (
                          <span className="flex items-center gap-1 text-xs text-content-tertiary">
                            <Clock size={11} />
                            {new Date(updatedAt).toLocaleDateString()}
                          </span>
                        )}
                        <ChevronRight size={16} className="text-content-tertiary" />
                      </div>
                    </div>
                  </CardBody>
                </Card>
              );
            })}
          </div>
        )}
      </PageSection>

      {/* Recent open operational notes panel */}
      {recentNotes && recentNotes.items.length > 0 && (
        <PageSection title={t('knowledgeHub.recentNotesTitle')}>
          <div className="space-y-2">
            {recentNotes.items.map(note => (
              <Card
                key={note.noteId}
                className="cursor-pointer hover:border-accent/50 transition-colors"
                onClick={() => navigate('/knowledge/notes')}
              >
                <CardBody>
                  <div className="flex items-center justify-between gap-3">
                    <div className="flex items-center gap-2 min-w-0">
                      <StickyNote size={15} className="text-content-secondary shrink-0" />
                      <span className="text-sm font-medium text-content-primary truncate">{note.title}</span>
                    </div>
                    <Badge
                      variant={note.severity === 'Critical' ? 'danger' : note.severity === 'Warning' ? 'warning' : 'info'}
                      size="sm"
                    >
                      {t(`knowledgeHub.severity.${note.severity}`)}
                    </Badge>
                  </div>
                </CardBody>
              </Card>
            ))}
            <button
              onClick={() => navigate('/knowledge/notes')}
              className="text-xs text-accent hover:underline"
            >
              {t('knowledgeHub.viewAllNotes')}
            </button>
          </div>
        </PageSection>
      )}
    </PageContainer>
  );
}
