import { useTranslation } from 'react-i18next';
import { useQueries } from '@tanstack/react-query';
import { LayoutDashboard } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { ServiceHealthSummaryCard } from '../components/executive/ServiceHealthSummaryCard';
import { ChangeConfidenceGauge } from '../components/executive/ChangeConfidenceGauge';
import { ComplianceCoverageWidget } from '../components/executive/ComplianceCoverageWidget';
import { FinOpsBudgetBurnWidget } from '../components/executive/FinOpsBudgetBurnWidget';
import { TopRiskyServicesTable } from '../components/executive/TopRiskyServicesTable';
import { MttrTrendMiniChart } from '../components/executive/MttrTrendMiniChart';
import { executiveIntelligenceApi } from '../api/executiveIntelligence';

/**
 * ExecutiveIntelligenceDashboardPage — Dashboard unificado para persona Executive/CTO
 * com KPIs consolidados de todas as dimensões operacionais.
 *
 * Responde a "o produto está a degradar ou a melhorar?" sem precisar navegar módulo a módulo.
 * Os 6 widgets cobrem: saúde dos serviços, confiança em mudanças, compliance, FinOps,
 * serviços de maior risco e tendência de MTTR.
 *
 * @see docs/FUTURE-ROADMAP.md Wave X.1
 * @see src/frontend/src/auth/persona.ts — persona Executive
 */
export function ExecutiveIntelligenceDashboardPage() {
  const { t } = useTranslation();

  const results = useQueries({
    queries: [
      {
        queryKey: ['executive-intelligence', 'service-health'],
        queryFn: executiveIntelligenceApi.getServiceHealthSummary,
        staleTime: 60_000,
      },
      {
        queryKey: ['executive-intelligence', 'change-confidence'],
        queryFn: executiveIntelligenceApi.getChangeConfidenceGauge,
        staleTime: 60_000,
      },
      {
        queryKey: ['executive-intelligence', 'compliance-coverage'],
        queryFn: executiveIntelligenceApi.getComplianceCoverageWidget,
        staleTime: 60_000,
      },
      {
        queryKey: ['executive-intelligence', 'finops-burn'],
        queryFn: executiveIntelligenceApi.getFinOpsBudgetBurnWidget,
        staleTime: 60_000,
      },
      {
        queryKey: ['executive-intelligence', 'top-risky-services'],
        queryFn: executiveIntelligenceApi.getTopRiskyServicesWidget,
        staleTime: 60_000,
      },
      {
        queryKey: ['executive-intelligence', 'mttr-trend'],
        queryFn: executiveIntelligenceApi.getMttrTrendWidget,
        staleTime: 60_000,
      },
    ],
  });

  const [healthQ, confidenceQ, complianceQ, finopsQ, riskyQ, mttrQ] = results;

  const isLoading = results.some((r) => r.isLoading);
  const isError = results.some((r) => r.isError);

  if (isLoading) {
    return <PageLoadingState message={t('executiveDashboard.loadingWidgets')} />;
  }

  if (isError) {
    return (
      <PageErrorState
        message={t('executiveDashboard.errorLoading')}
        onRetry={() => results.forEach((r) => r.refetch())}
      />
    );
  }

  return (
    <PageContainer>
      <PageHeader
        icon={<LayoutDashboard className="h-6 w-6 text-blue-500" />}
        title={t('executiveDashboard.title')}
        description={t('executiveDashboard.serviceHealthDesc')}
      />

      <PageSection>
        {/* Top row: Health + Confidence + FinOps */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
          {healthQ.data && <ServiceHealthSummaryCard data={healthQ.data} />}
          {confidenceQ.data && <ChangeConfidenceGauge data={confidenceQ.data} />}
          {finopsQ.data && <FinOpsBudgetBurnWidget data={finopsQ.data} />}
        </div>

        {/* Bottom row: Compliance + Top Risky Services + MTTR */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {complianceQ.data && <ComplianceCoverageWidget data={complianceQ.data} />}
          {riskyQ.data && <TopRiskyServicesTable data={riskyQ.data} />}
          {mttrQ.data && <MttrTrendMiniChart data={mttrQ.data} />}
        </div>
      </PageSection>
    </PageContainer>
  );
}
