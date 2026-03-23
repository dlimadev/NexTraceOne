import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/finOps', () => ({
  finOpsApi: {
    getSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en-US' },
  }),
}));

import { finOpsApi } from '../../features/governance/api/finOps';
import { ExecutiveFinOpsPage } from '../../features/governance/pages/ExecutiveFinOpsPage';

const mockData = {
  totalMonthlyCost: 125000,
  totalWaste: 18000,
  overallEfficiency: 'Acceptable' as const,
  services: [
    {
      serviceId: 'svc-1',
      serviceName: 'Order Processor',
      domain: 'Commerce',
      team: 'Order Squad',
      efficiency: 'Efficient' as const,
      monthlyCost: 8500,
      trend: 'Improving' as const,
      wasteSignals: [],
      reliabilityCorrelation: null,
    },
  ],
  optimizationOpportunities: [
    {
      serviceName: 'Payment Gateway',
      recommendation: 'Reduce idle replicas',
      potentialSavings: 3200,
    },
  ],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><ExecutiveFinOpsPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ExecutiveFinOpsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(finOpsApi.getSummary).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('governance.finops.executiveTitle')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Commerce')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(finOpsApi.getSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Commerce')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Commerce'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls finOpsApi.getSummary on mount', async () => {
    renderPage();
    await waitFor(() => expect(finOpsApi.getSummary).toHaveBeenCalled());
  });
});
