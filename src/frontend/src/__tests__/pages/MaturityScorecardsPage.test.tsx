import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getMaturityScorecards: vi.fn(),
  },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { MaturityScorecardsPage } from '../../features/governance/pages/MaturityScorecardsPage';

const mockData = {
  scorecards: [
    {
      groupId: 'team-1',
      groupName: 'Order Squad',
      overallScore: 78,
      maturityLevel: 'Managed' as const,
      dimensions: [
        { dimension: 'ContractGovernance', score: 85, level: 'Optimizing' as const },
      ],
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

describe('MaturityScorecardsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getMaturityScorecards).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderWithProviders(<MaturityScorecardsPage />);
    await waitFor(() => {
      expect(screen.getByText('governance.executive.scorecardsTitle')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderWithProviders(<MaturityScorecardsPage />);
    await waitFor(() => {
      expect(screen.getByText('Order Squad')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getMaturityScorecards).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<MaturityScorecardsPage />);
    expect(screen.queryByText('Order Squad')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderWithProviders(<MaturityScorecardsPage />);
    await waitFor(() => screen.getByText('Order Squad'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getMaturityScorecards on mount', async () => {
    renderWithProviders(<MaturityScorecardsPage />);
    await waitFor(() => expect(organizationGovernanceApi.getMaturityScorecards).toHaveBeenCalledTimes(1));
  });
});
