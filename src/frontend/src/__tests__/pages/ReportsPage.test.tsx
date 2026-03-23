import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getReportsSummary: vi.fn(),
  },
}));

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: () => ({ persona: 'Engineer', config: {} }),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
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

describe('ReportsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getReportsSummary).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    render(<MemoryRouter><ReportsPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.reportsTitle')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><ReportsPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.reports.packCoverage')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getReportsSummary).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><ReportsPage /></MemoryRouter>);
    expect(screen.queryByText('governance.reports.packCoverage')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><ReportsPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('governance.reportsTitle'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls getReportsSummary on mount', async () => {
    render(<MemoryRouter><ReportsPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.getReportsSummary).toHaveBeenCalledTimes(1));
  });
});
