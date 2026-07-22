import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Rocket, Clock, AlertTriangle, Heart, TrendingUp,
  ArrowUpRight, ArrowDownRight, Minus,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, ContentGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { queryKeys } from '../../../shared/api/queryKeys';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { changeConfidenceApi } from '../api/changeConfidence';
import { RiskScoreTrendPanel } from '../components/RiskScoreTrendPanel';
import type { DoraClassification } from '../api/changeConfidence';

// ── Helpers de classificação ─────────────────────────────────────────────────

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

/** Mapeia a classificação DORA para a cor de StatCard. */
const classificationStatColor = (c: DoraClassification): string => {
  switch (c) {
    case 'Elite': return 'text-success';
    case 'High': return 'text-info';
    case 'Medium': return 'text-warning';
    case 'Low': return 'text-critical';
  }
};

// ── Opções de filtros ────────────────────────────────────────────────────────

const ENVIRONMENT_OPTIONS = [
  { value: 'Production', label: 'Production' },
  { value: 'Staging', label: 'Staging' },
  { value: 'Development', label: 'Development' },
];

const DAYS_OPTIONS = [
  { value: '7', label: '7' },
  { value: '14', label: '14' },
  { value: '30', label: '30' },
  { value: '60', label: '60' },
  { value: '90', label: '90' },
];

/**
 * Página de métricas DORA — calcula as 4 métricas DORA a partir de dados reais
 * de releases e incidentes do NexTraceOne.
 *
 * Diferencial: DORA com contexto de contratos, ownership e blast radius.
 */
export function DoraMetricsPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
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
    queryKey: [...queryKeys.changes.dora(queryParams as Record<string, unknown>), activeEnvironmentId],
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

  if (isError || !data?.deploymentFrequency) {
    return (
      <PageContainer>
        <PageHeader title={t('doraMetrics.title')} subtitle={t('doraMetrics.subtitle')} />
        <PageErrorState message={t('common.errorLoading')} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* Cabeçalho com filtros inline */}
      <PageHeader
        title={t('doraMetrics.title')}
        subtitle={t('doraMetrics.subtitle')}
      >
        {/* Barra de filtros — abaixo do título, acima do conteúdo */}
        <div className="flex flex-wrap items-end gap-3 mt-4">
          <TextField
            label={t('doraMetrics.filterService')}
            value={serviceName}
            onChange={e => setServiceName(e.target.value)}
            placeholder={t('doraMetrics.allServices')}
            size="sm"
            className="w-44"
          />
          <TextField
            label={t('doraMetrics.filterTeam')}
            value={teamName}
            onChange={e => setTeamName(e.target.value)}
            placeholder={t('doraMetrics.allTeams')}
            size="sm"
            className="w-44"
          />
          <Select
            label={t('doraMetrics.filterEnvironment')}
            value={environment}
            options={ENVIRONMENT_OPTIONS}
            size="sm"
            onChange={e => setEnvironment(e.target.value)}
            className="w-40"
          />
          <Select
            label={t('doraMetrics.filterDays')}
            value={String(days)}
            options={DAYS_OPTIONS}
            size="sm"
            onChange={e => setDays(Number(e.target.value))}
            className="w-28"
          />
        </div>
      </PageHeader>

      {/* Banner de classificação geral */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex items-center gap-3">
            <TrendingUp size={24} className={classificationColor(data.overallClassification)} />
            <div>
              <p className="text-xs text-muted">{t('doraMetrics.overallPerformance')}</p>
              <div className="flex items-center gap-2">
                <span className={`text-xl font-bold ${classificationColor(data.overallClassification)}`}>
                  {t(`doraMetrics.classification.${data.overallClassification}`)}
                </span>
                {classificationIcon(data.overallClassification)}
                <Badge variant={classificationBadge(data.overallClassification)}>
                  {t('doraMetrics.days', { count: data.periodDays })}
                </Badge>
              </div>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* 4 KPIs DORA via StatCard */}
      <ContentGrid columns={2} className="mb-6">
        {/* Deployment Frequency */}
        <StatCard
          title={t('doraMetrics.deploymentFrequency')}
          value={`${data.deploymentFrequency.deploysPerDay}`}
          icon={<Rocket size={18} />}
          color={classificationStatColor(data.deploymentFrequency.classification)}
          context={`${t('doraMetrics.deploysPerDay')} · ${t('doraMetrics.totalDeploys')}: ${data.deploymentFrequency.totalDeploys}`}
          footer={t(`doraMetrics.classification.${data.deploymentFrequency.classification}`)}
        />

        {/* Lead Time for Changes */}
        <StatCard
          title={t('doraMetrics.leadTime')}
          value={`${data.leadTimeForChanges.averageHours}h`}
          icon={<Clock size={18} />}
          color={classificationStatColor(data.leadTimeForChanges.classification)}
          context={t('doraMetrics.hours')}
          footer={t(`doraMetrics.classification.${data.leadTimeForChanges.classification}`)}
        />

        {/* Change Failure Rate */}
        <StatCard
          title={t('doraMetrics.changeFailureRate')}
          value={`${data.changeFailureRate.failurePercentage}%`}
          icon={<AlertTriangle size={18} />}
          color={classificationStatColor(data.changeFailureRate.classification)}
          context={`${t('doraMetrics.failedDeploys')}: ${data.changeFailureRate.failedDeploys} · ${t('doraMetrics.rolledBackDeploys')}: ${data.changeFailureRate.rolledBackDeploys}`}
          footer={t(`doraMetrics.classification.${data.changeFailureRate.classification}`)}
        />

        {/* Time to Restore Service */}
        <StatCard
          title={t('doraMetrics.timeToRestore')}
          value={`${data.timeToRestoreService.averageHours}h`}
          icon={<Heart size={18} />}
          color={classificationStatColor(data.timeToRestoreService.classification)}
          context={t('doraMetrics.avgResolution')}
          footer={t(`doraMetrics.classification.${data.timeToRestoreService.classification}`)}
        />
      </ContentGrid>

      {/* Tendência de risco (Gap 12) */}
      <PageSection>
        <RiskScoreTrendPanel initialServiceName={serviceName} />
      </PageSection>
    </PageContainer>
  );
}
