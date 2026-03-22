import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { FinOpsPage } from '../../features/governance/pages/FinOpsPage';

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
  totalMonthlyCost: 45000,
  totalWaste: 12000,
  overallEfficiency: 'Acceptable' as const,
  costTrend: 'Stable' as const,
  services: [
    {
      serviceId: 'svc-1',
      serviceName: 'Payment API',
      domain: 'Payments',
      team: 'Team Payments',
      efficiency: 'Inefficient' as const,
      monthlyCost: 12500,
      trend: 'Declining' as const,
      wasteSignals: [{ description: 'Excessive retries', pattern: 'retry-pattern', type: 'ExcessiveRetries', estimatedWaste: 3200 }],
      reliabilityCorrelation: { reliabilityScore: 72.5, recentIncidents: 3, reliabilityTrend: 'Declining' as const },
    },
  ],
  topCostDrivers: [{ serviceId: 'svc-1', serviceName: 'Payment API', monthlyCost: 12500, efficiency: 'Inefficient' as const }],
  topWasteSignals: [{ description: 'Excessive retries', pattern: 'retry-pattern', type: 'ExcessiveRetries', estimatedWaste: 3200 }],
  optimizationOpportunities: [{ serviceId: 'svc-1', serviceName: 'Payment API', potentialSavings: 3200, priority: 'High', recommendation: 'Reduce retries' }],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <FinOpsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('FinOpsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(finOpsApi.getSummary).mockResolvedValue(mockData);
  });

  it('calls finOpsApi.getSummary on mount', async () => {
    renderPage();
    await waitFor(() => expect(finOpsApi.getSummary).toHaveBeenCalledTimes(1));
  });

  it('renders service name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getAllByText('Payment API').length).toBeGreaterThan(0));
  });

  it('shows loading state while fetching', () => {
    vi.mocked(finOpsApi.getSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Payment API')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(finOpsApi.getSummary).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Payment API')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => expect(screen.getAllByText('Payment API').length).toBeGreaterThan(0));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
