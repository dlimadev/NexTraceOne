import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Layers, ShieldCheck, TrendingUp, TrendingDown, Minus, AlertTriangle,
  FileText, Globe, Zap, Bot, BookOpen, Users,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type {
  ControlDimensionDto, ControlsSummaryResponse, ControlDimensionType,
  MaturityLevelType, GovernanceTrendDirection,
} from '../../../types';

/**
 * Dados simulados de controles enterprise — alinhados com o backend GetControlsSummary.
 * Em produção, virão da API /api/v1/controls/summary.
 */
const mockControls: ControlsSummaryResponse = {
  overallCoverage: 78.5,
  overallMaturity: 'Defined',
  totalDimensions: 7,
  criticalGapCount: 1,
  dimensions: [
    {
      dimension: 'ContractGovernance', coveragePercent: 83.3, totalAssessed: 42, gapCount: 7,
      maturity: 'Defined', trend: 'Improving',
      summary: 'Contract governance coverage is strong but 7 services lack proper contract definitions',
    },
    {
      dimension: 'SourceOfTruthCompleteness', coveragePercent: 72.5, totalAssessed: 42, gapCount: 12,
      maturity: 'Developing', trend: 'Improving',
      summary: 'Source of truth completeness improving; documentation and runbook gaps remain',
    },
    {
      dimension: 'ChangeGovernance', coveragePercent: 91.2, totalAssessed: 156, gapCount: 3,
      maturity: 'Managed', trend: 'Stable',
      summary: 'Change governance is mature with high validation coverage',
    },
    {
      dimension: 'IncidentMitigationEvidence', coveragePercent: 67.8, totalAssessed: 34, gapCount: 8,
      maturity: 'Developing', trend: 'Improving',
      summary: 'Mitigation evidence coverage growing but post-mortem discipline needs improvement',
    },
    {
      dimension: 'AiGovernance', coveragePercent: 88.5, totalAssessed: 89, gapCount: 2,
      maturity: 'Defined', trend: 'Stable',
      summary: 'AI governance well-controlled through model registry and access policies',
    },
    {
      dimension: 'DocumentationRunbookReadiness', coveragePercent: 55.4, totalAssessed: 42, gapCount: 19,
      maturity: 'Developing', trend: 'Declining',
      summary: 'Documentation and runbook coverage is below target; critical gap area',
    },
    {
      dimension: 'OwnershipCoverage', coveragePercent: 90.5, totalAssessed: 42, gapCount: 4,
      maturity: 'Managed', trend: 'Stable',
      summary: 'Ownership coverage is high with only 4 unassigned services',
    },
  ],
  generatedAt: new Date().toISOString(),
};

const dimensionIcon = (dim: ControlDimensionType) => {
  switch (dim) {
    case 'ContractGovernance': return <FileText size={18} />;
    case 'SourceOfTruthCompleteness': return <Globe size={18} />;
    case 'ChangeGovernance': return <Zap size={18} />;
    case 'IncidentMitigationEvidence': return <AlertTriangle size={18} />;
    case 'AiGovernance': return <Bot size={18} />;
    case 'DocumentationRunbookReadiness': return <BookOpen size={18} />;
    case 'OwnershipCoverage': return <Users size={18} />;
    default: return <Layers size={18} />;
  }
};

const trendIcon = (trend: GovernanceTrendDirection) => {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    case 'Stable': return <Minus size={14} className="text-muted" />;
    default: return null;
  }
};

const maturityBadge = (mat: MaturityLevelType): 'success' | 'info' | 'warning' | 'default' => {
  switch (mat) {
    case 'Optimizing': case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'default';
    default: return 'default';
  }
};

const coverageColor = (pct: number): string => {
  if (pct >= 80) return 'bg-emerald-500';
  if (pct >= 60) return 'bg-amber-500';
  return 'bg-critical';
};

/**
 * Página de Enterprise Controls — visão consolidada de controles enterprise por dimensão.
 * Parte do módulo Governance do NexTraceOne.
 */
