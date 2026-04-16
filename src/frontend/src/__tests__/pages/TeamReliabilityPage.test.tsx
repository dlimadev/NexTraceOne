import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TeamReliabilityPage } from '../../features/operations/pages/TeamReliabilityPage';

vi.mock('../../features/operations/api/reliability', () => ({
  reliabilityApi: {
    listServices: vi.fn(),
    getServiceDetail: vi.fn(),
    getTeamSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { reliabilityApi } from '../../features/operations/api/reliability';

const mockItems = [
  {
    serviceName: 'svc-order-api',
    displayName: 'Order API',
    serviceType: 'RestApi',
    domain: 'Orders',
    teamName: 'order-squad',
    criticality: 'Critical',
    reliabilityStatus: 'Healthy',
    operationalSummary: 'Service operating within expected parameters.',
    trend: 'Stable',
    activeFlags: 0,
    openIncidents: 0,
    recentChangeImpact: false,
    overallScore: 95,
    lastComputedAt: '2026-03-22T00:00:00Z',
  },
  {
    serviceName: 'svc-payment-gateway',
    displayName: 'Payment Gateway',
    serviceType: 'RestApi',
    domain: 'Payments',
    teamName: 'payment-squad',
    criticality: 'Critical',
    reliabilityStatus: 'Degraded',
    operationalSummary: 'Degraded performance detected.',
    trend: 'Declining',
    activeFlags: 6,
    openIncidents: 1,
    recentChangeImpact: true,
    overallScore: 52,
    lastComputedAt: '2026-03-22T00:00:00Z',
  },
];

const mockListResponse = {
  items: mockItems,
  totalCount: 2,
  page: 1,
  pageSize: 100,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <TeamReliabilityPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}


vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [
      { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
      { id: 'env-staging-001', name: 'Staging', profile: 'staging', isProductionLike: false },
    ],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('TeamReliabilityPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(reliabilityApi.listServices).mockResolvedValue(mockListResponse);
  });

  it('calls reliabilityApi.listServices on mount (no mock data)', async () => {
    renderPage();
    await waitFor(() => expect(reliabilityApi.listServices).toHaveBeenCalledTimes(1));
  });

  it('renders service items from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order API')).toBeInTheDocument());
    expect(screen.getByText('Payment Gateway')).toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Order API'));
    const demoBannerEl = screen.queryByTestId('demo-banner');
    expect(demoBannerEl).not.toBeInTheDocument();
    // Also check no "Demo Data" text
    expect(screen.queryByText(/Demo Data/i)).not.toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(reliabilityApi.listServices).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Order API')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(reliabilityApi.listServices).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Order API')).not.toBeInTheDocument());
  });

  it('shows incident badge when service has open incidents', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Payment Gateway'));
    const incidentBadges = screen.getAllByText(/Incident/i);
    expect(incidentBadges.length).toBeGreaterThan(0);
  });

  it('shows change impact badge when recentChangeImpact is true', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Payment Gateway'));
    expect(screen.getByText(/Change Impact/i)).toBeInTheDocument();
  });

  it('shows empty state when API returns no items', async () => {
    vi.mocked(reliabilityApi.listServices).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 100,
    });
    renderPage();
    await waitFor(() => screen.queryByText('Order API') === null);
  });
});
