import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  FileText,
  Lock,
  Clock,
  Tag,
  User,
  GitBranch,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import { Button } from '../../../components/Button';
import { useKnowledgeDocument } from '../hooks';
import type { DocumentStatus } from '../../../types';

const STATUS_VARIANT: Record<DocumentStatus, 'success' | 'info' | 'warning' | 'default'> = {
  Published: 'success',
  Draft: 'info',
  Archived: 'warning',
  Deprecated: 'default',
};

export function KnowledgeDocumentPage() {
  const { t } = useTranslation();
  const { documentId } = useParams<{ documentId: string }>();
  const navigate = useNavigate();

  const { data: document, isLoading, isError } = useKnowledgeDocument(documentId);

  if (isLoading) return <PageLoadingState />;
  if (isError || !document) return <PageErrorState />;

  return (
    <PageContainer>
      <div className="flex items-center gap-3 mb-6">
        <button
          onClick={() => navigate('/knowledge')}
          className="flex items-center gap-1.5 text-sm text-content-secondary hover:text-content-primary transition-colors"
        >
          <ArrowLeft size={16} />
          {t('knowledgeHub.backToHub')}
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Main content */}
        <div className="lg:col-span-3 space-y-4">
          <Card>
            <CardHeader>
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start gap-3 min-w-0">
                  <div className="p-2 rounded-lg bg-info/15 text-info shrink-0">
                    <FileText size={20} />
                  </div>
                  <div className="min-w-0">
                    <h1 className="text-xl font-semibold text-content-primary">{document.title}</h1>
                    {document.summary && (
                      <p className="text-sm text-content-secondary mt-1">{document.summary}</p>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <Badge variant={STATUS_VARIANT[document.status]} size="sm">
                    {t(`knowledgeHub.status.${document.status}`)}
                  </Badge>
                  <Badge variant="default" size="sm">
                    {t(`knowledgeHub.category.${document.category}`)}
                  </Badge>
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <div className="prose prose-invert prose-sm max-w-none">
                <pre className="whitespace-pre-wrap font-sans text-sm text-content-primary leading-relaxed">
                  {document.content}
                </pre>
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Sidebar metadata */}
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <h3 className="text-sm font-medium text-content-primary">{t('knowledgeHub.documentMeta')}</h3>
            </CardHeader>
            <CardBody>
              <dl className="space-y-3 text-sm">
                <div className="flex items-center gap-2">
                  <GitBranch size={14} className="text-content-tertiary shrink-0" />
                  <dt className="text-content-secondary shrink-0">{t('knowledgeHub.metaVersion')}</dt>
                  <dd className="text-content-primary font-mono ml-auto">v{document.version}</dd>
                </div>
                <div className="flex items-start gap-2">
                  <User size={14} className="text-content-tertiary shrink-0 mt-0.5" />
                  <dt className="text-content-secondary shrink-0">{t('knowledgeHub.metaAuthor')}</dt>
                  <dd className="text-content-primary ml-auto text-right text-xs font-mono truncate max-w-[120px]">
                    {document.authorId}
                  </dd>
                </div>
                <div className="flex items-center gap-2">
                  <Clock size={14} className="text-content-tertiary shrink-0" />
                  <dt className="text-content-secondary shrink-0">{t('knowledgeHub.metaCreated')}</dt>
                  <dd className="text-content-primary ml-auto">
                    {new Date(document.createdAt).toLocaleDateString()}
                  </dd>
                </div>
                {document.updatedAt && (
                  <div className="flex items-center gap-2">
                    <Clock size={14} className="text-content-tertiary shrink-0" />
                    <dt className="text-content-secondary shrink-0">{t('knowledgeHub.metaUpdated')}</dt>
                    <dd className="text-content-primary ml-auto">
                      {new Date(document.updatedAt).toLocaleDateString()}
                    </dd>
                  </div>
                )}
                {document.publishedAt && (
                  <div className="flex items-center gap-2">
                    <Lock size={14} className="text-content-tertiary shrink-0" />
                    <dt className="text-content-secondary shrink-0">{t('knowledgeHub.metaPublished')}</dt>
                    <dd className="text-content-primary ml-auto">
                      {new Date(document.publishedAt).toLocaleDateString()}
                    </dd>
                  </div>
                )}
              </dl>
            </CardBody>
          </Card>

          {document.tags.length > 0 && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Tag size={14} className="text-content-secondary" />
                  <h3 className="text-sm font-medium text-content-primary">{t('knowledgeHub.tags')}</h3>
                </div>
              </CardHeader>
              <CardBody>
                <div className="flex flex-wrap gap-1.5">
                  {document.tags.map(tag => (
                    <span
                      key={tag}
                      className="text-xs text-content-secondary bg-surface-raised px-2 py-1 rounded-full border border-edge"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              </CardBody>
            </Card>
          )}

          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate('/knowledge/notes')}
          >
            {t('knowledgeHub.viewRelatedNotes')}
          </Button>
        </div>
      </div>
    </PageContainer>
  );
}