export function EnterpriseControlsPage() {
  const { t } = useTranslation();
  const d = mockControls;

  const overallColor = d.overallCoverage >= 80 ? 'text-success' : d.overallCoverage >= 60 ? 'text-amber-400' : 'text-critical';

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.controls.title')}</h1>
        <p className="text-muted mt-1">{t('governance.controls.subtitle')}</p>
      </div>

      {/* Overall Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center">
          <p className="text-xs text-muted mb-1">{t('governance.controls.overallCoverage')}</p>
          <p className={`text-4xl font-bold ${overallColor}`}>{d.overallCoverage}%</p>
        </div>
        <StatCard title={t('governance.controls.overallMaturity')} value={t(`governance.maturity.${d.overallMaturity}`)} icon={<ShieldCheck size={20} />} color="text-accent" />
        <StatCard title={t('governance.controls.totalDimensions')} value={d.totalDimensions} icon={<Layers size={20} />} color="text-accent" />
        <StatCard title={t('governance.controls.criticalGaps')} value={d.criticalGapCount} icon={<AlertTriangle size={20} />} color="text-critical" />
      </div>

      {/* Dimension Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 mb-6">
        {d.dimensions.map(dim => (
          <Card key={dim.dimension}>
            <CardBody>
              <div className="flex items-start gap-3">
                <div className={`mt-0.5 ${dim.coveragePercent >= 80 ? 'text-success' : dim.coveragePercent >= 60 ? 'text-amber-400' : 'text-critical'}`}>
                  {dimensionIcon(dim.dimension)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="text-sm font-semibold text-heading">
                      {t(`governance.controls.dimension.${dim.dimension}`)}
                    </h3>
                    {trendIcon(dim.trend)}
                  </div>
                  <div className="flex items-center gap-2 mb-2">
                    <Badge variant={maturityBadge(dim.maturity)}>{t(`governance.maturity.${dim.maturity}`)}</Badge>
                    <span className="text-xs text-faded">{t(`governance.trend.${dim.trend}`)}</span>
                  </div>
                  {/* Coverage bar */}
                  <div className="mb-2">
                    <div className="flex items-center justify-between mb-1">
                      <p className="text-xs text-muted">{t('governance.controls.coverage')}</p>
                      <p className="text-xs font-medium text-heading">{dim.coveragePercent}%</p>
                    </div>
                    <div className="w-full bg-surface rounded-full h-2">
                      <div className={`${coverageColor(dim.coveragePercent)} rounded-full h-2 transition-all`} style={{ width: `${dim.coveragePercent}%` }} />
                    </div>
                  </div>
                  <div className="flex items-center gap-3 text-xs text-faded">
                    <span>{t('governance.controls.assessed')}: {dim.totalAssessed}</span>
                    {dim.gapCount > 0 && (
                      <span className="text-critical">{t('governance.controls.gaps')}: {dim.gapCount}</span>
                    )}
                  </div>
                  <p className="text-xs text-muted mt-2">{dim.summary}</p>
                </div>
              </div>
            </CardBody>
          </Card>
        ))}
      </div>

      {/* Coverage Overview Table */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Layers size={16} className="text-accent" />
            {t('governance.controls.coverageOverview')}
          </h2>
          <p className="text-xs text-muted mt-1">{t('governance.controls.coverageOverviewDescription')}</p>
        </CardHeader>
        <CardBody className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge">
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted">{t('governance.controls.dimensionColumn')}</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted">{t('governance.controls.coverageColumn')}</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted">{t('governance.controls.maturityColumn')}</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted">{t('governance.controls.trendColumn')}</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted">{t('governance.controls.gapsColumn')}</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted">{t('governance.controls.assessedColumn')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {d.dimensions.map(dim => (
                  <tr key={dim.dimension} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className="text-muted">{dimensionIcon(dim.dimension)}</span>
                        <span className="text-body font-medium">{t(`governance.controls.dimension.${dim.dimension}`)}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <div className="w-16 bg-surface rounded-full h-1.5">
                          <div className={`${coverageColor(dim.coveragePercent)} rounded-full h-1.5`} style={{ width: `${dim.coveragePercent}%` }} />
                        </div>
                        <span className="text-xs text-heading font-medium">{dim.coveragePercent}%</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={maturityBadge(dim.maturity)}>{t(`governance.maturity.${dim.maturity}`)}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        {trendIcon(dim.trend)}
                        <span className="text-xs text-muted">{t(`governance.trend.${dim.trend}`)}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`text-xs font-medium ${dim.gapCount > 0 ? 'text-critical' : 'text-success'}`}>{dim.gapCount}</span>
                    </td>
                    <td className="px-4 py-3 text-xs text-muted">{dim.totalAssessed}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
