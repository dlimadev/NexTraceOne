import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getMaturityScorecards: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
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

describe('MaturityScorecardsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getMaturityScorecards).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    render(<MemoryRouter><MaturityScorecardsPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.executive.scorecardsTitle')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><MaturityScorecardsPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('Order Squad')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getMaturityScorecards).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><MaturityScorecardsPage /></MemoryRouter>);
    expect(screen.queryByText('Order Squad')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><MaturityScorecardsPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('Order Squad'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getMaturityScorecards on mount', async () => {
    render(<MemoryRouter><MaturityScorecardsPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.getMaturityScorecards).toHaveBeenCalledTimes(1));
  });
});
