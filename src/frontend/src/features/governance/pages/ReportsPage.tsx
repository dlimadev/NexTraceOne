import { useTranslation } from 'react-i18next';
import {
  BarChart3, Users, FileText, AlertTriangle, Zap,
  ShieldCheck, TrendingUp, TrendingDown, Minus,
  Activity, Server, Scale, ClipboardCheck, Download,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { usePersona } from '../../../contexts/PersonaContext';
import type { ReportsSummaryResponse } from '../../../types';

/**
 * Dados simulados de relatório executivo — alinhados com o backend GetReportsSummary.
 * Em produção, virão da API /api/v1/governance/reports/summary.
 */
const mockReport: ReportsSummaryResponse = {
  totalServices: 42,
  servicesWithOwner: 38,
  servicesWithContract: 31,
  servicesWithDocumentation: 27,
  servicesWithRunbook: 19,
  overallRiskLevel: 'Medium',
  overallMaturity: 'Defined',
  changeConfidenceTrend: 'Improving',
  reliabilityTrend: 'Stable',
  openIncidents: 5,
  recentChanges: 18,
  complianceScore: 74,
  costEfficiency: 'Acceptable',
  generatedAt: new Date().toISOString(),
};

const trendIcon = (dir: string) => {
  switch (dir) {
    case 'Improving': return <TrendingUp size={16} className="text-success" />;
    case 'Declining': return <TrendingDown size={16} className="text-critical" />;
    default: return <Minus size={16} className="text-muted" />;
  }
};

const trendBadgeVariant = (dir: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (dir) {
    case 'Improving': return 'success';
    case 'Declining': return 'danger';
    default: return 'default';
  }
};

const riskBadgeVariant = (level: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (level) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'warning';
    case 'Low': return 'success';
    default: return 'default';
  }
};

/**
 * Página de Reports — relatórios segmentados por persona e contexto operacional.
 * Parte do módulo Governance do NexTraceOne.
 */
