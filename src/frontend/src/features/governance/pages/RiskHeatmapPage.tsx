import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Grid3X3, AlertTriangle, ShieldAlert, FileWarning, BookOpen,
  GitBranch, RotateCcw, AlertCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { RiskHeatmapResponse, RiskLevel } from '../../../types';

type HeatmapDimension = 'domain' | 'team' | 'criticality';

/**
 * Dados simulados do heatmap de risco — alinhados com o backend GetRiskHeatmap.
 * Em produção, virão da API /api/v1/governance/executive/heatmap.
 */
const mockHeatmap: RiskHeatmapResponse = {
  dimension: 'domain',
  cells: [
    { groupId: 'g-payments', groupName: 'Payments', riskLevel: 'Critical', riskScore: 92, incidents: 8, changeFailures: 4, reliabilityDegradation: true, contractGaps: 3, documentationGaps: 2, runbookGaps: 2, dependencyFragility: 7, regressionCount: 3, explanation: 'Recurring production incidents with SLA breaches and high dependency fragility' },
    { groupId: 'g-orders', groupName: 'Orders', riskLevel: 'High', riskScore: 78, incidents: 4, changeFailures: 3, reliabilityDegradation: true, contractGaps: 4, documentationGaps: 1, runbookGaps: 3, dependencyFragility: 5, regressionCount: 2, explanation: 'Breaking contract changes and consumer lag detected' },
    { groupId: 'g-inventory', groupName: 'Inventory', riskLevel: 'High', riskScore: 71, incidents: 3, changeFailures: 2, reliabilityDegradation: false, contractGaps: 2, documentationGaps: 3, runbookGaps: 2, dependencyFragility: 4, regressionCount: 1, explanation: 'Missing ownership definitions and consumer lag after deployments' },
    { groupId: 'g-catalog', groupName: 'Catalog', riskLevel: 'Medium', riskScore: 55, incidents: 2, changeFailures: 1, reliabilityDegradation: false, contractGaps: 1, documentationGaps: 2, runbookGaps: 1, dependencyFragility: 3, regressionCount: 0, explanation: 'Integration partner SLA issues and documentation gaps' },
    { groupId: 'g-platform', groupName: 'Platform', riskLevel: 'Medium', riskScore: 48, incidents: 1, changeFailures: 1, reliabilityDegradation: false, contractGaps: 2, documentationGaps: 3, runbookGaps: 4, dependencyFragility: 2, regressionCount: 0, explanation: 'Shared services missing runbooks and documentation' },
    { groupId: 'g-identity', groupName: 'Identity', riskLevel: 'Low', riskScore: 25, incidents: 0, changeFailures: 0, reliabilityDegradation: false, contractGaps: 1, documentationGaps: 0, runbookGaps: 0, dependencyFragility: 1, regressionCount: 0, explanation: 'Minor schema mismatch in staging environment' },
    { groupId: 'g-analytics', groupName: 'Analytics', riskLevel: 'Low', riskScore: 18, incidents: 0, changeFailures: 0, reliabilityDegradation: false, contractGaps: 0, documentationGaps: 1, runbookGaps: 0, dependencyFragility: 1, regressionCount: 0, explanation: 'Stable domain with minor documentation improvement needed' },
    { groupId: 'g-notifications', groupName: 'Notifications', riskLevel: 'Low', riskScore: 15, incidents: 0, changeFailures: 0, reliabilityDegradation: false, contractGaps: 0, documentationGaps: 0, runbookGaps: 1, dependencyFragility: 0, regressionCount: 0, explanation: 'Well governed with minor runbook gap' },
  ],
  generatedAt: new Date().toISOString(),
};

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
 * Página de Heatmap de Risco — visualização multidimensional do risco operacional.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function RiskHeatmapPage() {
  const { t } = useTranslation();
  const [dimension, setDimension] = useState<HeatmapDimension>('domain');

  const d = mockHeatmap;
  const dimensions: HeatmapDimension[] = ['domain', 'team', 'criticality'];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.executive.heatmapTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.executive.heatmapSubtitle')}</p>
      </div>

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
            {t(`governance.executive.dimension.${dim}`)}
          </button>
        ))}
      </div>

      {/* Heatmap Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {d.cells.map(cell => (
          <Card key={cell.groupId} className={`${riskBorderClass(cell.riskLevel)} border-l-4`}>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Grid3X3 size={14} className="text-muted" />
                  <span className="text-sm font-medium text-heading">{cell.groupName}</span>
                </div>
                <Badge variant={riskBadgeVariant(cell.riskLevel)}>
                  {t(`governance.executive.riskLevel.${cell.riskLevel}`)}
                </Badge>
              </div>
              <p className="text-xs text-muted mt-1">
                {t('governance.executive.riskScore')}: <span className="font-medium text-heading">{cell.riskScore}</span>
              </p>
            </CardHeader>
            <CardBody>
              {/* Indicators */}
              <div className="grid grid-cols-2 gap-2 mb-3">
                <div className="flex items-center gap-1.5 text-xs">
                  <AlertCircle size={12} className="text-critical shrink-0" />
                  <span className="text-muted">{t('governance.executive.incidents')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.incidents}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <AlertTriangle size={12} className="text-orange-400 shrink-0" />
                  <span className="text-muted">{t('governance.executive.changeFailures')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.changeFailures}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <FileWarning size={12} className="text-amber-400 shrink-0" />
                  <span className="text-muted">{t('governance.executive.contractGaps')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.contractGaps}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <BookOpen size={12} className="text-amber-400 shrink-0" />
                  <span className="text-muted">{t('governance.executive.docGaps')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.documentationGaps}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <ShieldAlert size={12} className="text-orange-400 shrink-0" />
                  <span className="text-muted">{t('governance.executive.runbookGaps')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.runbookGaps}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <GitBranch size={12} className="text-muted shrink-0" />
                  <span className="text-muted">{t('governance.executive.depFragility')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.dependencyFragility}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <RotateCcw size={12} className="text-muted shrink-0" />
                  <span className="text-muted">{t('governance.executive.regressions')}</span>
                  <span className="font-medium text-heading ml-auto">{cell.regressionCount}</span>
                </div>
                <div className="flex items-center gap-1.5 text-xs">
                  <span className="text-muted">{t('governance.executive.reliabilityDeg')}</span>
                  <Badge variant={cell.reliabilityDegradation ? 'danger' : 'success'} className="ml-auto">
                    {cell.reliabilityDegradation
                      ? t('governance.executive.yes')
                      : t('governance.executive.no')}
                  </Badge>
                </div>
              </div>

              {/* Explanation */}
              <p className="text-xs text-muted italic">{cell.explanation}</p>
            </CardBody>
          </Card>
        ))}
      </div>
    </div>
  );
}
