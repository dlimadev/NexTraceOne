import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Layers, ShieldCheck, TrendingUp, TrendingDown, Minus, AlertTriangle,
  FileText, Globe, Zap, Bot, BookOpen, Users, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import type {
  ControlDimensionDto, ControlsSummaryResponse, ControlDimensionType,
  MaturityLevelType, GovernanceTrendDirection,
} from '../../../types';
import { organizationGovernanceApi } from '../api/organizationGovernance';

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
 * Dados reais derivados de Governance Packs, Rollouts e Waivers por categoria.
 * Parte do módulo Governance do NexTraceOne.
 */
export function EnterpriseControlsPage() {
  const { t } = useTranslation();
  const [data, setData] = useState<ControlsSummaryResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    organizationGovernanceApi.getControlsSummary()
      .then((d) => { if (!cancelled) { setData(d); setLoading(false); } })
      .catch((err) => { if (!cancelled) { setError(err.message || t('common.errorLoading')); setLoading(false); } });
    return () => { cancelled = true; };
  }, [t]);

  if (loading) {
    return (
      <PageContainer>
        <div className="flex items-center justify-center py-20">
          <Loader2 size={32} className="animate-spin text-accent" />
        </div>
      </PageContainer>
    );
  }

  if (error || !data) {
    return (
      <PageContainer>
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <AlertTriangle size={32} className="text-critical mb-2" />
          <p className="text-sm text-muted">{error ?? t('common.errorLoading')}</p>
        </div>
      </PageContainer>
    );
  }

  const overallColor = data.overallCoverage >= 80 ? 'text-success' : data.overallCoverage >= 60 ? 'text-amber-400' : 'text-critical';

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.controls.title')}</h1>
        <p className="text-muted mt-1">{t('governance.controls.subtitle')}</p>
      </div>

      {/* Overall Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center">
          <p className="text-xs text-muted mb-1">{t('governance.controls.overallCoverage')}</p>
          <p className={`text-4xl font-bold ${overallColor}`}>{data.overallCoverage}%</p>
        </div>
        <StatCard title={t('governance.controls.overallMaturity')} value={t(`governance.maturity.${data.overallMaturity}`)} icon={<ShieldCheck size={20} />} color="text-accent" />
        <StatCard title={t('governance.controls.totalDimensions')} value={data.totalDimensions} icon={<Layers size={20} />} color="text-accent" />
        <StatCard title={t('governance.controls.criticalGaps')} value={data.criticalGapCount} icon={<AlertTriangle size={20} />} color="text-critical" />
      </div>

      {/* Dimension Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 mb-6">
        {data.dimensions.map(dim => (
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
                {data.dimensions.map(dim => (
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
    </PageContainer>
  );
}
