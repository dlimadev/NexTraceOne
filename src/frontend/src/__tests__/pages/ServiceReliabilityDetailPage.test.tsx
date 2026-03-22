import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ServiceReliabilityDetailPage } from '../../features/operations/pages/ServiceReliabilityDetailPage';

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

const mockDetail = {
  identity: {
    serviceId: 'svc-order-api',
    displayName: 'Order API',
    serviceType: 'RestApi',
    domain: 'Orders',
    teamName: 'order-squad',
    criticality: 'Critical',
  },
  status: 'Healthy',
  operationalSummary: 'Service operating within expected parameters.',
  trend: { direction: 'Stable', timeframe: '7d', summary: 'All indicators stable.' },
  metrics: { availabilityPercent: 99.95, latencyP99Ms: 45.2, errorRatePercent: 0.3, requestsPerSecond: 1250, queueLag: null, processingDelay: null },
  activeFlags: 0,
  recentChanges: [],
  linkedIncidents: [],
  dependencies: [],
  linkedContracts: [],
  runbooks: [],
  anomalySummary: 'No anomalies detected.',
  coverage: {
    hasOperationalSignals: true,
    hasRunbook: false,
    hasOwner: true,
    hasDependenciesMapped: false,
    hasRecentChangeContext: false,
    hasIncidentLinkage: false,
  },
};

function renderDetailPage(serviceId = 'svc-order-api') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/operations/reliability/${serviceId}`]}>
        <Routes>
          <Route path="/operations/reliability/:serviceId" element={<ServiceReliabilityDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceReliabilityDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(reliabilityApi.getServiceDetail).mockResolvedValue(mockDetail);
  });

  it('calls reliabilityApi.getServiceDetail with serviceId from route', async () => {
    renderDetailPage('svc-order-api');
    await waitFor(() => expect(reliabilityApi.getServiceDetail).toHaveBeenCalledWith('svc-order-api'));
  });

  it('renders service identity from API response', async () => {
    renderDetailPage();
    await waitFor(() => expect(screen.getByText('Order API')).toBeInTheDocument());
    expect(screen.getByText(/svc-order-api/)).toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderDetailPage();
    await waitFor(() => screen.getByText('Order API'));
    expect(screen.queryByText(/Demo Data/i)).not.toBeInTheDocument();
  });

  it('renders metrics from API response', async () => {
    renderDetailPage();
    await waitFor(() => screen.getByText('Order API'));
    expect(screen.getByText('99.95%')).toBeInTheDocument();
  });

  it('shows not-found state when API returns 404', async () => {
    vi.mocked(reliabilityApi.getServiceDetail).mockRejectedValue({
      response: { status: 404 },
    });
    renderDetailPage('svc-nonexistent');
    await waitFor(() => expect(screen.queryByText('Order API')).not.toBeInTheDocument());
  });

  it('renders operational summary from API', async () => {
    renderDetailPage();
    await waitFor(() => screen.getByText('Order API'));
    expect(screen.getByText('Service operating within expected parameters.')).toBeInTheDocument();
  });

  it('renders coverage indicators from API', async () => {
    renderDetailPage();
    await waitFor(() => screen.getByText('Order API'));
    expect(screen.getByText(/Signals/i)).toBeInTheDocument();
  });

  it('shows no incidents message when linkedIncidents is empty', async () => {
    renderDetailPage();
    await waitFor(() => screen.getByText('Order API'));
    expect(screen.getByText(/No incidents/i)).toBeInTheDocument();
  });
});
