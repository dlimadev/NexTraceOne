import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  ClipboardCheck,
  ExternalLink,
  Info,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeConfidenceApi } from '../api/changeConfidence';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function riskVariant(score: number | null): 'success' | 'warning' | 'danger' | 'default' {
  if (score === null) return 'default';
  if (score < 0.3) return 'success';
  if (score <= 0.6) return 'warning';
  return 'danger';
}

function confidenceVariant(status: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  switch (status) {
    case 'Validated': return 'success';
    case 'NeedsAttention': return 'warning';
    case 'SuspectedRegression':
    case 'CorrelatedWithIncident': return 'danger';
    case 'Mitigated': return 'info';
    default: return 'default';
  }
}

/**
 * ChangeAdvisoryPage — Change Advisory Board (CAB) lightweight.
 *
 * Visão operacional de mudanças pendentes de revisão com contexto de risco,
 * blast radius, confidence score e recomendação de advisory.
 * Permite filtrar por risco, ambiente, serviço e equipa para facilitar decisões.
 */
export function ChangeAdvisoryPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [serviceName, setServiceName] = useState('');
  const [environment, setEnvironment] = useState('');
  const [confidenceStatus, setConfidenceStatus] = useState('');
  const [page] = useState(1);

  const changesQuery = useQuery({
    queryKey: ['changes-advisory', serviceName, environment, confidenceStatus, page, activeEnvironmentId],
    queryFn: () =>
      changeConfidenceApi.listChanges({
        serviceName: serviceName || undefined,
        environment: environment || undefined,
        confidenceStatus: confidenceStatus || undefined,
        page,
        pageSize: 30,
      }),
  });

  const changes = changesQuery.data?.items ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('changeAdvisory.title')}
        subtitle={t('changeAdvisory.subtitle')}
      />

      {/* Filters */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading">{t('changeAdvisory.filters')}</h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('changeAdvisory.filterService')}
              </label>
              <input
                type="text"
                value={serviceName}
                onChange={(e) => setServiceName(e.target.value)}
                placeholder={t('changeAdvisory.filterServicePlaceholder')}
                className={INPUT_CLS}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('changeAdvisory.filterEnvironment')}
              </label>
              <input
                type="text"
                value={environment}
                onChange={(e) => setEnvironment(e.target.value)}
                placeholder={t('changeAdvisory.filterEnvironmentPlaceholder')}
                className={INPUT_CLS}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('changeAdvisory.filterStatus')}
              </label>
              <select
                value={confidenceStatus}
                onChange={(e) => setConfidenceStatus(e.target.value)}
                className={INPUT_CLS}
              >
                <option value="">{t('changeAdvisory.allStatuses')}</option>
                <option value="NeedsAttention">{t('changeConfidence.confidenceStatus.NeedsAttention')}</option>
                <option value="SuspectedRegression">{t('changeConfidence.confidenceStatus.SuspectedRegression')}</option>
                <option value="CorrelatedWithIncident">{t('changeConfidence.confidenceStatus.CorrelatedWithIncident')}</option>
                <option value="NotAssessed">{t('changeConfidence.confidenceStatus.NotAssessed')}</option>
                <option value="Validated">{t('changeConfidence.confidenceStatus.Validated')}</option>
              </select>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Summary metrics */}
      {changesQuery.data && (
        <div className="grid grid-cols-3 gap-4 mb-6">
          <Card>
            <CardBody className="text-center py-4">
              <p className="text-2xl font-bold text-heading">{changesQuery.data.totalCount}</p>
              <p className="text-xs text-muted mt-1">{t('changeAdvisory.totalChanges')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center py-4">
              <p className="text-2xl font-bold text-critical">
                {changes.filter((c) => (c.changeScore ?? 0) > 0.6).length}
              </p>
              <p className="text-xs text-muted mt-1">{t('changeAdvisory.highRiskCount')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center py-4">
              <p className="text-2xl font-bold text-warning">
                {changes.filter((c) => c.confidenceStatus === 'NeedsAttention' || c.confidenceStatus === 'SuspectedRegression').length}
              </p>
              <p className="text-xs text-muted mt-1">{t('changeAdvisory.needsReviewCount')}</p>
            </CardBody>
          </Card>
        </div>
      )}

      {/* Changes list */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <ClipboardCheck size={16} className="text-accent" />
            <h2 className="text-sm font-semibold text-heading">{t('changeAdvisory.pendingReview')}</h2>
            {changesQuery.data && (
              <span className="text-xs text-muted ml-auto">
                {changesQuery.data.totalCount} {t('common.total')}
              </span>
            )}
          </div>
        </CardHeader>

        {changesQuery.isLoading && <PageLoadingState />}
        {changesQuery.isError && <PageErrorState message={t('changeAdvisory.loadFailed')} />}

        {!changesQuery.isLoading && !changesQuery.isError && (
          <>
            {changes.length === 0 ? (
              <CardBody>
                <p className="text-sm text-muted py-12 text-center">{t('changeAdvisory.noChanges')}</p>
              </CardBody>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.service')}</th>
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.version')}</th>
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.environment')}</th>
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.confidence')}</th>
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.risk')}</th>
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.advisory')}</th>
                      <th className="px-5 py-3 font-medium text-muted">{t('changeAdvisory.col.created')}</th>
                      <th className="px-5 py-3 font-medium text-muted"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {changes.map((change) => (
                      <tr key={change.id} className="hover:bg-hover transition-colors">
                        <td className="px-5 py-3">
                          <p className="text-sm font-medium text-heading">{change.serviceName ?? '—'}</p>
                          {change.teamName && (
                            <p className="text-xs text-muted">{change.teamName}</p>
                          )}
                        </td>
                        <td className="px-5 py-3 font-mono text-xs text-muted">{change.version}</td>
                        <td className="px-5 py-3 capitalize text-body">{change.environment ?? '—'}</td>
                        <td className="px-5 py-3">
                          <Badge variant={confidenceVariant(change.confidenceStatus ?? '')}>
                            {t(`changeConfidence.confidenceStatus.${change.confidenceStatus}`, { defaultValue: change.confidenceStatus ?? '—' })}
                          </Badge>
                        </td>
                        <td className="px-5 py-3">
                          {change.changeScore != null ? (
                            <Badge variant={riskVariant(change.changeScore)}>
                              {(change.changeScore * 100).toFixed(0)}%
                            </Badge>
                          ) : (
                            <span className="text-muted">—</span>
                          )}
                        </td>
                        <td className="px-5 py-3">
                          {/* Advisory requires a separate query per change — shown as a quick action */}
                          <Link
                            to={`/changes/${change.id}`}
                            className="text-xs text-info hover:underline"
                          >
                            {t('changeAdvisory.viewAdvisory')}
                          </Link>
                        </td>
                        <td className="px-5 py-3 text-xs text-muted whitespace-nowrap">
                          {new Date(change.createdAt).toLocaleString()}
                        </td>
                        <td className="px-5 py-3">
                          <Link
                            to={`/changes/${change.id}`}
                            className="inline-flex items-center gap-1 text-xs text-accent hover:underline"
                          >
                            <ExternalLink size={12} />
                            {t('changeAdvisory.viewDetails')}
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </Card>

      {/* Guidance */}
      <Card className="mt-6">
        <CardBody>
          <div className="flex items-start gap-3">
            <Info size={16} className="text-info mt-0.5 shrink-0" />
            <div>
              <p className="text-sm font-medium text-heading mb-1">{t('changeAdvisory.guidanceTitle')}</p>
              <p className="text-xs text-muted">{t('changeAdvisory.guidanceDescription')}</p>
              <div className="mt-3 flex gap-3">
                <Link
                  to="/changes"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-elevated border border-edge text-sm text-body hover:text-heading hover:border-accent transition-colors"
                >
                  {t('changeAdvisory.viewCatalog')}
                </Link>
                <Link
                  to="/promotion"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-elevated border border-edge text-sm text-body hover:text-heading hover:border-accent transition-colors"
                >
                  {t('changeAdvisory.viewPromotion')}
                </Link>
              </div>
            </div>
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
