import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { FileText, RefreshCw, Sparkles, Clock } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { changeIntelligenceApi } from '../api/changeIntelligence';

interface ReleaseNotesPanelProps {
  releaseId: string | null;
}

interface NotesSectionProps {
  title: string;
  content: string | null;
}

function NotesSection({ title, content }: NotesSectionProps) {
  if (!content) return null;
  return (
    <div>
      <h4 className="text-xs font-semibold text-muted uppercase tracking-wide mb-2">{title}</h4>
      <p className="text-sm text-body leading-relaxed whitespace-pre-line">{content}</p>
    </div>
  );
}

function statusVariant(status: string): 'default' | 'success' | 'warning' | 'info' {
  switch (status) {
    case 'Generated':
      return 'success';
    case 'Pending':
      return 'warning';
    case 'Regenerating':
      return 'info';
    default:
      return 'default';
  }
}

/**
 * Painel de release notes geradas por IA.
 * Exibe, gera e regenera notas de release contextualizadas por serviço e contrato.
 */
export function ReleaseNotesPanel({ releaseId }: ReleaseNotesPanelProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const notesQuery = useQuery({
    queryKey: ['release-notes', releaseId],
    queryFn: () => changeIntelligenceApi.getReleaseNotes(releaseId!),
    enabled: !!releaseId,
    retry: false,
  });

  const generateMutation = useMutation({
    mutationFn: () => changeIntelligenceApi.generateReleaseNotes(releaseId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-notes', releaseId] });
    },
  });

  const regenerateMutation = useMutation({
    mutationFn: () => changeIntelligenceApi.regenerateReleaseNotes(releaseId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-notes', releaseId] });
    },
  });

  if (!releaseId) {
    return (
      <Card>
        <CardBody>
          <p className="text-sm text-muted py-12 text-center">
            {t('releaseNotes.selectRelease')}
          </p>
        </CardBody>
      </Card>
    );
  }

  const notes = notesQuery.data;
  const notFound = notesQuery.isError;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <FileText size={16} className="text-accent" />
              <h2 className="text-sm font-semibold text-heading">{t('releaseNotes.title')}</h2>
              {notes && (
                <Badge variant={statusVariant(notes.status)}>{notes.status}</Badge>
              )}
            </div>
            <div className="flex items-center gap-2">
              {notes ? (
                <Button
                  variant="secondary"
                  onClick={() => regenerateMutation.mutate()}
                  loading={regenerateMutation.isPending}
                >
                  <RefreshCw size={14} />
                  {t('releaseNotes.regenerate')}
                </Button>
              ) : (
                <Button
                  onClick={() => generateMutation.mutate()}
                  loading={generateMutation.isPending}
                  disabled={notesQuery.isLoading}
                >
                  <Sparkles size={14} />
                  {t('releaseNotes.generate')}
                </Button>
              )}
            </div>
          </div>
        </CardHeader>

        {notesQuery.isLoading && <PageLoadingState />}

        {notFound && !notes && (
          <CardBody>
            <div className="py-8 text-center">
              <FileText size={32} className="mx-auto mb-3 text-muted opacity-50" />
              <p className="text-sm font-medium text-heading mb-1">
                {t('releaseNotes.notGenerated')}
              </p>
              <p className="text-xs text-muted mb-4">
                {t('releaseNotes.notGeneratedDescription')}
              </p>
              <Button
                onClick={() => generateMutation.mutate()}
                loading={generateMutation.isPending}
              >
                <Sparkles size={14} />
                {t('releaseNotes.generateWithAi')}
              </Button>
            </div>
          </CardBody>
        )}

        {notes && (
          <CardBody className="space-y-6">
            {/* Metadata */}
            <div className="flex flex-wrap gap-4 text-xs text-muted border-b border-edge pb-3">
              <span className="flex items-center gap-1">
                <Clock size={12} />
                {new Date(notes.generatedAt).toLocaleString()}
              </span>
              {notes.lastRegeneratedAt && (
                <span>
                  {t('releaseNotes.regeneratedAt')}:{' '}
                  {new Date(notes.lastRegeneratedAt).toLocaleString()}
                </span>
              )}
              <span>{t('releaseNotes.model')}: {notes.modelUsed}</span>
              <span>{t('releaseNotes.tokens')}: {notes.tokensUsed.toLocaleString()}</span>
              {notes.regenerationCount > 0 && (
                <span>
                  {t('releaseNotes.regenerations')}: {notes.regenerationCount}
                </span>
              )}
            </div>

            {/* Content sections */}
            <NotesSection title={t('releaseNotes.sections.technical')} content={notes.technicalSummary} />
            <NotesSection title={t('releaseNotes.sections.executive')} content={notes.executiveSummary} />
            <NotesSection title={t('releaseNotes.sections.newEndpoints')} content={notes.newEndpointsSection} />
            <NotesSection title={t('releaseNotes.sections.breakingChanges')} content={notes.breakingChangesSection} />
            <NotesSection title={t('releaseNotes.sections.affectedServices')} content={notes.affectedServicesSection} />
            <NotesSection title={t('releaseNotes.sections.confidenceMetrics')} content={notes.confidenceMetricsSection} />
            <NotesSection title={t('releaseNotes.sections.evidenceLinks')} content={notes.evidenceLinksSection} />
          </CardBody>
        )}
      </Card>
    </div>
  );
}
