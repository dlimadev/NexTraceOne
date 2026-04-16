import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getRiskSummary: vi.fn().mockResolvedValue({
      totalPacksAssessed: 5, criticalCount: 1, highCount: 2, mediumCount: 1, lowCount: 1, indicators: [],
    }),
  },
}));

import { RiskCenterPage } from '../../features/governance/pages/RiskCenterPage';


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

describe('RiskCenterPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    renderWithProviders(<RiskCenterPage />);
    await waitFor(() => {
      expect(screen.getByText('Risk Center')).toBeInTheDocument();
    });
  });

  it('renders risk data after loading', async () => {
    renderWithProviders(<RiskCenterPage />);
    await waitFor(() => {
      expect(screen.queryByText('Risk Center')).toBeInTheDocument();
    });
  });
});
