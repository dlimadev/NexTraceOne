import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  FileText,
  Sparkles,
  RefreshCw,
  Clock,
  Cpu,
  Hash,
  CheckCircle2,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import { ReleaseSelector } from '../components/ReleaseSelector';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const PERSONA_MODES = ['Technical', 'Executive', 'ProductManager'] as const;

/**
 * ReleaseNotesPage — gestão de release notes geradas por IA.
 *
 * Permite engineers e product managers:
 * - Pesquisar e visualizar release notes de uma release específica
 * - Gerar release notes via IA com personalização de persona (Technical/Executive/PM)
 * - Regenerar release notes com dados actualizados
 * - Ver todas as secções: technical summary, executive summary, breaking changes, etc.
 */
export function ReleaseNotesPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const qc = useQueryClient();
  const [releaseId, setReleaseId] = useState('');
  const [personaMode, setPersonaMode] = useState<string>('Technical');
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['technical']));

  const {
    data: notes,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['release-notes', releaseId, activeEnvironmentId],
    queryFn: () => changeIntelligenceApi.getReleaseNotes(releaseId),
    enabled: !!releaseId,
    retry: false,
  });

  const generateMutation = useMutation({
    mutationFn: () => changeIntelligenceApi.generateReleaseNotes(releaseId, personaMode),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['release-notes', releaseId] }),
  });

  const regenerateMutation = useMutation({
    mutationFn: () => changeIntelligenceApi.regenerateReleaseNotes(releaseId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['release-notes', releaseId] }),
  });

  function toggleSection(key: string) {
    setExpandedSections((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  function statusBadge(status: string): 'success' | 'info' | 'warning' | 'default' {
    switch (status) {
      case 'Generated':
        return 'success';
      case 'Generating':
        return 'info';
      case 'Failed':
        return 'warning';
      default:
        return 'default';
    }
  }

  const inputCls =
    'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

  const sections = notes
    ? [
        { key: 'technical', label: t('releaseNotes.sections.technical', 'Technical Summary'), content: notes.technicalSummary },
        { key: 'executive', label: t('releaseNotes.sections.executive', 'Executive Summary'), content: notes.executiveSummary },
        { key: 'newEndpoints', label: t('releaseNotes.sections.newEndpoints', 'New Endpoints'), content: notes.newEndpointsSection },
        { key: 'breakingChanges', label: t('releaseNotes.sections.breakingChanges', 'Breaking Changes'), content: notes.breakingChangesSection },
        { key: 'affectedServices', label: t('releaseNotes.sections.affectedServices', 'Affected Services'), content: notes.affectedServicesSection },
        { key: 'confidenceMetrics', label: t('releaseNotes.sections.confidenceMetrics', 'Confidence Metrics'), content: notes.confidenceMetricsSection },
        { key: 'evidenceLinks', label: t('releaseNotes.sections.evidenceLinks', 'Evidence Links'), content: notes.evidenceLinksSection },
      ].filter((s) => s.content)
    : [];

  return (
    <PageContainer>
      <PageHeader
        icon={<FileText className="text-accent" />}
        title={t('releaseNotes.title', 'Release Notes')}
        subtitle={t(
          'releaseNotes.subtitle',
          'AI-assisted release notes with technical summary, breaking changes, affected services and evidence links',
        )}
      />

      {/* Release selection */}
      <div className="mb-6">
        <Card>
          <CardBody>
            <p className="text-sm text-muted mb-3">
              {t('releaseNotes.selectRelease', 'Select a release to view or generate its notes')}
            </p>
            <ReleaseSelector
              value={releaseId}
              onChange={(id) => setReleaseId(id)}
              placeholder={t('releaseNotes.selectorPlaceholder', 'Select a release…')}
            />
          </CardBody>
        </Card>
      </div>

      {!releaseId && (
        <EmptyState
          icon={<FileText size={40} />}
          title={t('releaseNotes.emptyTitle', 'No release selected')}
          description={t('releaseNotes.emptyDescription', 'Select a release above to view or generate release notes')}
        />
      )}

      {releaseId && isLoading && <PageLoadingState />}

      {/* No notes yet */}
      {releaseId && isError && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <div className="flex items-start gap-3">
                <Sparkles className="text-accent mt-0.5" size={20} />
                <div className="flex-1">
                  <p className="text-sm font-medium text-heading">
                    {t('releaseNotes.notGenerated', 'No release notes generated yet')}
                  </p>
                  <p className="text-xs text-muted mt-1">
                    {t('releaseNotes.notGeneratedDescription', 'Generate AI-assisted release notes that summarize changes, breaking changes and affected services.')}
                  </p>
                  <div className="mt-3">
                    <label className="block text-xs text-muted mb-1">
                      {t('releaseNotes.personaMode', 'Persona Mode')}
                    </label>
                    <div className="flex gap-2 flex-wrap">
                      {PERSONA_MODES.map((mode) => (
                        <button
                          key={mode}
                          className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                            personaMode === mode
                              ? 'bg-accent text-white border-accent'
                              : 'border-edge text-muted hover:text-heading hover:border-accent'
                          }`}
                          onClick={() => setPersonaMode(mode)}
                        >
                          {mode}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>
          <Button
            variant="primary"
            icon={<Sparkles size={16} />}
            loading={generateMutation.isPending}
            onClick={() => generateMutation.mutate()}
          >
            {t('releaseNotes.generateWithAi', 'Generate with AI')}
          </Button>
        </div>
      )}

      {/* Notes display */}
      {releaseId && notes && (
        <div className="space-y-4">
          {/* Notes header */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between flex-wrap gap-2">
                <div className="flex items-center gap-2">
                  <FileText size={16} className="text-accent" />
                  <h2 className="font-semibold text-heading">
                    {t('releaseNotes.generatedNotes', 'Release Notes')}
                  </h2>
                  <Badge variant={statusBadge(notes.status)}>{notes.status}</Badge>
                </div>
                <Button
                  variant="secondary"
                  icon={<RefreshCw size={14} />}
                  loading={regenerateMutation.isPending}
                  onClick={() => regenerateMutation.mutate()}
                >
                  {t('releaseNotes.regenerate', 'Regenerate')}
                </Button>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <Cpu size={14} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseNotes.model', 'Model')}</p>
                    <p className="text-sm font-medium text-heading truncate">{notes.modelUsed}</p>
                  </div>
                </div>
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <Hash size={14} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseNotes.tokens', 'Tokens')}</p>
                    <p className="text-sm font-medium text-heading">{notes.tokensUsed?.toLocaleString()}</p>
                  </div>
                </div>
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <RefreshCw size={14} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseNotes.regenerations', 'Regenerations')}</p>
                    <p className="text-sm font-medium text-heading">{notes.regenerationCount}</p>
                  </div>
                </div>
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <Clock size={14} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseNotes.generatedAt', 'Generated At')}</p>
                    <p className="text-sm font-medium text-heading">
                      {new Date(notes.generatedAt).toLocaleDateString()}
                    </p>
                  </div>
                </div>
              </div>

              {notes.lastRegeneratedAt && (
                <p className="text-xs text-muted">
                  <RefreshCw size={10} className="inline mr-1" />
                  {t('releaseNotes.regeneratedAt', 'Regenerated')}{' '}
                  {new Date(notes.lastRegeneratedAt).toLocaleString()}
                </p>
              )}
            </CardBody>
          </Card>

          {/* Sections */}
          {sections.map((section) => (
            <Card key={section.key}>
              <CardHeader>
                <button
                  className="w-full flex items-center justify-between text-left"
                  onClick={() => toggleSection(section.key)}
                >
                  <div className="flex items-center gap-2">
                    <CheckCircle2 size={14} className="text-success" />
                    <h3 className="font-medium text-heading">{section.label}</h3>
                  </div>
                  <span className="text-xs text-muted">
                    {expandedSections.has(section.key)
                      ? t('common.collapse', 'Collapse')
                      : t('common.expand', 'Expand')}
                  </span>
                </button>
              </CardHeader>
              {expandedSections.has(section.key) && (
                <CardBody>
                  <div className="prose prose-sm max-w-none text-heading">
                    <pre className="whitespace-pre-wrap text-sm font-sans leading-relaxed bg-surface rounded-lg p-4 border border-edge">
                      {section.content}
                    </pre>
                  </div>
                </CardBody>
              )}
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
