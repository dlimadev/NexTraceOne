import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listPolicies: vi.fn(),
  },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { PolicyCatalogPage } from '../../features/governance/pages/PolicyCatalogPage';

const mockData = {
  totalPolicies: 5,
  activeCount: 3,
  draftCount: 2,
  policies: [
    {
      policyId: 'pol-1',
      name: 'svc-ownership',
      displayName: 'Service Ownership Required',
      description: 'All services must have an owner',
      category: 'ServiceGovernance' as const,
      status: 'Active' as const,
      severity: 'High',
      enforcementMode: 'Enforced',
      scope: 'Organization',
      effectiveEnvironments: ['Production'],
      affectedAssetsCount: 12,
      violationCount: 3,
    },
  ],
};

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
describe('PolicyCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.listPolicies).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderWithProviders(<PolicyCatalogPage />);
    await waitFor(() => {
      expect(screen.getByText('Policy Catalog')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderWithProviders(<PolicyCatalogPage />);
    await waitFor(() => {
      expect(screen.getByText('Service Ownership Required')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.listPolicies).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<PolicyCatalogPage />);
    expect(screen.queryByText('Service Ownership Required')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderWithProviders(<PolicyCatalogPage />);
    await waitFor(() => screen.getByText('Service Ownership Required'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls listPolicies on mount', async () => {
    renderWithProviders(<PolicyCatalogPage />);
    await waitFor(() => expect(organizationGovernanceApi.listPolicies).toHaveBeenCalledTimes(1));
  });
});
