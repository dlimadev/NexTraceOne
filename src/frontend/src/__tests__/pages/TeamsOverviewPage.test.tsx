import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TeamsOverviewPage } from '../../features/governance/pages/TeamsOverviewPage';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listTeams: vi.fn(),
    getTeamDetail: vi.fn(),
    getTeamGovernanceSummary: vi.fn(),
    listDomains: vi.fn(),
    listGovernancePacks: vi.fn(),
    getGovernancePack: vi.fn(),
    listWaivers: vi.fn(),
    getComplianceSummary: vi.fn(),
    getRiskSummary: vi.fn(),
    getExecutiveReport: vi.fn(),
    getMaturityScorecard: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';

const mockTeams = [
  {
    teamId: 'team-1',
    displayName: 'Order Squad',
    description: 'Handles order processing',
    parentOrganizationUnit: 'Engineering',
    status: 'Active',
    maturityLevel: 'Managed',
    serviceCount: 5,
    contractCount: 12,
    memberCount: 8,
  },
  {
    teamId: 'team-2',
    displayName: 'Payment Squad',
    description: 'Handles payments',
    parentOrganizationUnit: 'Engineering',
    status: 'Active',
    maturityLevel: 'Developing',
    serviceCount: 3,
    contractCount: 7,
    memberCount: 6,
  },
];

const mockResponse = {
  teams: mockTeams,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <TeamsOverviewPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TeamsOverviewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.listTeams).mockResolvedValue(mockResponse);
  });

  it('calls organizationGovernanceApi.listTeams on mount', async () => {
    renderPage();
    await waitFor(() => expect(organizationGovernanceApi.listTeams).toHaveBeenCalledTimes(1));
  });

  it('renders team items from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order Squad')).toBeInTheDocument());
    expect(screen.getByText('Payment Squad')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.listTeams).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Order Squad')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(organizationGovernanceApi.listTeams).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Order Squad')).not.toBeInTheDocument());
  });

  it('shows empty state when API returns no items', async () => {
    vi.mocked(organizationGovernanceApi.listTeams).mockResolvedValue({
      teams: [],
    });
    renderPage();
    await waitFor(() => expect(screen.queryByText('Order Squad')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Order Squad'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
    expect(screen.queryByText(/Demo Data/i)).not.toBeInTheDocument();
  });
});
