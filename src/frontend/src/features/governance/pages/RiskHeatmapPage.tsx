import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Grid3X3, AlertTriangle, ShieldAlert, FileWarning, BookOpen,
  Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { RiskHeatmapResponse, RiskLevel } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { organizationGovernanceApi } from '../api/organizationGovernance';

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
    case 'Critical': return 'border-red-500/60';
    case 'High': return 'border-amber-500/60';
    case 'Medium': return 'border-yellow-500/40';
    case 'Low': return 'border-emerald-500/40';
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
  const [dimension, setDimension] = useState<HeatmapDimension>('category');
  const [data, setData] = useState<RiskHeatmapResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const dimensions: HeatmapDimension[] = ['category', 'domain', 'team'];

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setLoading(true);
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setError(null);
    organizationGovernanceApi.getRiskHeatmap(dimension)
      .then((d) => { if (!cancelled) { setData(d); setLoading(false); } })
      .catch((err) => { if (!cancelled) { setError(err.message || t('common.errorLoading')); setLoading(false); } });
    return () => { cancelled = true; };
  }, [dimension, t]);

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

      {loading && (
        <div className="flex items-center justify-center py-20">
          <Loader2 size={32} className="animate-spin text-accent" />
        </div>
      )}

      {!loading && (error || !data) && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <AlertTriangle size={32} className="text-critical mb-2" />
          <p className="text-sm text-muted">{error ?? t('common.errorLoading')}</p>
        </div>
      )}

      {/* Heatmap Grid */}
      {!loading && data && (
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
                    <AlertTriangle size={12} className="text-orange-400 shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapChangeFailures')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.changeFailures}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <FileWarning size={12} className="text-amber-400 shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapContractGaps')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.contractGaps}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <BookOpen size={12} className="text-amber-400 shrink-0" />
                    <span className="text-muted">{t('governance.executive.heatmapDocumentationGaps')}</span>
                    <span className="font-medium text-heading ml-auto">{cell.documentationGaps}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs">
                    <ShieldAlert size={12} className="text-orange-400 shrink-0" />
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

