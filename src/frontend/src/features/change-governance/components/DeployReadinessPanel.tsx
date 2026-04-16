import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ShieldCheck, CheckCircle2, XCircle, AlertTriangle, RefreshCw } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeIntelligenceApi } from '../api/changeIntelligence';

interface DeployReadinessPanelProps {
  releaseId: string | null;
  environmentName?: string;
}

/**
 * Painel de verificação de prontidão de deploy.
 * Mostra quais checks passaram/falharam antes de registar um deploy ou promover.
 */
export function DeployReadinessPanel({ releaseId, environmentName }: DeployReadinessPanelProps) {
  const { t } = useTranslation();

  const readinessQuery = useQuery({
    queryKey: ['deploy-readiness', releaseId, environmentName],
    queryFn: () => changeIntelligenceApi.getDeployReadiness(releaseId!, environmentName),
    enabled: !!releaseId,
  });

  if (!releaseId) {
    return (
      <Card>
        <CardBody>
          <p className="text-sm text-muted py-12 text-center">
            {t('deployReadiness.selectRelease')}
          </p>
        </CardBody>
      </Card>
    );
  }

  const data = readinessQuery.data;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ShieldCheck size={16} className="text-accent" />
              <h2 className="text-sm font-semibold text-heading">{t('deployReadiness.title')}</h2>
              {data && (
                <Badge variant={data.isReady ? 'success' : 'danger'}>
                  {data.isReady ? t('deployReadiness.ready') : t('deployReadiness.notReady')}
                </Badge>
              )}
            </div>
            <Button
              variant="secondary"
              onClick={() => readinessQuery.refetch()}
              loading={readinessQuery.isFetching}
            >
              <RefreshCw size={14} />
              {t('common.refresh')}
            </Button>
          </div>
        </CardHeader>

        {readinessQuery.isLoading && <PageLoadingState />}
        {readinessQuery.isError && (
          <CardBody>
            <PageErrorState message={t('deployReadiness.loadFailed')} />
          </CardBody>
        )}

        {data && (
          <CardBody>
            {/* Summary row */}
            <div className="flex items-center gap-6 mb-4 p-3 rounded-md bg-elevated border border-edge">
              <div className="text-center">
                <p className="text-lg font-bold text-heading">{data.passedChecks}</p>
                <p className="text-xs text-success">{t('deployReadiness.passed')}</p>
              </div>
              <div className="text-center">
                <p className="text-lg font-bold text-heading">{data.failedChecks}</p>
                <p className="text-xs text-critical">{t('deployReadiness.failed')}</p>
              </div>
              <div className="text-center">
                <p className="text-lg font-bold text-heading">{data.totalChecks}</p>
                <p className="text-xs text-muted">{t('deployReadiness.total')}</p>
              </div>
              {environmentName && (
                <div className="ml-auto">
                  <Badge variant="info">{environmentName}</Badge>
                </div>
              )}
            </div>

            {/* Checks list */}
            {data.checks.length > 0 ? (
              <ul className="space-y-2">
                {data.checks.map((check) => (
                  <li
                    key={check.checkId}
                    className="flex items-start gap-3 p-3 rounded-md border border-edge bg-surface"
                  >
                    {check.passed ? (
                      <CheckCircle2 size={16} className="text-success mt-0.5 shrink-0" />
                    ) : (
                      <XCircle size={16} className="text-critical mt-0.5 shrink-0" />
                    )}
                    <div className="flex-1">
                      <p className="text-sm font-medium text-heading">{check.description}</p>
                      <p className="text-xs text-muted mt-0.5">{check.message}</p>
                    </div>
                    <Badge variant={check.passed ? 'success' : 'danger'}>
                      {check.passed ? t('common.pass') : t('common.fail')}
                    </Badge>
                  </li>
                ))}
              </ul>
            ) : (
              <div className="flex items-center gap-2 py-4">
                <AlertTriangle size={16} className="text-warning" />
                <p className="text-sm text-muted">{t('deployReadiness.noChecks')}</p>
              </div>
            )}

            <p className="text-xs text-faded mt-3">
              {t('deployReadiness.evaluatedAt')}:{' '}
              {new Date(data.evaluatedAt).toLocaleString()}
            </p>
          </CardBody>
        )}
      </Card>
    </div>
  );
}
