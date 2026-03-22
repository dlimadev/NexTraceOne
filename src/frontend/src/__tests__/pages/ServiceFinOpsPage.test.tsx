import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ServiceFinOpsPage } from '../../features/governance/pages/ServiceFinOpsPage';

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
  serviceId: 'svc-1',
  serviceName: 'Payment API',
  domain: 'Payments',
  team: 'Team Payments',
  efficiency: 'Inefficient' as const,
  monthlyCost: 12500,
  trend: 'Declining' as const,
  wasteSignals: [{ description: 'Excessive retries', pattern: 'retry-pattern', type: 'ExcessiveRetries', estimatedWaste: 3200, detectedAt: '2026-03-10T08:00:00Z' }],
  reliabilityCorrelation: { reliabilityScore: 72.5, recentIncidents: 3, reliabilityTrend: 'Declining' as const },
  optimizationOpportunities: [{ recommendation: 'Reduce retry backoff', potentialSavings: 1800, priority: 'High', rationale: 'Excessive retries' }],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/governance/finops/services/svc-1']}>
        <Routes>
          <Route path="/governance/finops/services/:serviceId" element={<ServiceFinOpsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceFinOpsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(finOpsApi.getServiceFinOps).mockResolvedValue(mockData);
  });

  it('calls finOpsApi.getServiceFinOps on mount', async () => {
    renderPage();
    await waitFor(() => expect(finOpsApi.getServiceFinOps).toHaveBeenCalledWith('svc-1'));
  });

  it('renders service name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Payment API')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(finOpsApi.getServiceFinOps).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Payment API')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(finOpsApi.getServiceFinOps).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Payment API')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Payment API'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
