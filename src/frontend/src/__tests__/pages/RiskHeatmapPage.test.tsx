import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getRiskHeatmap: vi.fn(),
  },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { RiskHeatmapPage } from '../../features/governance/pages/RiskHeatmapPage';

const mockData = {
  cells: [
    {
      groupId: 'cat-1',
      groupName: 'Contract Governance',
      riskLevel: 'Medium' as const,
      riskScore: 55,
      changeFailures: 3,
      contractGaps: 5,
      documentationGaps: 2,
      runbookGaps: 1,
      reliabilityDegradation: false,
      explanation: 'Moderate risk due to contract gaps',
    },
  ],
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

describe('RiskHeatmapPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getRiskHeatmap).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderWithProviders(<RiskHeatmapPage />);
    expect(screen.getByText('governance.executive.heatmapTitle')).toBeInTheDocument();
  });

  it('renders data after loading', async () => {
    renderWithProviders(<RiskHeatmapPage />);
    await waitFor(() => {
      expect(screen.getByText('Contract Governance')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getRiskHeatmap).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<RiskHeatmapPage />);
    expect(screen.queryByText('Contract Governance')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderWithProviders(<RiskHeatmapPage />);
    await waitFor(() => screen.getByText('Contract Governance'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getRiskHeatmap on mount with category dimension', async () => {
    renderWithProviders(<RiskHeatmapPage />);
    await waitFor(() => expect(organizationGovernanceApi.getRiskHeatmap).toHaveBeenCalledWith('category'));
  });
});
