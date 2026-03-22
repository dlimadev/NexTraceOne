import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { TeamFinOpsPage } from '../../features/governance/pages/TeamFinOpsPage';

vi.mock('../../features/governance/api/finOps', () => ({
  finOpsApi: {
    getSummary: vi.fn(),
    getServiceFinOps: vi.fn(),
    getTeamFinOps: vi.fn(),
    getDomainFinOps: vi.fn(),
    getTrends: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { finOpsApi } from '../../features/governance/api/finOps';

const mockData = {
  teamId: 'team-1',
  teamName: 'Team Commerce',
  totalCost: 42800,
  efficiency: 'Inefficient' as const,
  trend: 'Declining' as const,
  services: [{
    serviceId: 'svc-1',
    serviceName: 'Order Processor',
    domain: 'Commerce',
    team: 'Team Commerce',
    efficiency: 'Wasteful' as const,
    monthlyCost: 18700,
    trend: 'Declining' as const,
    wasteSignals: [],
    reliabilityCorrelation: { reliabilityScore: 58.3, recentIncidents: 5, reliabilityTrend: 'Declining' as const },
  }],
  topWasteSignals: [],
  optimizationOpportunities: [],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/governance/finops/teams/team-1']}>
        <Routes>
          <Route path="/governance/finops/teams/:teamId" element={<TeamFinOpsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TeamFinOpsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(finOpsApi.getTeamFinOps).mockResolvedValue(mockData);
  });

  it('calls finOpsApi.getTeamFinOps on mount', async () => {
    renderPage();
    await waitFor(() => expect(finOpsApi.getTeamFinOps).toHaveBeenCalledWith('team-1'));
  });

  it('renders team name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Team Commerce')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(finOpsApi.getTeamFinOps).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Team Commerce')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(finOpsApi.getTeamFinOps).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Team Commerce')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Team Commerce'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
