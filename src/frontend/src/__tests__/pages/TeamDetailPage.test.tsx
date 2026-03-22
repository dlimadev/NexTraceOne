import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { TeamDetailPage } from '../../features/governance/pages/TeamDetailPage';

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

const mockTeamDetail = {
  teamId: 'team-1',
  displayName: 'Order Squad',
  description: 'Handles order processing',
  parentOrganizationUnit: 'Engineering',
  status: 'Active' as const,
  maturityLevel: 'Managed' as const,
  serviceCount: 5,
  contractCount: 12,
  memberCount: 8,
  createdAt: '2026-01-01T00:00:00Z',
  activeIncidentCount: 0,
  reliabilityScore: 95,
  services: [],
  contracts: [],
  crossTeamDependencies: [],
};

const mockGovernanceSummary = {
  overallMaturity: 'Managed' as const,
  openRiskCount: 1,
  policyViolationCount: 0,
  ownershipCoverage: 95,
  contractCoverage: 88,
  documentationCoverage: 72,
  runbookCoverage: 60,
  dimensions: [],
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/governance/teams/team-1']}>
        <Routes>
          <Route path="/governance/teams/:teamId" element={<TeamDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TeamDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getTeamDetail).mockResolvedValue(mockTeamDetail);
    vi.mocked(organizationGovernanceApi.getTeamGovernanceSummary).mockResolvedValue(mockGovernanceSummary);
  });

  it('calls getTeamDetail with correct teamId', async () => {
    renderPage();
    await waitFor(() => expect(organizationGovernanceApi.getTeamDetail).toHaveBeenCalledWith('team-1'));
  });

  it('renders team name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order Squad')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getTeamDetail).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Order Squad')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(organizationGovernanceApi.getTeamDetail).mockRejectedValue(new Error('Not found'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Order Squad')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Order Squad'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
