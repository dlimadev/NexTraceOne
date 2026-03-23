import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listPolicies: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
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

describe('PolicyCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.listPolicies).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    render(<MemoryRouter><PolicyCatalogPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.policies.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><PolicyCatalogPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('Service Ownership Required')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.listPolicies).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><PolicyCatalogPage /></MemoryRouter>);
    expect(screen.queryByText('Service Ownership Required')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><PolicyCatalogPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('Service Ownership Required'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls listPolicies on mount', async () => {
    render(<MemoryRouter><PolicyCatalogPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.listPolicies).toHaveBeenCalledTimes(1));
  });
});
