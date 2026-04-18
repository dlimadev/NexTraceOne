/**
 * Tests for LoadTestingPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getLoadTestRuns: vi.fn(),
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

import { getLoadTestRuns } from '../../features/operations/api/telemetry';
import { LoadTestingPage } from '../../features/operations/pages/LoadTestingPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <LoadTestingPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockRuns = [
  {
    id: 'lt-001',
    name: 'Order Service Load Test v2.3.1',
    serviceName: 'order-service',
    source: 'k6' as const,
    status: 'passed' as const,
    vus: 500,
    durationMs: 300000,
    p95LatencyMs: 380,
    errorRate: 0.3,
    maxCapacityVus: 850,
    maxRps: 1200,
    executedAt: new Date(Date.now() - 3600000).toISOString(),
    environment: 'staging',
  },
  {
    id: 'lt-002',
    name: 'Payment Gateway Stress Test',
    serviceName: 'payment-service',
    source: 'gatling' as const,
    status: 'failed' as const,
    vus: 1000,
    durationMs: 600000,
    p95LatencyMs: 8900,
    errorRate: 12.4,
    executedAt: new Date(Date.now() - 7200000).toISOString(),
    environment: 'staging',
  },
];

beforeEach(() => {
  vi.mocked(getLoadTestRuns).mockResolvedValue(mockRuns);
});

describe('LoadTestingPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Load Test Correlation')).toBeTruthy();
    });
  });

  it('renders test run names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order Service Load Test v2.3.1')).toBeTruthy();
      expect(screen.getByText('Payment Gateway Stress Test')).toBeTruthy();
    });
  });

  it('renders passed status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Passed').length).toBeGreaterThan(0);
    });
  });

  it('renders failed status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Failed').length).toBeGreaterThan(0);
    });
  });

  it('renders tool name (k6)', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('k6')).toBeTruthy();
    });
  });

  it('renders virtual users count', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('500')).toBeTruthy();
    });
  });

  it('renders service names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getLoadTestRuns).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getLoadTestRuns).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders Total Runs hero stat', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Total Runs')).toBeTruthy();
    });
  });
});
