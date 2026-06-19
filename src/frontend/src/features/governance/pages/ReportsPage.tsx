import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Award, FileText, AlertTriangle,
  ShieldCheck, TrendingUp, TrendingDown, Minus,
  Activity, ClipboardCheck, Download, Package,
  CheckCircle2, XCircle, Clock,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { queryKeys } from '../../../shared/api/queryKeys';
import { organizationGovernanceApi } from '../api/organizationGovernance';

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
 * Página de Reports — relatórios de governança segmentados por persona.
 * Dados reais derivados de Governance Packs, Rollouts e Waivers.
 * Parte do módulo Governance do NexTraceOne.
 */
export function ReportsPage() {
  const { t } = useTranslation();
  const { persona } = usePersona();
  const { activeEnvironmentId } = useEnvironment();
  const personaKeyMap: Record<string, string> = {
    Engineer: 'engineer', TechLead: 'techLead', Architect: 'architect',
    Product: 'product', Executive: 'executive', PlatformAdmin: 'platformAdmin', Auditor: 'auditor',
  };
  const personaKey = personaKeyMap[persona] ?? 'engineer';
  const personaFocusKey = `governance.reports.${personaKey}.focus`;

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.governance.reports(activeEnvironmentId),
    queryFn: () => organizationGovernanceApi.getReportsSummary(),
  });

  if (isLoading) {
    return <PageContainer><PageLoadingState /></PageContainer>;
  }

  if (isError || !data) {
    return <PageContainer><PageErrorState /></PageContainer>;
  }

  const rolloutCompletionPct = data.totalRollouts === 0 ? 0 : Math.round((data.completedRollouts / data.totalRollouts) * 100);
  const packPublishedPct = data.totalPacks === 0 ? 0 : Math.round((data.publishedPacks / data.totalPacks) * 100);

  return (
    <PageContainer>
      {/* Cabeçalho com badge de persona e CTA de exportação */}
      <PageHeader
        title={t('governance.reportsTitle')}
        subtitle={t('governance.reportsSubtitle')}
        badge={
          <Badge variant="info">
            {t('governance.reports.personaView', { persona })}
          </Badge>
        }
        actions={
          <Button variant="primary" icon={<Download size={14} />}>
            {t('governance.reports.export')}
          </Button>
        }
      />

      {/* Cobertura de Governance Packs — KPIs principais */}
      <PageSection
        title={t('governance.reports.packCoverage')}
        icon={<Package size={16} />}
      >
        <ContentGrid columns={6}>
          <StatCard title={t('governance.reports.totalPacks')} value={data.totalPacks} icon={<Package size={20} />} color="text-accent" />
          <StatCard title={t('governance.reports.publishedPacks')} value={`${packPublishedPct}%`} icon={<CheckCircle2 size={20} />} color="text-success" />
          <StatCard title={t('governance.reports.completedRollouts')} value={`${rolloutCompletionPct}%`} icon={<CheckCircle2 size={20} />} color="text-success" />
          <StatCard title={t('governance.reports.pendingRollouts')} value={data.pendingRollouts} icon={<Clock size={20} />} color="text-warning" />
          <StatCard title={t('governance.reports.failedRollouts')} value={data.failedRollouts} icon={<XCircle size={20} />} color="text-critical" />
          <StatCard title={t('governance.reports.complianceScore')} value={`${data.complianceScore}%`} icon={<ShieldCheck size={20} />} color="text-success" />
        </ContentGrid>
      </PageSection>

      {/* Tendências e indicadores */}
      <PageSection
        title={t('governance.reports.trends')}
        icon={<Activity size={16} />}
      >
        <ContentGrid columns={4}>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.changeConfidenceTrend')}</p>
              <div className="flex items-center gap-2">
                {trendIcon(data.changeConfidenceTrend)}
                <Badge variant={trendBadgeVariant(data.changeConfidenceTrend)}>
                  {t(`governance.trend.${data.changeConfidenceTrend}`)}
                </Badge>
              </div>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.riskLevel')}</p>
              <Badge variant={riskBadgeVariant(data.overallRiskLevel)}>
                {t(`governance.risk.level.${data.overallRiskLevel}`)}
              </Badge>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.maturityLevel')}</p>
              <Badge variant="info">
                {t(`governance.maturity.${data.overallMaturity}`)}
              </Badge>
            </CardBody>
          </Card>
          <Card>
            <CardBody>
              <p className="text-xs text-muted mb-1">{t('governance.reports.pendingWaivers')}</p>
              <span className={`text-lg font-bold ${data.pendingWaivers > 0 ? 'text-warning' : 'text-success'}`}>
                {data.pendingWaivers}
              </span>
            </CardBody>
          </Card>
        </ContentGrid>
      </PageSection>

      {/* Cobertura de Rollouts — barras de progresso */}
      <PageSection>
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Award size={16} className="text-accent" />
              {t('governance.reports.rolloutCoverage')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-6">
              {[
                { label: t('governance.reports.publishedPacks'), count: data.publishedPacks, total: data.totalPacks },
                { label: t('governance.reports.packsWithRollout'), count: data.packsWithRollout, total: data.totalPacks },
                { label: t('governance.reports.packsWithCompletedRollout'), count: data.packsWithCompletedRollout, total: data.totalPacks },
              ].map((item) => (
                <div key={item.label}>
                  <p className="text-xs text-muted mb-1">{item.label}</p>
                  <div className="w-full bg-card rounded-full h-2">
                    <div
                      className="bg-accent rounded-full h-2 transition-all"
                      style={{ width: item.total === 0 ? '0%' : `${Math.round((item.count / item.total) * 100)}%` }}
                    />
                  </div>
                  <p className="text-xs text-muted mt-1">
                    {item.count}/{item.total} ({item.total === 0 ? 0 : Math.round((item.count / item.total) * 100)}%)
                  </p>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Insights filtrados pela persona activa */}
      <PageSection>
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
              {(persona === 'Engineer' || persona === 'TechLead') && (
                <div className="py-3">
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <StatCard title={t('governance.reports.totalPacks')} value={data.totalPacks} icon={<Package size={18} />} color="text-accent" />
                    <StatCard title={t('governance.reports.pendingRollouts')} value={data.pendingRollouts} icon={<Clock size={18} />} color="text-warning" />
                    <StatCard title={t('governance.reports.failedRollouts')} value={data.failedRollouts} icon={<XCircle size={18} />} color="text-critical" />
                  </div>
                </div>
              )}
              {(persona === 'Architect' || persona === 'PlatformAdmin') && (
                <div className="py-3">
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <StatCard title={t('governance.reports.publishedPacks')} value={data.publishedPacks} icon={<CheckCircle2 size={18} />} color="text-success" />
                    <StatCard title={t('governance.reports.complianceScore')} value={`${data.complianceScore}%`} icon={<ShieldCheck size={18} />} color="text-success" />
                    <StatCard title={t('governance.reports.riskLevel')} value={t(`governance.risk.level.${data.overallRiskLevel}`)} icon={<AlertTriangle size={18} />} color="text-warning" />
                  </div>
                </div>
              )}
              {(persona === 'Executive' || persona === 'Product') && (
                <div className="py-3">
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <StatCard title={t('governance.reports.complianceScore')} value={`${data.complianceScore}%`} icon={<ShieldCheck size={18} />} color="text-success" />
                    <StatCard title={t('governance.reports.riskLevel')} value={t(`governance.risk.level.${data.overallRiskLevel}`)} icon={<AlertTriangle size={18} />} color="text-warning" />
                    <StatCard title={t('governance.reports.maturityLevel')} value={t(`governance.maturity.${data.overallMaturity}`)} icon={<Award size={18} />} color="text-accent" />
                  </div>
                </div>
              )}
              {persona === 'Auditor' && (
                <div className="py-3">
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    <StatCard title={t('governance.reports.complianceScore')} value={`${data.complianceScore}%`} icon={<ShieldCheck size={18} />} color="text-success" />
                    <StatCard title={t('governance.reports.totalWaivers')} value={data.totalWaivers} icon={<FileText size={18} />} color="text-info" />
                    <StatCard title={t('governance.reports.pendingWaivers')} value={data.pendingWaivers} icon={<AlertTriangle size={18} />} color="text-warning" />
                  </div>
                </div>
              )}
            </div>
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
