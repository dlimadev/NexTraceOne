import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Rocket, Clock, AlertTriangle, Heart, TrendingUp,
  ArrowUpRight, ArrowDownRight, Minus,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { queryKeys } from '../../../shared/api/queryKeys';
import { changeConfidenceApi } from '../api/changeConfidence';
import { RiskScoreTrendPanel } from '../components/RiskScoreTrendPanel';
import type { DoraClassification } from '../api/changeConfidence';

const classificationColor = (c: DoraClassification): string => {
  switch (c) {
    case 'Elite': return 'text-success';
    case 'High': return 'text-info';
    case 'Medium': return 'text-warning';
    case 'Low': return 'text-critical';
  }
};

const classificationBadge = (c: DoraClassification): 'success' | 'info' | 'warning' | 'danger' => {
  switch (c) {
    case 'Elite': return 'success';
    case 'High': return 'info';
    case 'Medium': return 'warning';
    case 'Low': return 'danger';
  }
};

const classificationIcon = (c: DoraClassification) => {
  switch (c) {
    case 'Elite':
    case 'High':
      return <ArrowUpRight size={14} className="text-success" />;
    case 'Medium':
      return <Minus size={14} className="text-warning" />;
    case 'Low':
      return <ArrowDownRight size={14} className="text-critical" />;
  }
};

/**
 * Página de métricas DORA — calcula as 4 métricas DORA a partir de dados reais
 * de releases e incidentes do NexTraceOne.
 *
 * Diferencial: DORA com contexto de contratos, ownership e blast radius.
 */
