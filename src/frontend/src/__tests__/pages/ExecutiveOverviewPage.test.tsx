import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getExecutiveOverview: vi.fn().mockResolvedValue({
      operationalState: 'Healthy', riskLevel: 'Low', maturityScore: 78,
      changesLast30Days: 42, openIncidents: 3, isSimulated: false,
      operationalTrend: { stabilityTrend: 'Stable', incidentRateChange: -5, avgResolutionHours: 4 },
      riskSummary: { overallRisk: 'Low', criticalDomains: 0, highRiskServices: 1, riskTrend: 'Improving' },
      maturitySummary: { ownershipCoverage: 80, contractCoverage: 70, documentationCoverage: 60, runbookCoverage: 50 },
      changeSafetySummary: { safeChanges: 30, riskyChanges: 5, rollbacks: 2, confidenceTrend: 'Stable' },
      incidentTrendSummary: { openIncidents: 3, resolvedLast30Days: 10, avgResolutionHours: 4, recurrenceRate: 5, trend: 'Improving' },
      criticalFocusAreas: [],
      topDomainsRequiringAttention: [],
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { ExecutiveOverviewPage } from '../../features/governance/pages/ExecutiveOverviewPage';

function createWrapper() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));
describe('ExecutiveOverviewPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    render(<ExecutiveOverviewPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText('governance.executive.overviewTitle')).toBeInTheDocument();
    });
  });

  it('renders executive data after loading', async () => {
    render(<ExecutiveOverviewPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText('governance.executive.overviewTitle')).toBeInTheDocument();
    });
  });
});
