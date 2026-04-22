import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/executiveIntelligence', () => ({
  executiveIntelligenceApi: {
    getServiceHealthSummary: vi.fn(),
    getChangeConfidenceGauge: vi.fn(),
    getComplianceCoverageWidget: vi.fn(),
    getFinOpsBudgetBurnWidget: vi.fn(),
    getTopRiskyServicesWidget: vi.fn(),
    getMttrTrendWidget: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    environments: [],
    setActiveEnvironmentId: vi.fn(),
  }),
}));

import { executiveIntelligenceApi } from '../../features/governance/api/executiveIntelligence';
import { ExecutiveIntelligenceDashboardPage } from '../../features/governance/pages/ExecutiveIntelligenceDashboardPage';

const makeHealthData = () => ({
  overallScore: 82, sloComplianceScore: 88, riskScore: 76, ownershipHealthScore: 85,
  deploymentSuccessRate: 79, trend: 'Stable' as const, criticalServicesCount: 0, isSimulated: false,
});
const makeConfidenceData = () => ({
  averageConfidencePct: 74, byTier: [{ tier: 'Critical', confidencePct: 68, serviceCount: 5 }],
  trendDirection: 'Improving' as const, periodLabel: 'last_4w', isSimulated: false,
});
const makeComplianceData = () => ({
  standards: [{ name: 'SOC2', coveragePct: 90, serviceCoveredCount: 18, totalServices: 20 }],
  overallCoveragePct: 85, isSimulated: false,
});
const makeFinOpsData = () => ({
  budgetConsumedPct: 62, totalBudget: 100000, totalSpent: 62000,
  burnAccelerated: false, burnRate: 2100, periodLabel: 'June 2025', isSimulated: false,
});
const makeRiskyData = () => ({
  services: [{ serviceId: 's1', serviceName: 'PaymentService', domain: 'Finance', riskScore: 88, riskLevel: 'High' as const, topRiskDimension: 'Contract' }],
  isSimulated: false,
});
const makeMttrData = () => ({
  services: [{ serviceId: 's1', serviceName: 'PaymentService', sparkline: [{ date: '2025-06-01', mttrHours: 4 }, { date: '2025-06-02', mttrHours: 3 }], currentMttrHours: 3.1, trend: 'Improving' as const }],
  isSimulated: false,
});

function createWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}><MemoryRouter>{children}</MemoryRouter></QueryClientProvider>
  );
}

describe('ExecutiveIntelligenceDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while queries are in flight', () => {
    vi.mocked(executiveIntelligenceApi.getServiceHealthSummary).mockReturnValue(new Promise(() => {}));
    vi.mocked(executiveIntelligenceApi.getChangeConfidenceGauge).mockReturnValue(new Promise(() => {}));
    vi.mocked(executiveIntelligenceApi.getComplianceCoverageWidget).mockReturnValue(new Promise(() => {}));
    vi.mocked(executiveIntelligenceApi.getFinOpsBudgetBurnWidget).mockReturnValue(new Promise(() => {}));
    vi.mocked(executiveIntelligenceApi.getTopRiskyServicesWidget).mockReturnValue(new Promise(() => {}));
    vi.mocked(executiveIntelligenceApi.getMttrTrendWidget).mockReturnValue(new Promise(() => {}));
    render(<ExecutiveIntelligenceDashboardPage />, { wrapper: createWrapper() });
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders all 6 widgets when data loads successfully', async () => {
    vi.mocked(executiveIntelligenceApi.getServiceHealthSummary).mockResolvedValue(makeHealthData());
    vi.mocked(executiveIntelligenceApi.getChangeConfidenceGauge).mockResolvedValue(makeConfidenceData());
    vi.mocked(executiveIntelligenceApi.getComplianceCoverageWidget).mockResolvedValue(makeComplianceData());
    vi.mocked(executiveIntelligenceApi.getFinOpsBudgetBurnWidget).mockResolvedValue(makeFinOpsData());
    vi.mocked(executiveIntelligenceApi.getTopRiskyServicesWidget).mockResolvedValue(makeRiskyData());
    vi.mocked(executiveIntelligenceApi.getMttrTrendWidget).mockResolvedValue(makeMttrData());
    render(<ExecutiveIntelligenceDashboardPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByTestId('service-health-summary-card')).toBeInTheDocument();
      expect(screen.getByTestId('change-confidence-gauge')).toBeInTheDocument();
      expect(screen.getByTestId('compliance-coverage-widget')).toBeInTheDocument();
      expect(screen.getByTestId('finops-budget-burn-widget')).toBeInTheDocument();
      expect(screen.getByTestId('top-risky-services-table')).toBeInTheDocument();
      expect(screen.getByTestId('mttr-trend-mini-chart')).toBeInTheDocument();
    });
  });

  it('shows error state when any query fails', async () => {
    vi.mocked(executiveIntelligenceApi.getServiceHealthSummary).mockRejectedValue(new Error('fail'));
    vi.mocked(executiveIntelligenceApi.getChangeConfidenceGauge).mockResolvedValue(makeConfidenceData());
    vi.mocked(executiveIntelligenceApi.getComplianceCoverageWidget).mockResolvedValue(makeComplianceData());
    vi.mocked(executiveIntelligenceApi.getFinOpsBudgetBurnWidget).mockResolvedValue(makeFinOpsData());
    vi.mocked(executiveIntelligenceApi.getTopRiskyServicesWidget).mockResolvedValue(makeRiskyData());
    vi.mocked(executiveIntelligenceApi.getMttrTrendWidget).mockResolvedValue(makeMttrData());
    render(<ExecutiveIntelligenceDashboardPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  it('shows burn accelerated badge when FinOps data has burnAccelerated=true', async () => {
    vi.mocked(executiveIntelligenceApi.getServiceHealthSummary).mockResolvedValue(makeHealthData());
    vi.mocked(executiveIntelligenceApi.getChangeConfidenceGauge).mockResolvedValue(makeConfidenceData());
    vi.mocked(executiveIntelligenceApi.getComplianceCoverageWidget).mockResolvedValue(makeComplianceData());
    vi.mocked(executiveIntelligenceApi.getFinOpsBudgetBurnWidget).mockResolvedValue({ ...makeFinOpsData(), burnAccelerated: true, budgetConsumedPct: 95 });
    vi.mocked(executiveIntelligenceApi.getTopRiskyServicesWidget).mockResolvedValue(makeRiskyData());
    vi.mocked(executiveIntelligenceApi.getMttrTrendWidget).mockResolvedValue(makeMttrData());
    render(<ExecutiveIntelligenceDashboardPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText('executiveDashboard.burnAccelerated')).toBeInTheDocument();
    });
  });

  it('shows risky service name in TopRiskyServicesTable', async () => {
    vi.mocked(executiveIntelligenceApi.getServiceHealthSummary).mockResolvedValue(makeHealthData());
    vi.mocked(executiveIntelligenceApi.getChangeConfidenceGauge).mockResolvedValue(makeConfidenceData());
    vi.mocked(executiveIntelligenceApi.getComplianceCoverageWidget).mockResolvedValue(makeComplianceData());
    vi.mocked(executiveIntelligenceApi.getFinOpsBudgetBurnWidget).mockResolvedValue(makeFinOpsData());
    vi.mocked(executiveIntelligenceApi.getTopRiskyServicesWidget).mockResolvedValue(makeRiskyData());
    vi.mocked(executiveIntelligenceApi.getMttrTrendWidget).mockResolvedValue(makeMttrData());
    render(<ExecutiveIntelligenceDashboardPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByTestId('top-risky-services-table')).toBeInTheDocument();
    });
    // PaymentService appears in the risky services table (and possibly MTTR too — use getAllByText)
    const instances = screen.getAllByText('PaymentService');
    expect(instances.length).toBeGreaterThanOrEqual(1);
  });
});
