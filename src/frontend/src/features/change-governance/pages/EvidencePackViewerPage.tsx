import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  PackageCheck,
  Download,
  FileJson,
  Shield,
  GitCommit,
  CheckCircle2,
  AlertTriangle,
  BarChart2,
  RefreshCw,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { workflowApi } from '../api/workflow';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/**
 * EvidencePackViewerPage — visualização de Evidence Packs de instâncias de workflow.
 *
 * Permite auditores, tech leads e engineers:
 * - Selecionar uma instância de workflow e carregar o seu Evidence Pack
 * - Ver metadados do pack: completeness, scores, hashes, CI/CD, aprovações
 * - Gerar um Evidence Pack se ainda não existir
 * - Exportar o Evidence Pack como PDF auditável
 *
 * Evidence Pack é o pacote de artefactos de auditoria produzido no fim de um workflow:
 * diff semântico, blast radius, aprovações, CI/CD checks e score de mudança.
 */
export function EvidencePackViewerPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const qc = useQueryClient();
  const [instanceId, setInstanceId] = useState('');
  const [expandedSection, setExpandedSection] = useState<string | null>('scores');

  const { data: instances, isLoading: loadingInstances } = useQuery({
    queryKey: ['workflow-instances-selector', activeEnvironmentId],
    queryFn: () => workflowApi.listInstances(1, 50),
    staleTime: 60_000,
  });

  const {
    data: pack,
    isLoading: loadingPack,
    isError: packError,
  } = useQuery({
    queryKey: ['evidence-pack', instanceId, activeEnvironmentId],
    queryFn: () => workflowApi.getEvidencePack(instanceId),
    enabled: !!instanceId,
    retry: false,
  });

  const generateMutation = useMutation({
    mutationFn: () =>
      workflowApi.generateEvidencePack(instanceId, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['evidence-pack', instanceId] }),
  });

  const exportMutation = useMutation({
    mutationFn: () => workflowApi.exportEvidencePackPdf(instanceId),
    onSuccess: (data) => {
      const link = document.createElement('a');
      link.href = `data:application/pdf;base64,${data.base64Content}`;
      link.download = data.fileName;
      link.click();
    },
  });

  const instanceList = instances?.items ?? [];
  const selectedInstance = instanceList.find((i) => i.id === instanceId);

  function completenessVariant(pct: number): 'success' | 'warning' | 'danger' {
    if (pct >= 80) return 'success';
    if (pct >= 50) return 'warning';
    return 'danger';
  }

  function scoreVariant(score: number | null): 'success' | 'warning' | 'danger' | 'default' {
    if (score === null) return 'default';
    if (score >= 0.7) return 'success';
    if (score >= 0.4) return 'warning';
    return 'danger';
  }

  function toggleSection(key: string) {
    setExpandedSection((prev) => (prev === key ? null : key));
  }

  return (
    <PageContainer>
      <PageHeader
        icon={<PackageCheck className="text-accent" />}
        title={t('evidencePackViewer.title', 'Evidence Pack Viewer')}
        subtitle={t(
          'evidencePackViewer.subtitle',
          'Inspect the audit evidence pack generated at the end of a workflow — scores, approvals, CI/CD checks and contract diff',
        )}
      />

      {/* Workflow instance selector */}
      <div className="mb-6">
        <Card>
          <CardBody>
            <p className="text-sm text-muted mb-3">
              {t('evidencePackViewer.selectInstance', 'Select a workflow instance to inspect its evidence pack')}
            </p>
            <div className="relative">
              <select
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                value={instanceId}
                onChange={(e) => setInstanceId(e.target.value)}
              >
                <option value="">{t('evidencePackViewer.selectPlaceholder', '— Select a workflow instance —')}</option>
                {loadingInstances && (
                  <option disabled>{t('evidencePackViewer.loading', 'Loading…')}</option>
                )}
                {instanceList.map((i) => (
                  <option key={i.id} value={i.id}>
                    {i.templateName} — {i.status}
                  </option>
                ))}
              </select>
            </div>
          </CardBody>
        </Card>
      </div>

      {!instanceId && (
        <EmptyState
          icon={<PackageCheck size={40} />}
          title={t('evidencePackViewer.emptyTitle', 'No instance selected')}
          description={t('evidencePackViewer.emptyDescription', 'Select a workflow instance above to view its evidence pack')}
        />
      )}

      {instanceId && loadingPack && <PageLoadingState />}

      {/* No evidence pack yet */}
      {instanceId && packError && (
        <Card>
          <CardBody>
            <div className="flex items-start gap-3">
              <AlertTriangle className="text-warning mt-0.5 flex-shrink-0" size={20} />
              <div className="flex-1">
                <p className="text-sm font-medium text-heading mb-1">
                  {t('evidencePackViewer.notFound', 'No evidence pack found for this instance')}
                </p>
                <p className="text-xs text-muted mb-4">
                  {t('evidencePackViewer.generateHint', 'Generate an evidence pack to capture the audit artefacts for this workflow')}
                </p>
                <Button
                  variant="primary"
                  icon={<RefreshCw size={14} />}
                  loading={generateMutation.isPending}
                  onClick={() => generateMutation.mutate()}
                >
                  {t('evidencePackViewer.generate', 'Generate Evidence Pack')}
                </Button>
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Evidence Pack details */}
      {instanceId && pack && (
        <div className="space-y-4">
          {/* Header / summary */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <PackageCheck size={18} className="text-accent" />
                  <span className="font-semibold text-heading">
                    {t('evidencePackViewer.packSummary', 'Evidence Pack')}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={completenessVariant(pack.completenessPercentage)}>
                    {pack.completenessPercentage.toFixed(0)}% {t('evidencePackViewer.complete', 'complete')}
                  </Badge>
                  <Button
                    variant="ghost"
                    icon={<Download size={14} />}
                    loading={exportMutation.isPending}
                    onClick={() => exportMutation.mutate()}
                  >
                    {t('evidencePackViewer.exportPdf', 'Export PDF')}
                  </Button>
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <dl className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <dt className="text-muted text-xs mb-1">{t('evidencePackViewer.generatedAt', 'Generated')}</dt>
                  <dd className="text-heading font-medium">{new Date(pack.generatedAt).toLocaleString()}</dd>
                </div>
                {pack.commitSha && (
                  <div>
                    <dt className="text-muted text-xs mb-1">{t('evidencePackViewer.commitSha', 'Commit SHA')}</dt>
                    <dd className="text-heading font-mono text-xs">{pack.commitSha.slice(0, 12)}</dd>
                  </div>
                )}
                {pack.buildId && (
                  <div>
                    <dt className="text-muted text-xs mb-1">{t('evidencePackViewer.buildId', 'Build ID')}</dt>
                    <dd className="text-heading font-mono text-xs">{pack.buildId}</dd>
                  </div>
                )}
                {pack.pipelineSource && (
                  <div>
                    <dt className="text-muted text-xs mb-1">{t('evidencePackViewer.pipeline', 'Pipeline')}</dt>
                    <dd className="text-heading">{pack.pipelineSource}</dd>
                  </div>
                )}
              </dl>
            </CardBody>
          </Card>

          {/* Scores section */}
          <Card>
            <button
              type="button"
              className="w-full text-left"
              onClick={() => toggleSection('scores')}
            >
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <BarChart2 size={16} className="text-accent" />
                    <span className="font-semibold text-heading">
                      {t('evidencePackViewer.scoresTitle', 'Quality Scores')}
                    </span>
                  </div>
                  {expandedSection === 'scores' ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                </div>
              </CardHeader>
            </button>
            {expandedSection === 'scores' && (
              <CardBody>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="rounded-lg bg-canvas p-4 text-center">
                    <p className="text-xs text-muted mb-1">{t('evidencePackViewer.blastRadius', 'Blast Radius Score')}</p>
                    <p className="text-2xl font-bold text-heading">
                      {pack.blastRadiusScore !== null ? (pack.blastRadiusScore * 100).toFixed(0) + '%' : '—'}
                    </p>
                    <Badge variant={scoreVariant(pack.blastRadiusScore)} className="mt-2 text-xs">
                      {pack.blastRadiusScore !== null
                        ? pack.blastRadiusScore >= 0.7 ? t('evidencePackViewer.low', 'Low Risk') : t('evidencePackViewer.high', 'High Risk')
                        : t('evidencePackViewer.na', 'N/A')}
                    </Badge>
                  </div>
                  <div className="rounded-lg bg-canvas p-4 text-center">
                    <p className="text-xs text-muted mb-1">{t('evidencePackViewer.spectral', 'Spectral Score')}</p>
                    <p className="text-2xl font-bold text-heading">
                      {pack.spectralScore !== null ? (pack.spectralScore * 100).toFixed(0) + '%' : '—'}
                    </p>
                    <Badge variant={scoreVariant(pack.spectralScore)} className="mt-2 text-xs">
                      {pack.spectralScore !== null
                        ? pack.spectralScore >= 0.7 ? t('evidencePackViewer.pass', 'Pass') : t('evidencePackViewer.warn', 'Warning')
                        : t('evidencePackViewer.na', 'N/A')}
                    </Badge>
                  </div>
                  <div className="rounded-lg bg-canvas p-4 text-center">
                    <p className="text-xs text-muted mb-1">{t('evidencePackViewer.changeIntelligence', 'Change Intelligence Score')}</p>
                    <p className="text-2xl font-bold text-heading">
                      {pack.changeIntelligenceScore !== null ? (pack.changeIntelligenceScore * 100).toFixed(0) + '%' : '—'}
                    </p>
                    <Badge variant={scoreVariant(pack.changeIntelligenceScore)} className="mt-2 text-xs">
                      {pack.changeIntelligenceScore !== null
                        ? pack.changeIntelligenceScore >= 0.7 ? t('evidencePackViewer.confident', 'Confident') : t('evidencePackViewer.caution', 'Caution')
                        : t('evidencePackViewer.na', 'N/A')}
                    </Badge>
                  </div>
                </div>
              </CardBody>
            )}
          </Card>

          {/* CI/CD section */}
          {(pack.ciChecksResult || pack.buildId || pack.commitSha) && (
            <Card>
              <button
                type="button"
                className="w-full text-left"
                onClick={() => toggleSection('cicd')}
              >
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <GitCommit size={16} className="text-accent" />
                      <span className="font-semibold text-heading">
                        {t('evidencePackViewer.cicdTitle', 'CI/CD Evidence')}
                      </span>
                    </div>
                    {expandedSection === 'cicd' ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                  </div>
                </CardHeader>
              </button>
              {expandedSection === 'cicd' && (
                <CardBody>
                  <dl className="space-y-3 text-sm">
                    {pack.pipelineSource && (
                      <div className="flex justify-between">
                        <dt className="text-muted">{t('evidencePackViewer.pipelineSource', 'Pipeline')}</dt>
                        <dd className="text-heading">{pack.pipelineSource}</dd>
                      </div>
                    )}
                    {pack.buildId && (
                      <div className="flex justify-between">
                        <dt className="text-muted">{t('evidencePackViewer.buildId', 'Build ID')}</dt>
                        <dd className="font-mono text-xs text-heading">{pack.buildId}</dd>
                      </div>
                    )}
                    {pack.commitSha && (
                      <div className="flex justify-between">
                        <dt className="text-muted">{t('evidencePackViewer.commitSha', 'Commit')}</dt>
                        <dd className="font-mono text-xs text-heading">{pack.commitSha}</dd>
                      </div>
                    )}
                    {pack.ciChecksResult && (
                      <div>
                        <dt className="text-muted mb-1">{t('evidencePackViewer.ciChecks', 'CI Checks Result')}</dt>
                        <dd>
                          <pre className="text-xs bg-canvas rounded p-3 overflow-x-auto">{pack.ciChecksResult}</pre>
                        </dd>
                      </div>
                    )}
                  </dl>
                </CardBody>
              )}
            </Card>
          )}

          {/* Contract diff section */}
          {pack.contractDiffSummary && (
            <Card>
              <button
                type="button"
                className="w-full text-left"
                onClick={() => toggleSection('diff')}
              >
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <FileJson size={16} className="text-accent" />
                      <span className="font-semibold text-heading">
                        {t('evidencePackViewer.diffTitle', 'Contract Diff Summary')}
                      </span>
                    </div>
                    {expandedSection === 'diff' ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                  </div>
                </CardHeader>
              </button>
              {expandedSection === 'diff' && (
                <CardBody>
                  <pre className="text-xs bg-canvas rounded p-3 overflow-x-auto whitespace-pre-wrap">{pack.contractDiffSummary}</pre>
                  {pack.contractHash && (
                    <p className="mt-2 text-xs text-muted flex items-center gap-1">
                      <Shield size={12} />
                      {t('evidencePackViewer.contractHash', 'Contract hash:')} <span className="font-mono">{pack.contractHash}</span>
                    </p>
                  )}
                </CardBody>
              )}
            </Card>
          )}

          {/* Approval history */}
          {pack.approvalHistory && (
            <Card>
              <button
                type="button"
                className="w-full text-left"
                onClick={() => toggleSection('approvals')}
              >
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <CheckCircle2 size={16} className="text-accent" />
                      <span className="font-semibold text-heading">
                        {t('evidencePackViewer.approvalsTitle', 'Approval History')}
                      </span>
                    </div>
                    {expandedSection === 'approvals' ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
                  </div>
                </CardHeader>
              </button>
              {expandedSection === 'approvals' && (
                <CardBody>
                  <pre className="text-xs bg-canvas rounded p-3 overflow-x-auto whitespace-pre-wrap">{pack.approvalHistory}</pre>
                </CardBody>
              )}
            </Card>
          )}
        </div>
      )}
    </PageContainer>
  );
}
