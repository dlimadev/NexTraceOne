import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { FileBarChart2, AlertCircle, Users, GitCommit, Tag } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function riskVariant(score: number | null | undefined): 'success' | 'warning' | 'danger' | 'default' {
  if (score == null) return 'default';
  if (score < 0.3) return 'success';
  if (score <= 0.6) return 'warning';
  return 'danger';
}

/**
 * ReleaseImpactReportPage — relatório de impacto de uma release.
 *
 * Consolida dados de blast radius, work items, commits, risk score,
 * e pedidos de aprovação pendentes numa visão exportável para o PO, PM
 * e Change Advisory Board.
 */
export function ReleaseImpactReportPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [releaseId, setReleaseId] = useState('');
  const [submittedId, setSubmittedId] = useState('');

  const reportQuery = useQuery({
    queryKey: ['release-impact-report', submittedId, activeEnvironmentId],
    queryFn: () => changeIntelligenceApi.getReleaseImpactReport(submittedId),
    enabled: !!submittedId,
  });

  const report = reportQuery.data;

  return (
    <PageContainer>
      <PageHeader
        title={t('impactReport.title')}
        subtitle={t('impactReport.subtitle')}
      />

      {/* Release ID input */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex gap-3 items-end">
            <div className="flex-1">
              <label className="block text-xs font-medium text-muted mb-1">
                {t('impactReport.releaseIdLabel')}
              </label>
              <input
                type="text"
                value={releaseId}
                onChange={(e) => setReleaseId(e.target.value)}
                placeholder={t('impactReport.releaseIdPlaceholder')}
                className={INPUT_CLS}
              />
            </div>
            <button
              onClick={() => setSubmittedId(releaseId)}
              disabled={!releaseId}
              className="px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors whitespace-nowrap"
            >
              {t('impactReport.generateBtn')}
            </button>
          </div>
        </CardBody>
      </Card>

      {reportQuery.isLoading && <PageLoadingState />}
      {reportQuery.isError && <PageErrorState />}

      {report && (
        <div className="space-y-6">
          {/* Header summary */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between flex-wrap gap-2">
                <h2 className="text-base font-semibold text-heading">
                  {report.serviceName} <span className="text-muted font-normal">v{report.version}</span>
                </h2>
                <div className="flex items-center gap-2">
                  <Badge variant="default">{report.status}</Badge>
                  <Badge variant="default">{report.changeLevel}</Badge>
                  {report.riskScore != null && (
                    <Badge variant={riskVariant(report.riskScore)}>
                      {t('impactReport.riskScore')}: {(report.riskScore * 100).toFixed(0)}%
                    </Badge>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="text-center p-3 rounded-md bg-surface">
                  <p className="text-2xl font-bold text-heading">
                    {report.workItemsSummary.total}
                  </p>
                  <p className="text-xs text-muted mt-1">{t('impactReport.workItems')}</p>
                </div>
                <div className="text-center p-3 rounded-md bg-surface">
                  <p className="text-2xl font-bold text-heading">
                    {report.commitsSummary.total}
                  </p>
                  <p className="text-xs text-muted mt-1">{t('impactReport.commits')}</p>
                </div>
                <div className="text-center p-3 rounded-md bg-surface">
                  <p className="text-2xl font-bold text-heading">
                    {report.blastRadius?.totalAffectedConsumers ?? 0}
                  </p>
                  <p className="text-xs text-muted mt-1">{t('impactReport.affectedConsumers')}</p>
                </div>
                <div className="text-center p-3 rounded-md bg-surface">
                  <p className={`text-2xl font-bold ${report.pendingApprovals > 0 ? 'text-warning' : 'text-success'}`}>
                    {report.pendingApprovals}
                  </p>
                  <p className="text-xs text-muted mt-1">{t('impactReport.pendingApprovals')}</p>
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Work Items */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Tag className="w-4 h-4" />
                {t('impactReport.workItemsSection')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="flex gap-4 text-sm">
                <span className="text-muted">
                  {t('impactReport.wiStories')}: <strong className="text-heading">{report.workItemsSummary.stories}</strong>
                </span>
                <span className="text-muted">
                  {t('impactReport.wiBugs')}: <strong className="text-danger">{report.workItemsSummary.bugs}</strong>
                </span>
                <span className="text-muted">
                  {t('impactReport.wiFeatures')}: <strong className="text-success">{report.workItemsSummary.features}</strong>
                </span>
                <span className="text-muted">
                  {t('impactReport.wiOthers')}: <strong className="text-heading">{report.workItemsSummary.others}</strong>
                </span>
              </div>
            </CardBody>
          </Card>

          {/* Commits by author */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <GitCommit className="w-4 h-4" />
                {t('impactReport.commitsSection')} ({report.commitsSummary.total})
              </h2>
            </CardHeader>
            <CardBody>
              {report.commitsSummary.byAuthor.length === 0 ? (
                <p className="text-sm text-muted">{t('impactReport.noCommits')}</p>
              ) : (
                <div className="space-y-1">
                  {report.commitsSummary.byAuthor.map((a) => (
                    <div key={a.author} className="flex items-center gap-2 text-sm">
                      <Users className="w-3.5 h-3.5 text-muted shrink-0" />
                      <span className="text-heading flex-1">{a.author}</span>
                      <Badge variant="default">{a.count}</Badge>
                    </div>
                  ))}
                </div>
              )}
            </CardBody>
          </Card>

          {/* Blast Radius */}
          {report.blastRadius && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <AlertCircle className="w-4 h-4 text-warning" />
                  {t('impactReport.blastRadiusSection')}
                </h2>
              </CardHeader>
              <CardBody>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-xs font-medium text-muted mb-2">
                      {t('impactReport.directConsumers')}
                    </p>
                    {report.blastRadius.directConsumers.length === 0 ? (
                      <p className="text-xs text-muted">{t('impactReport.none')}</p>
                    ) : (
                      <div className="flex flex-wrap gap-1">
                        {report.blastRadius.directConsumers.map((c) => (
                          <Badge key={c} variant="warning">{c}</Badge>
                        ))}
                      </div>
                    )}
                  </div>
                  <div>
                    <p className="text-xs font-medium text-muted mb-2">
                      {t('impactReport.transitiveConsumers')}
                    </p>
                    {report.blastRadius.transitiveConsumers.length === 0 ? (
                      <p className="text-xs text-muted">{t('impactReport.none')}</p>
                    ) : (
                      <div className="flex flex-wrap gap-1">
                        {report.blastRadius.transitiveConsumers.map((c) => (
                          <Badge key={c} variant="default">{c}</Badge>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </CardBody>
            </Card>
          )}

          {/* Generated at */}
          <p className="text-xs text-muted text-center">
            <FileBarChart2 className="w-3.5 h-3.5 inline mr-1" />
            {t('impactReport.generatedAt')}: {new Date(report.generatedAt).toLocaleString()}
          </p>
        </div>
      )}
    </PageContainer>
  );
}
