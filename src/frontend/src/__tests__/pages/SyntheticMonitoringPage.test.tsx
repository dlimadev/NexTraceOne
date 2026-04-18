/**
 * Tests for SyntheticMonitoringPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getSyntheticProbes: vi.fn(),
  getSreSummary: vi.fn(),
  getSreTimeSeries: vi.fn(),
  getSreTopRequests: vi.fn(),
  getSreTopQueries: vi.fn(),
  queryLogs: vi.fn(),
  queryTraces: vi.fn(),
  getTraceDetail: vi.fn(),
  queryMetrics: vi.fn(),
  getTopErrors: vi.fn(),
  compareLatency: vi.fn(),
  correlateByTraceId: vi.fn(),
  getTelemetryHealth: vi.fn(),
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production' }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import { getSyntheticProbes } from '../../features/operations/api/telemetry';
import { SyntheticMonitoringPage } from '../../features/operations/pages/SyntheticMonitoringPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SyntheticMonitoringPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockProbes = [
  {
    id: 'probe-001',
    name: 'Order API Health Check',
    target: 'https://api.example.com/orders/health',
    type: 'httpSingle' as const,
    status: 'healthy' as const,
    uptimePercent: 99.97,
    lastResult: '200 OK — 142ms',
    schedule: '1m',
    lastCheck: new Date(Date.now() - 60000).toISOString(),
    contractValidation: 'pass' as const,
    environment: 'production',
  },
  {
    id: 'probe-002',
    name: 'Payment Multi-Step Flow',
    target: 'https://api.example.com/payment/checkout',
    type: 'httpMultiStep' as const,
    status: 'down' as const,
    uptimePercent: 97.3,
    lastResult: '503 Service Unavailable',
    schedule: '5m',
    lastCheck: new Date(Date.now() - 300000).toISOString(),
    contractValidation: 'fail' as const,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getSyntheticProbes).mockResolvedValue(mockProbes);
});

describe('SyntheticMonitoringPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Synthetic Monitoring')).toBeTruthy();
    });
  });

  it('renders probe names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order API Health Check')).toBeTruthy();
      expect(screen.getByText('Payment Multi-Step Flow')).toBeTruthy();
    });
  });

  it('renders passing status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Healthy').length).toBeGreaterThan(0);
    });
  });

  it('renders failing status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Down').length).toBeGreaterThan(0);
    });
  });

  it('renders uptime percentage', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('99.97%')).toBeTruthy();
    });
  });

  it('renders contract schema valid indicator', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Pass')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getSyntheticProbes).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getSyntheticProbes).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders target URL', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('https://api.example.com/orders/health')).toBeTruthy();
    });
  });

  it('renders probe type (http-single)', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('HTTP Single')).toBeTruthy();
    });
  });
});