export function DoraMetricsPage() {
  const { t } = useTranslation();
  const [serviceName, setServiceName] = useState('');
  const [teamName, setTeamName] = useState('');
  const [environment, setEnvironment] = useState('Production');
  const [days, setDays] = useState(30);

  const queryParams = {
    serviceName: serviceName || undefined,
    teamName: teamName || undefined,
    environment: environment || undefined,
    days,
  };

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.changes.dora(queryParams as Record<string, unknown>),
    queryFn: () => changeConfidenceApi.getDoraMetrics(queryParams),
    staleTime: 60_000,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageHeader title={t('doraMetrics.title')} subtitle={t('doraMetrics.subtitle')} />
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <PageHeader title={t('doraMetrics.title')} subtitle={t('doraMetrics.subtitle')} />
        <PageErrorState message={t('common.errorLoading')} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader title={t('doraMetrics.title')} subtitle={t('doraMetrics.subtitle')} />

      {/* Filters */}
      <div className="flex flex-wrap items-end gap-3 mb-6">
        <div>
          <label className="block text-xs font-medium text-muted mb-1">{t('doraMetrics.filterService')}</label>
          <input
            type="text"
            value={serviceName}
            onChange={e => setServiceName(e.target.value)}
            placeholder={t('doraMetrics.allServices')}
            className="px-3 py-1.5 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent w-44"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-muted mb-1">{t('doraMetrics.filterTeam')}</label>
          <input
            type="text"
            value={teamName}
            onChange={e => setTeamName(e.target.value)}
            placeholder={t('doraMetrics.allTeams')}
            className="px-3 py-1.5 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent w-44"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-muted mb-1">{t('doraMetrics.filterEnvironment')}</label>
          <select
            value={environment}
            onChange={e => setEnvironment(e.target.value)}
            className="px-3 py-1.5 text-sm rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
          >
            <option value="Production">{t('environment.profile.production', 'Production')}</option>
            <option value="Staging">{t('environment.profile.staging', 'Staging')}</option>
            <option value="Development">{t('environment.profile.development', 'Development')}</option>
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-muted mb-1">{t('doraMetrics.filterDays')}</label>
          <select
            value={days}
            onChange={e => setDays(Number(e.target.value))}
            className="px-3 py-1.5 text-sm rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
          >
            <option value={7}>7</option>
            <option value={14}>14</option>
            <option value={30}>30</option>
            <option value={60}>60</option>
            <option value={90}>90</option>
          </select>
        </div>
      </div>

      {/* Overall Classification */}
      <div className="flex items-center gap-3 mb-6 p-4 rounded-lg bg-panel border border-edge">
        <TrendingUp size={24} className={classificationColor(data.overallClassification)} />
        <div>
          <p className="text-xs text-muted">{t('doraMetrics.overallPerformance')}</p>
          <div className="flex items-center gap-2">
            <span className={`text-xl font-bold ${classificationColor(data.overallClassification)}`}>
              {t(`doraMetrics.classification.${data.overallClassification}`)}
            </span>
            <Badge variant={classificationBadge(data.overallClassification)}>
              {t('doraMetrics.days', { count: data.periodDays })}
            </Badge>
          </div>
        </div>
      </div>

      {/* DORA Metric Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        {/* Deployment Frequency */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Rocket size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">{t('doraMetrics.deploymentFrequency')}</h3>
              {classificationIcon(data.deploymentFrequency.classification)}
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex items-baseline gap-2 mb-2">
              <span className="text-3xl font-bold text-heading">{data.deploymentFrequency.deploysPerDay}</span>
              <span className="text-sm text-muted">{t('doraMetrics.deploysPerDay')}</span>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted mb-3">
              <span>{t('doraMetrics.totalDeploys')}: {data.deploymentFrequency.totalDeploys}</span>
            </div>
            <Badge variant={classificationBadge(data.deploymentFrequency.classification)}>
              {t(`doraMetrics.classification.${data.deploymentFrequency.classification}`)}
            </Badge>
          </CardBody>
        </Card>

        {/* Lead Time for Changes */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Clock size={16} className="text-info" />
              <h3 className="text-sm font-semibold text-heading">{t('doraMetrics.leadTime')}</h3>
              {classificationIcon(data.leadTimeForChanges.classification)}
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex items-baseline gap-2 mb-2">
              <span className="text-3xl font-bold text-heading">{data.leadTimeForChanges.averageHours}</span>
              <span className="text-sm text-muted">{t('doraMetrics.hours')}</span>
            </div>
            <div className="h-5" />
            <Badge variant={classificationBadge(data.leadTimeForChanges.classification)}>
              {t(`doraMetrics.classification.${data.leadTimeForChanges.classification}`)}
            </Badge>
          </CardBody>
        </Card>

        {/* Change Failure Rate */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <AlertTriangle size={16} className="text-warning" />
              <h3 className="text-sm font-semibold text-heading">{t('doraMetrics.changeFailureRate')}</h3>
              {classificationIcon(data.changeFailureRate.classification)}
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex items-baseline gap-2 mb-2">
              <span className="text-3xl font-bold text-heading">{data.changeFailureRate.failurePercentage}%</span>
            </div>
            <div className="flex items-center gap-3 text-xs text-muted mb-3">
              <span>{t('doraMetrics.failedDeploys')}: {data.changeFailureRate.failedDeploys}</span>
              <span>{t('doraMetrics.rolledBackDeploys')}: {data.changeFailureRate.rolledBackDeploys}</span>
              <span>{t('doraMetrics.totalDeploysLabel')}: {data.changeFailureRate.totalDeploys}</span>
            </div>
            <Badge variant={classificationBadge(data.changeFailureRate.classification)}>
              {t(`doraMetrics.classification.${data.changeFailureRate.classification}`)}
            </Badge>
          </CardBody>
        </Card>

        {/* Time to Restore Service */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Heart size={16} className="text-critical" />
              <h3 className="text-sm font-semibold text-heading">{t('doraMetrics.timeToRestore')}</h3>
              {classificationIcon(data.timeToRestoreService.classification)}
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex items-baseline gap-2 mb-2">
              <span className="text-3xl font-bold text-heading">{data.timeToRestoreService.averageHours}</span>
              <span className="text-sm text-muted">{t('doraMetrics.hours')}</span>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted mb-3">
              <span>{t('doraMetrics.avgResolution')}</span>
            </div>
            <Badge variant={classificationBadge(data.timeToRestoreService.classification)}>
              {t(`doraMetrics.classification.${data.timeToRestoreService.classification}`)}
            </Badge>
          </CardBody>
        </Card>
      </div>

      {/* Risk Score Trend (Gap 12) */}
      <div className="mt-8">
        <RiskScoreTrendPanel initialServiceName={serviceName} />
      </div>
    </PageContainer>
  );
}
