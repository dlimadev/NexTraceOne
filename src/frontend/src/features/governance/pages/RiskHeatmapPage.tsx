import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Grid3X3, AlertTriangle, ShieldAlert, FileWarning, BookOpen,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { RiskLevel } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { queryKeys } from '../../../shared/api/queryKeys';
import { organizationGovernanceApi } from '../api/organizationGovernance';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type HeatmapDimension = 'category' | 'domain' | 'team';

/** Mapeia RiskLevel para variante do Badge. */
const riskBadgeVariant = (level: RiskLevel): 'success' | 'warning' | 'danger' | 'default' => {
  switch (level) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'warning';
    case 'Low': return 'success';
    default: return 'default';
  }
};

/** Mapeia RiskLevel para classe de borda do card. */
const riskBorderClass = (level: RiskLevel): string => {
  switch (level) {
    case 'Critical': return 'border-critical/25';
    case 'High': return 'border-warning/25';
    case 'Medium': return 'border-warning/25';
    case 'Low': return 'border-success/25';
    default: return 'border-edge';
  }
};

/**
 * Página de Heatmap de Risco — visualização de risco por categoria de Governance Pack.
 * Dados reais derivados de Governance Packs, Rollouts e Waivers por categoria.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function RiskHeatmapPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [dimension, setDimension] = useState<HeatmapDimension>('category');
  const dimensions: HeatmapDimension[] = ['category', 'domain', 'team'];

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.governance.executive.heatmap(dimension, activeEnvironmentId),
    queryFn: () => organizationGovernanceApi.getRiskHeatmap(dimension),
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.executive.heatmapTitle')}
        subtitle={t('governance.executive.heatmapSubtitle')}
      />

      {/* Dimension Selector */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        {dimensions.map(dim => (
          <button
            key={dim}
            onClick={() => setDimension(dim)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              dimension === dim
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(`governance.executive.heatmapDimension${dim.charAt(0).toUpperCase()}${dim.slice(1)}`)}
          </button>
        ))}
      </div>

      {isLoading && (
        <PageLoadingState />
      )}

      {!isLoading && (isError || !data) && (
        <PageErrorState message={t('common.errorLoading')} />
      )}

      {/* Heatmap Grid */}
      {!isLoading && data && data.cells.length === 0 && (
        <EmptyState
          title={t('governance.riskHeatmap.empty', 'No risk data available')}
          description={t('governance.riskHeatmap.emptyDescription', 'Risk heatmap cells will appear here once governance packs are assessed.')}
          size="compact"
        />
      )}
      {!isLoading && data && data.cells.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {data.cells.map(cell => (
            <Card key={cell.groupId} className={`${riskBorderClass(cell.riskLevel)} border-l-4`}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Grid3X3 size={14} className="text-muted" />
                    <span className="text-sm font-medium text-heading">{cell.groupName}</span>
                  </div>
                  <Badge variant={riskBadgeVariant(cell.riskLevel)}>
                    {t(`governance.risk.level.${cell.riskLevel}`)}
                  </Badge>
                </div>
                <p className="text-xs text-muted mt-1">
                  {t('governance.executive.heatmapRiskScore')}: <span className="font-medium text-heading">{cell.riskScore}</span>
                </p>
              </CardHeader>
              <CardBody>
                {/* Indicators */}
                <div className="grid grid-cols-2 gap-2 mb-3">
                  <div className="flex items-center gap-1.5 text-xs">
                    <AlertTriangle size={12} className="text-warning shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapChangeFailures')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.changeFailures}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <FileWarning size={12} className="text-warning shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapContractGaps')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.contractGaps}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <BookOpen size={12} className="text-warning shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapDocumentationGaps')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.documentationGaps}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <ShieldAlert size={12} className="text-warning shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapRunbookGaps')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.runbookGaps}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs col-span-2">
                    <span className="text-muted">{t('governance.executive.heatmapReliabilityDegradation')}</span>
                    <Badge variant={cell.reliabilityDegradation ? 'danger' : 'success'} className="ml-auto">
                      {cell.reliabilityDegradation
                        ? t('governance.executive.heatmapYes')
                        : t('governance.executive.heatmapNo')}
                    </Badge>
                  </div>
                </div>

                {/* Explanation */}
                <p className="text-xs text-muted italic">{cell.explanation}</p>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}

