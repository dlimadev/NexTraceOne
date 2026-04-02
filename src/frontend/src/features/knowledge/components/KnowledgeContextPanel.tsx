import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { BookOpen, ClipboardList } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { useKnowledgeRelationsByTarget } from '../hooks';
import type { KnowledgeRelationType } from '../../../types';

interface KnowledgeContextPanelProps {
  targetType: KnowledgeRelationType;
  targetEntityId: string | undefined;
}

/**
 * Painel contextual com documentos e notas operacionais ligados à entidade atual.
 */
export function KnowledgeContextPanel({
  targetType,
  targetEntityId,
}: KnowledgeContextPanelProps) {
  const { t } = useTranslation();

  const relationsQuery = useKnowledgeRelationsByTarget(targetType, targetEntityId);

  const relatedDocuments = relationsQuery.data?.documents ?? [];
  const relatedNotes = relationsQuery.data?.notes ?? [];

  return (
    <Card>
      <CardHeader>
        <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
          <BookOpen size={16} className="text-accent" aria-hidden="true" />
          {t('knowledge.relations.title', 'Knowledge context')}
        </h2>
      </CardHeader>
      <CardBody>
        {relationsQuery.isLoading ? (
          <p className="text-sm text-muted">{t('common.loading')}</p>
        ) : relatedDocuments.length === 0 && relatedNotes.length === 0 ? (
          <p className="text-sm text-muted">
            {t('knowledge.relations.empty', 'No linked documents or operational notes were found for this entity.')}
          </p>
        ) : (
          <div className="space-y-4">
            {relatedDocuments.length > 0 && (
              <div>
                <p className="text-xs text-muted mb-2">
                  {t('knowledge.relations.documents', 'Related knowledge documents')}
                </p>
                <div className="space-y-2">
                  {relatedDocuments.map((doc) => (
                    <Link
                      key={doc.relationId}
                      to={`/knowledge/documents/${doc.documentId}`}
                      className="flex items-center justify-between p-2 rounded bg-elevated hover:bg-hover transition-colors"
                    >
                      <div className="min-w-0">
                        <p className="text-sm text-body truncate">{doc.title}</p>
                        <p className="text-xs text-muted truncate">{doc.category}</p>
                      </div>
                      <Badge variant="default" className="text-[10px] ml-2 shrink-0">
                        {doc.status}
                      </Badge>
                    </Link>
                  ))}
                </div>
              </div>
            )}

            {relatedNotes.length > 0 && (
              <div>
                <p className="text-xs text-muted mb-2">
                  {t('knowledge.relations.notes', 'Operational notes')}
                </p>
                <div className="space-y-2">
                  {relatedNotes.map((note) => (
                    <Link
                      key={note.relationId}
                      to="/knowledge/notes"
                      className="flex items-center gap-2 p-2 rounded bg-elevated hover:bg-hover transition-colors"
                    >
                      <ClipboardList size={14} className="text-accent shrink-0" aria-hidden="true" />
                      <div className="min-w-0 flex-1">
                        <p className="text-sm text-body truncate">{note.title}</p>
                        <p className="text-xs text-muted truncate">
                          {note.noteType} · {note.origin}
                        </p>
                      </div>
                      <Badge
                        variant={note.isResolved ? 'success' : 'warning'}
                        className="text-[10px] shrink-0"
                      >
                        {note.isResolved
                          ? t('knowledge.notes.resolved', 'Resolved')
                          : t('knowledge.notes.open', 'Open')}
                      </Badge>
                    </Link>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </CardBody>
    </Card>
  );
}
