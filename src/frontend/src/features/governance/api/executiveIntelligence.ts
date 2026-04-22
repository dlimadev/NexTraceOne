import client from '../../../api/client';

/** Resposta do widget de saúde dos serviços. */
export interface ServiceHealthSummaryResponse {
  overallScore: number;
  sloComplianceScore: number;
  riskScore: number;
  ownershipHealthScore: number;
  deploymentSuccessRate: number;
  trend: 'Improving' | 'Stable' | 'Declining';
  criticalServicesCount: number;
  isSimulated: boolean;
}

/** Resposta do widget de confiança em mudanças. */
export interface ChangeConfidenceGaugeResponse {
  averageConfidencePct: number;
  byTier: Array<{ tier: string; confidencePct: number; serviceCount: number }>;
  trendDirection: 'Improving' | 'Stable' | 'Declining';
  periodLabel: string;
  isSimulated: boolean;
}

/** Resposta do widget de cobertura de compliance. */
export interface ComplianceCoverageWidgetResponse {
  standards: Array<{ name: string; coveragePct: number; serviceCoveredCount: number; totalServices: number }>;
  overallCoveragePct: number;
  isSimulated: boolean;
}

/** Resposta do widget de budget FinOps. */
export interface FinOpsBudgetBurnWidgetResponse {
  budgetConsumedPct: number;
  totalBudget: number;
  totalSpent: number;
  burnAccelerated: boolean;
  burnRate: number;
  periodLabel: string;
  isSimulated: boolean;
}

/** Resposta do widget de top serviços de risco. */
export interface TopRiskyServicesWidgetResponse {
  services: Array<{
    serviceId: string;
    serviceName: string;
    domain: string;
    riskScore: number;
    riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
    topRiskDimension: string;
  }>;
  isSimulated: boolean;
}

/** Resposta do widget de tendência de MTTR. */
export interface MttrTrendWidgetResponse {
  services: Array<{
    serviceId: string;
    serviceName: string;
    sparkline: Array<{ date: string; mttrHours: number }>;
    currentMttrHours: number;
    trend: 'Improving' | 'Stable' | 'Worsening';
  }>;
  isSimulated: boolean;
}

/** Cliente de API para o Executive Intelligence Dashboard (Wave X.1). */
export const executiveIntelligenceApi = {
  getServiceHealthSummary: () =>
    client.get<ServiceHealthSummaryResponse>('/executive/intelligence/service-health').then((r) => r.data),

  getChangeConfidenceGauge: () =>
    client.get<ChangeConfidenceGaugeResponse>('/executive/intelligence/change-confidence').then((r) => r.data),

  getComplianceCoverageWidget: () =>
    client.get<ComplianceCoverageWidgetResponse>('/executive/intelligence/compliance-coverage').then((r) => r.data),

  getFinOpsBudgetBurnWidget: () =>
    client.get<FinOpsBudgetBurnWidgetResponse>('/executive/intelligence/finops-burn').then((r) => r.data),

  getTopRiskyServicesWidget: () =>
    client.get<TopRiskyServicesWidgetResponse>('/executive/intelligence/top-risky-services').then((r) => r.data),

  getMttrTrendWidget: () =>
    client.get<MttrTrendWidgetResponse>('/executive/intelligence/mttr-trend').then((r) => r.data),
};
