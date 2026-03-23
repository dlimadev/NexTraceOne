import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getRiskHeatmap: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
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

describe('RiskHeatmapPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getRiskHeatmap).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    render(<MemoryRouter><RiskHeatmapPage /></MemoryRouter>);
    expect(screen.getByText('governance.executive.heatmapTitle')).toBeInTheDocument();
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><RiskHeatmapPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('Contract Governance')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getRiskHeatmap).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><RiskHeatmapPage /></MemoryRouter>);
    expect(screen.queryByText('Contract Governance')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><RiskHeatmapPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('Contract Governance'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getRiskHeatmap on mount with category dimension', async () => {
    render(<MemoryRouter><RiskHeatmapPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.getRiskHeatmap).toHaveBeenCalledWith('category'));
  });
});
