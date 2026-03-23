import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getControlsSummary: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
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
    render(<MemoryRouter><EnterpriseControlsPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.controls.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><EnterpriseControlsPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('82%')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getControlsSummary).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><EnterpriseControlsPage /></MemoryRouter>);
    expect(screen.queryByText('governance.controls.title')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><EnterpriseControlsPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('governance.controls.title'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getControlsSummary on mount', async () => {
    render(<MemoryRouter><EnterpriseControlsPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.getControlsSummary).toHaveBeenCalledTimes(1));
  });
});
