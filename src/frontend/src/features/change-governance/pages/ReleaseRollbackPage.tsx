import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  RotateCcw,
  Search,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Shield,
  Users,
  GitBranch,
  Database,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi } from '../api/changeIntelligence';

/**
 * ReleaseRollbackPage — avaliação e gestão de rollback de releases.
 *
 * Permite engineers e tech leads:
 * - Consultar a avaliação de viabilidade de rollback para uma release
 * - Registar a avaliação de rollback com os factores relevantes
 * - Ver readiness score e recomendação de rollback
 * - Executar rollback com justificativa auditada
 */
export function ReleaseRollbackPage() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const [releaseId, setReleaseId] = useState('');
  const [inputValue, setInputValue] = useState('');
  const [assessForm, setAssessForm] = useState({
    isViable: true,
    previousVersion: '',
    hasReversibleMigrations: true,
    consumersAlreadyMigrated: 0,
    totalConsumersImpacted: 0,
    inviabilityReason: '',
    recommendation: '',
  });
  const [rollbackReason, setRollbackReason] = useState('');
  const [showAssessForm, setShowAssessForm] = useState(false);
  const [showRollbackForm, setShowRollbackForm] = useState(false);

  const {
    data: assessment,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['rollback-assessment', releaseId],
    queryFn: () => changeIntelligenceApi.getRollbackAssessment(releaseId),
    enabled: !!releaseId,
    retry: false,
  });

  const assessMutation = useMutation({
    mutationFn: (data: typeof assessForm) =>
      changeIntelligenceApi.assessRollbackViability(releaseId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rollback-assessment', releaseId] });
      setShowAssessForm(false);
    },
  });

  const rollbackMutation = useMutation({
    mutationFn: (reason: string) =>
      changeIntelligenceApi.registerRollback(releaseId, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rollback-assessment', releaseId] });
      setShowRollbackForm(false);
      setRollbackReason('');
    },
  });

  function handleSearch() {
    setReleaseId(inputValue.trim());
  }

  function readinessBadge(score: number): 'success' | 'warning' | 'danger' {
    if (score >= 70) return 'success';
    if (score >= 40) return 'warning';
    return 'danger';
  }

  const inputCls =
    'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

  return (
    <PageContainer>
      <PageHeader
        icon={<RotateCcw className="text-accent" />}
        title={t('releaseRollback.title', 'Release Rollback')}
        subtitle={t(
          'releaseRollback.subtitle',
          'Assess rollback viability, evaluate risk factors and execute controlled rollback with full audit trail',
        )}
      />

      {/* Search */}
      <div className="mb-6">
        <Card>
          <CardBody>
            <p className="text-sm text-muted mb-3">
              {t('releaseRollback.enterReleaseId', 'Enter a Release ID to load or create a rollback assessment')}
            </p>
            <div className="flex gap-2">
              <input
                className={inputCls}
                placeholder={t('releaseRollback.releaseIdPlaceholder', 'Release ID (UUID)…')}
                value={inputValue}
                onChange={(e) => setInputValue(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              />
              <Button variant="primary" icon={<Search size={16} />} onClick={handleSearch}>
                {t('releaseRollback.search', 'Search')}
              </Button>
            </div>
          </CardBody>
        </Card>
      </div>

      {!releaseId && (
        <EmptyState
          icon={<RotateCcw size={40} />}
          title={t('releaseRollback.emptyTitle', 'No assessment loaded')}
          description={t('releaseRollback.emptyDescription', 'Search for a release ID above to view or create its rollback assessment')}
        />
      )}

      {releaseId && isLoading && <PageLoadingState />}

      {/* No assessment yet */}
      {releaseId && isError && !showAssessForm && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <div className="flex items-center gap-3">
                <AlertTriangle className="text-warning" size={20} />
                <div>
                  <p className="text-sm font-medium text-heading">
                    {t('releaseRollback.noAssessment', 'No rollback assessment found for this release')}
                  </p>
                  <p className="text-xs text-muted mt-1">
                    {t('releaseRollback.createAssessmentHint', 'Create an assessment to evaluate rollback viability')}
                  </p>
                </div>
              </div>
            </CardBody>
          </Card>
          <Button
            variant="secondary"
            icon={<Shield size={16} />}
            onClick={() => setShowAssessForm(true)}
          >
            {t('releaseRollback.createAssessment', 'Create Rollback Assessment')}
          </Button>
        </div>
      )}

      {/* Assessment form */}
      {showAssessForm && (
        <Card>
          <CardHeader>
            <h3 className="font-medium text-heading">
              {t('releaseRollback.assessmentForm', 'Rollback Assessment')}
            </h3>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs text-muted mb-1">
                  {t('releaseRollback.previousVersion', 'Previous Version')}
                </label>
                <input
                  className={inputCls}
                  value={assessForm.previousVersion}
                  onChange={(e) => setAssessForm((f) => ({ ...f, previousVersion: e.target.value }))}
                  placeholder="e.g. 1.0.4"
                />
              </div>
              <div>
                <label className="block text-xs text-muted mb-1">
                  {t('releaseRollback.totalConsumers', 'Total Consumers Impacted')}
                </label>
                <input
                  type="number"
                  className={inputCls}
                  value={assessForm.totalConsumersImpacted}
                  onChange={(e) =>
                    setAssessForm((f) => ({ ...f, totalConsumersImpacted: Number(e.target.value) }))
                  }
                />
              </div>
              <div>
                <label className="block text-xs text-muted mb-1">
                  {t('releaseRollback.consumersAlreadyMigrated', 'Consumers Already Migrated')}
                </label>
                <input
                  type="number"
                  className={inputCls}
                  value={assessForm.consumersAlreadyMigrated}
                  onChange={(e) =>
                    setAssessForm((f) => ({ ...f, consumersAlreadyMigrated: Number(e.target.value) }))
                  }
                />
              </div>
              <div className="flex items-center gap-4 pt-4">
                <label className="flex items-center gap-2 text-sm text-heading cursor-pointer">
                  <input
                    type="checkbox"
                    checked={assessForm.isViable}
                    onChange={(e) => setAssessForm((f) => ({ ...f, isViable: e.target.checked }))}
                    className="rounded"
                  />
                  {t('releaseRollback.isViable', 'Rollback is Viable')}
                </label>
                <label className="flex items-center gap-2 text-sm text-heading cursor-pointer">
                  <input
                    type="checkbox"
                    checked={assessForm.hasReversibleMigrations}
                    onChange={(e) =>
                      setAssessForm((f) => ({ ...f, hasReversibleMigrations: e.target.checked }))
                    }
                    className="rounded"
                  />
                  {t('releaseRollback.reversibleMigrations', 'Reversible Migrations')}
                </label>
              </div>
              <div className="md:col-span-2">
                <label className="block text-xs text-muted mb-1">
                  {t('releaseRollback.recommendation', 'Recommendation *')}
                </label>
                <textarea
                  className={inputCls}
                  rows={3}
                  value={assessForm.recommendation}
                  onChange={(e) => setAssessForm((f) => ({ ...f, recommendation: e.target.value }))}
                  placeholder={t('releaseRollback.recommendationPlaceholder', 'Describe the recommended action and rationale…')}
                />
              </div>
              {!assessForm.isViable && (
                <div className="md:col-span-2">
                  <label className="block text-xs text-muted mb-1">
                    {t('releaseRollback.inviabilityReason', 'Reason for Inviability')}
                  </label>
                  <input
                    className={inputCls}
                    value={assessForm.inviabilityReason}
                    onChange={(e) =>
                      setAssessForm((f) => ({ ...f, inviabilityReason: e.target.value }))
                    }
                  />
                </div>
              )}
            </div>
            <div className="flex gap-2 mt-4">
              <Button
                variant="primary"
                loading={assessMutation.isPending}
                onClick={() => assessMutation.mutate(assessForm)}
                disabled={!assessForm.recommendation.trim()}
              >
                {t('releaseRollback.saveAssessment', 'Save Assessment')}
              </Button>
              <Button variant="ghost" onClick={() => setShowAssessForm(false)}>
                {t('common.cancel', 'Cancel')}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Assessment result */}
      {releaseId && assessment && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between flex-wrap gap-2">
                <h2 className="font-semibold text-heading">
                  {t('releaseRollback.assessmentResult', 'Rollback Assessment Result')}
                </h2>
                <div className="flex items-center gap-2">
                  <Badge variant={assessment.isViable ? 'success' : 'danger'}>
                    {assessment.isViable
                      ? t('releaseRollback.viable', 'Viable')
                      : t('releaseRollback.notViable', 'Not Viable')}
                  </Badge>
                  <Badge variant={readinessBadge(assessment.readinessScore ?? 0)}>
                    {t('releaseRollback.readiness', 'Readiness')}: {assessment.readinessScore ?? 0}
                  </Badge>
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <GitBranch size={16} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseRollback.previousVersion', 'Previous Version')}</p>
                    <p className="text-sm font-medium text-heading">{assessment.previousVersion ?? '—'}</p>
                  </div>
                </div>
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <Database size={16} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseRollback.reversibleMigrations', 'Reversible Migrations')}</p>
                    <p className="text-sm font-medium text-heading">
                      {assessment.hasReversibleMigrations ? (
                        <CheckCircle2 size={14} className="text-success inline mr-1" />
                      ) : (
                        <XCircle size={14} className="text-danger inline mr-1" />
                      )}
                      {assessment.hasReversibleMigrations ? 'Yes' : 'No'}
                    </p>
                  </div>
                </div>
                <div className="bg-surface rounded-lg p-3 flex items-center gap-2">
                  <Users size={16} className="text-muted" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseRollback.consumersImpact', 'Consumers Impact')}</p>
                    <p className="text-sm font-medium text-heading">
                      {assessment.consumersAlreadyMigrated ?? 0}/{assessment.totalConsumersImpacted ?? 0}
                    </p>
                  </div>
                </div>
                <div className="bg-surface rounded-lg p-3">
                  <p className="text-xs text-muted">{t('releaseRollback.assessedAt', 'Assessed At')}</p>
                  <p className="text-sm font-medium text-heading">
                    {assessment.assessedAt ? new Date(assessment.assessedAt).toLocaleDateString() : '—'}
                  </p>
                </div>
              </div>

              {assessment.recommendation && (
                <div className="bg-surface rounded-lg p-3 border border-edge mb-4">
                  <p className="text-xs text-muted mb-1">{t('releaseRollback.recommendation', 'Recommendation')}</p>
                  <p className="text-sm text-heading">{assessment.recommendation}</p>
                </div>
              )}

              {assessment.inviabilityReason && (
                <div className="bg-danger/10 rounded-lg p-3 border border-danger/20 mb-4">
                  <p className="text-xs text-danger mb-1">{t('releaseRollback.inviabilityReason', 'Inviability Reason')}</p>
                  <p className="text-sm text-heading">{assessment.inviabilityReason}</p>
                </div>
              )}

              {/* Rollback actions */}
              {assessment.isViable && !showRollbackForm && (
                <Button
                  variant="danger"
                  icon={<RotateCcw size={16} />}
                  onClick={() => setShowRollbackForm(true)}
                >
                  {t('releaseRollback.executeRollback', 'Execute Rollback')}
                </Button>
              )}

              {showRollbackForm && (
                <div className="border border-danger/30 rounded-lg p-4 bg-danger/5 mt-4">
                  <p className="text-sm font-medium text-heading mb-2">
                    {t('releaseRollback.rollbackConfirmation', 'Confirm Rollback — this action will be audited')}
                  </p>
                  <textarea
                    className={inputCls}
                    rows={3}
                    placeholder={t('releaseRollback.rollbackReasonPlaceholder', 'Describe the reason for rollback…')}
                    value={rollbackReason}
                    onChange={(e) => setRollbackReason(e.target.value)}
                  />
                  <div className="flex gap-2 mt-3">
                    <Button
                      variant="danger"
                      loading={rollbackMutation.isPending}
                      onClick={() => rollbackMutation.mutate(rollbackReason)}
                      disabled={!rollbackReason.trim()}
                    >
                      {t('releaseRollback.confirmRollback', 'Confirm Rollback')}
                    </Button>
                    <Button variant="ghost" onClick={() => setShowRollbackForm(false)}>
                      {t('common.cancel', 'Cancel')}
                    </Button>
                  </div>
                </div>
              )}
            </CardBody>
          </Card>
        </div>
      )}
    </PageContainer>
  );
}
