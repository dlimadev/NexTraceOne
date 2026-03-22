import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { DomainFinOpsPage } from '../../features/governance/pages/DomainFinOpsPage';

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
  domainId: 'domain-1',
  domainName: 'Commerce',
  totalCost: 64800,
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
    reliabilityCorrelation: null,
  }],
  topWasteSignals: [],
  optimizationOpportunities: [],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/governance/finops/domains/domain-1']}>
        <Routes>
          <Route path="/governance/finops/domains/:domainId" element={<DomainFinOpsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DomainFinOpsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(finOpsApi.getDomainFinOps).mockResolvedValue(mockData);
  });

  it('calls finOpsApi.getDomainFinOps on mount', async () => {
    renderPage();
    await waitFor(() => expect(finOpsApi.getDomainFinOps).toHaveBeenCalledWith('domain-1'));
  });

  it('renders domain name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Commerce')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(finOpsApi.getDomainFinOps).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Commerce')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(finOpsApi.getDomainFinOps).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Commerce')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Commerce'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