export function ReportsPage() {
  const { t } = useTranslation();
  const { persona } = usePersona();
  const d = mockReport;

  const ownerPct = Math.round((d.servicesWithOwner / d.totalServices) * 100);
  const contractPct = Math.round((d.servicesWithContract / d.totalServices) * 100);

  const personaKeyMap: Record<string, string> = {
    Engineer: 'engineer', TechLead: 'techLead', Architect: 'architect',
    Product: 'product', Executive: 'executive', PlatformAdmin: 'platformAdmin', Auditor: 'auditor',
  };
  const personaKey = personaKeyMap[persona] ?? 'engineer';
  const personaFocusKey = `governance.reports.${personaKey}.focus`;

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="governance" />

      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.reportsTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.reportsSubtitle')}</p>
        <Badge variant="info" className="mt-2">
          {t('governance.reports.personaView', { persona })}
        </Badge>
      </div>

      {/* Executive summary stats */}
      <div className="mb-6">
        <h2 className="text-sm font-semibold text-heading mb-3 flex items-center gap-2">
          <BarChart3 size={16} className="text-accent" />
          {t('governance.reports.executiveSummary')}
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
          <StatCard title={t('governance.reports.totalServices')} value={d.totalServices} icon={<Server size={20} />} color="text-accent" />
          <StatCard title={t('governance.reports.ownerCoverage')} value={`${ownerPct}%`} icon={<Users size={20} />} color="text-blue-500" />
          <StatCard title={t('governance.reports.contractCoverage')} value={`${contractPct}%`} icon={<FileText size={20} />} color="text-emerald-500" />
          <StatCard title={t('governance.reports.openIncidents')} value={d.openIncidents} icon={<AlertTriangle size={20} />} color="text-red-500" />
          <StatCard title={t('governance.reports.recentChanges')} value={d.recentChanges} icon={<Zap size={20} />} color="text-amber-500" />
          <StatCard title={t('governance.reports.complianceScore')} value={`${d.complianceScore}%`} icon={<ShieldCheck size={20} />} color="text-emerald-500" />
        </div>
      </div>

      {/* Trends & Indicators */}
      <div className="mb-6">
        <h2 className="text-sm font-semibold text-heading mb-3 flex items-center gap-2">
          <Activity size={16} className="text-accent" />
          {t('governance.reports.trends')}
        </h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.changeConfidenceTrend')}</p>
              <div className="flex items-center gap-2">
                {trendIcon(d.changeConfidenceTrend)}
                <Badge variant={trendBadgeVariant(d.changeConfidenceTrend)}>
                  {t(`governance.trend.${d.changeConfidenceTrend}`)}
                </Badge>
              </div>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.reliabilityTrend')}</p>
              <div className="flex items-center gap-2">
                {trendIcon(d.reliabilityTrend)}
                <Badge variant={trendBadgeVariant(d.reliabilityTrend)}>
                  {t(`governance.trend.${d.reliabilityTrend}`)}
                </Badge>
              </div>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.riskLevel')}</p>
              <Badge variant={riskBadgeVariant(d.overallRiskLevel)}>
                {t(`governance.risk.level.${d.overallRiskLevel}`)}
              </Badge>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.maturityLevel')}</p>
              <Badge variant="info">
                {t(`governance.maturity.${d.overallMaturity}`)}
              </Badge>
            </CardBody>
          </Card>
        </div>
      </div>

      {/* Coverage Indicators */}
      <div className="mb-6">
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Scale size={16} className="text-accent" />
              {t('governance.reports.coverageSection')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {[
                { label: t('governance.reports.ownerCoverage'), count: d.servicesWithOwner },
                { label: t('governance.reports.contractCoverage'), count: d.servicesWithContract },
                { label: t('governance.reports.docsAndRunbooks'), count: d.servicesWithDocumentation },
              ].map((item) => (
                <div key={item.label}>
                  <p className="text-xs text-muted mb-1">{item.label}</p>
                  <div className="w-full bg-surface rounded-full h-2">
                    <div
                      className="bg-accent rounded-full h-2 transition-all"
                      style={{ width: `${Math.round((item.count / d.totalServices) * 100)}%` }}
                    />
                  </div>
                  <p className="text-xs text-muted mt-1">{item.count}/{d.totalServices} ({Math.round((item.count / d.totalServices) * 100)}%)</p>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Persona-specific insights */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <ClipboardCheck size={16} className="text-accent" />
            {t('governance.reports.personaInsights', { persona })}
          </h2>
        </CardHeader>
        <CardBody>
          <p className="text-sm text-muted mb-4">{t(personaFocusKey)}</p>
          <div className="divide-y divide-edge">
            {persona === 'Engineer' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.totalServices')} value={d.totalServices} icon={<Server size={18} />} color="text-accent" />
                  <StatCard title={t('governance.reports.openIncidents')} value={d.openIncidents} icon={<AlertTriangle size={18} />} color="text-red-500" />
                  <StatCard title={t('governance.reports.recentChanges')} value={d.recentChanges} icon={<Zap size={18} />} color="text-amber-500" />
                </div>
              </div>
            )}
            {persona === 'TechLead' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.ownerCoverage')} value={`${ownerPct}%`} icon={<Users size={18} />} color="text-blue-500" />
                  <StatCard title={t('governance.reports.complianceScore')} value={`${d.complianceScore}%`} icon={<ShieldCheck size={18} />} color="text-emerald-500" />
                  <StatCard title={t('governance.reports.openIncidents')} value={d.openIncidents} icon={<AlertTriangle size={18} />} color="text-red-500" />
                </div>
              </div>
            )}
            {persona === 'Architect' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.contractCoverage')} value={`${contractPct}%`} icon={<FileText size={18} />} color="text-emerald-500" />
                  <StatCard title={t('governance.reports.riskLevel')} value={t(`governance.risk.level.${d.overallRiskLevel}`)} icon={<AlertTriangle size={18} />} color="text-amber-500" />
                  <StatCard title={t('governance.reports.totalServices')} value={d.totalServices} icon={<Server size={18} />} color="text-accent" />
                </div>
              </div>
            )}
            {persona === 'Product' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.changeConfidenceTrend')} value={t(`governance.trend.${d.changeConfidenceTrend}`)} icon={<TrendingUp size={18} />} color="text-emerald-500" />
                  <StatCard title={t('governance.reports.reliabilityTrend')} value={t(`governance.trend.${d.reliabilityTrend}`)} icon={<Activity size={18} />} color="text-blue-500" />
                  <StatCard title={t('governance.reports.openIncidents')} value={d.openIncidents} icon={<AlertTriangle size={18} />} color="text-red-500" />
                </div>
              </div>
            )}
            {persona === 'Executive' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.complianceScore')} value={`${d.complianceScore}%`} icon={<ShieldCheck size={18} />} color="text-emerald-500" />
                  <StatCard title={t('governance.reports.riskLevel')} value={t(`governance.risk.level.${d.overallRiskLevel}`)} icon={<AlertTriangle size={18} />} color="text-amber-500" />
                  <StatCard title={t('governance.reports.maturityLevel')} value={t(`governance.maturity.${d.overallMaturity}`)} icon={<BarChart3 size={18} />} color="text-accent" />
                </div>
              </div>
            )}
            {persona === 'PlatformAdmin' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.ownerCoverage')} value={`${ownerPct}%`} icon={<Users size={18} />} color="text-blue-500" />
                  <StatCard title={t('governance.reports.contractCoverage')} value={`${contractPct}%`} icon={<FileText size={18} />} color="text-emerald-500" />
                  <StatCard title={t('governance.reports.totalServices')} value={d.totalServices} icon={<Server size={18} />} color="text-accent" />
                </div>
              </div>
            )}
            {persona === 'Auditor' && (
              <div className="py-3">
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <StatCard title={t('governance.reports.complianceScore')} value={`${d.complianceScore}%`} icon={<ShieldCheck size={18} />} color="text-emerald-500" />
                  <StatCard title={t('governance.reports.docsAndRunbooks')} value={`${d.servicesWithRunbook}/${d.totalServices}`} icon={<FileText size={18} />} color="text-blue-500" />
                  <StatCard title={t('governance.reports.riskLevel')} value={t(`governance.risk.level.${d.overallRiskLevel}`)} icon={<AlertTriangle size={18} />} color="text-amber-500" />
                </div>
              </div>
            )}
          </div>
          <div className="mt-4 flex justify-end">
            <button className="flex items-center gap-2 px-4 py-2 text-sm rounded-md bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors">
              <Download size={14} />
              {t('governance.reports.export')}
            </button>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
