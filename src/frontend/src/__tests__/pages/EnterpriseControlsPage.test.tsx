import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getControlsSummary: vi.fn(),
  },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { EnterpriseControlsPage } from '../../features/governance/pages/EnterpriseControlsPage';

const mockData = {
  overallCoverage: 82,
  overallMaturity: 'Managed' as const,
  dimensions: [
    {
      dimension: 'ContractGovernance' as const,
      coverage: 90,
      maturity: 'Optimizing' as const,
      trend: 'Improving' as const,
      packCount: 4,
      ruleCount: 20,
      compliancePercentage: 95,
    },
  ],
};

describe('EnterpriseControlsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getControlsSummary).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderWithProviders(<EnterpriseControlsPage />);
    await waitFor(() => {
      expect(screen.getByText('Control management')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderWithProviders(<EnterpriseControlsPage />);
    await waitFor(() => {
      expect(screen.getByText('82%')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getControlsSummary).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<EnterpriseControlsPage />);
    expect(screen.queryByText('Control management')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderWithProviders(<EnterpriseControlsPage />);
    await waitFor(() => screen.getByText('Control management'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getControlsSummary on mount', async () => {
    renderWithProviders(<EnterpriseControlsPage />);
    await waitFor(() => expect(organizationGovernanceApi.getControlsSummary).toHaveBeenCalledTimes(1));
  });
});
