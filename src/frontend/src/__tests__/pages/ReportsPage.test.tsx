import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getReportsSummary: vi.fn(),
  },
}));

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: () => ({ persona: 'Engineer', config: {} }),
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { ReportsPage } from '../../features/governance/pages/ReportsPage';

const mockData = {
  totalPacks: 10,
  publishedPacks: 7,
  completedRollouts: 5,
  totalRollouts: 8,
  pendingRollouts: 2,
  failedRollouts: 1,
  complianceScore: 85,
  changeConfidenceTrend: 'Improving',
  overallRiskLevel: 'Medium',
  overallMaturity: 'Defined',
  pendingWaivers: 3,
  packsWithRollout: 6,
  packsWithCompletedRollout: 4,
  totalWaivers: 15,
};


vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [
      { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
      { id: 'env-staging-001', name: 'Staging', profile: 'staging', isProductionLike: false },
    ],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('ReportsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getReportsSummary).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderWithProviders(<ReportsPage />);
    await waitFor(() => {
      expect(screen.getByText('governance.reportsTitle')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderWithProviders(<ReportsPage />);
    await waitFor(() => {
      expect(screen.getByText('governance.reports.packCoverage')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getReportsSummary).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<ReportsPage />);
    expect(screen.queryByText('governance.reports.packCoverage')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderWithProviders(<ReportsPage />);
    await waitFor(() => screen.getByText('governance.reportsTitle'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getReportsSummary on mount', async () => {
    renderWithProviders(<ReportsPage />);
    await waitFor(() => expect(organizationGovernanceApi.getReportsSummary).toHaveBeenCalledTimes(1));
  });
});
