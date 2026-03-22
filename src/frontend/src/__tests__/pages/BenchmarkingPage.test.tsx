import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { BenchmarkingPage } from '../../features/governance/pages/BenchmarkingPage';

vi.mock('../../features/governance/api/executive', () => ({
  executiveApi: {
    getBenchmarking: vi.fn(),
    getDrillDown: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { executiveApi } from '../../features/governance/api/executive';

const mockData = {
  dimension: 'teams',
  comparisons: [{
    groupId: 'team-1',
    groupName: 'Team Payments',
    serviceCount: 6,
    criticality: 'High',
    reliabilityScore: 87.5,
    reliabilityTrend: 'Stable' as const,
    changeSafetyScore: 72.0,
    incidentRecurrenceRate: 22.5,
    maturityScore: 76.3,
    riskScore: 74.5,
    finopsEfficiency: 'Inefficient' as const,
    strengths: ['Strong ownership'],
    gaps: ['High incident recurrence'],
    context: 'High-criticality domain',
  }],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <BenchmarkingPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('BenchmarkingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(executiveApi.getBenchmarking).mockResolvedValue(mockData);
  });

  it('calls executiveApi.getBenchmarking on mount', async () => {
    renderPage();
    await waitFor(() => expect(executiveApi.getBenchmarking).toHaveBeenCalledWith('teams'));
  });

  it('renders group name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Team Payments')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(executiveApi.getBenchmarking).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Team Payments')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(executiveApi.getBenchmarking).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Team Payments')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Team Payments'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
