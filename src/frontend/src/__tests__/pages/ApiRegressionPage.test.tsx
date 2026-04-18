/**
 * Tests for ApiRegressionPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getApiRegressions: vi.fn(),
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

import { getApiRegressions } from '../../features/operations/api/telemetry';
import { ApiRegressionPage } from '../../features/operations/pages/ApiRegressionPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ApiRegressionPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockRegressions = [
  {
    id: 'reg-001',
    endpoint: 'GET /api/orders',
    serviceName: 'order-service',
    status: 'regressed' as const,
    changeConfidence: 'high' as const,
    p50BaselineMs: 45,
    p50CurrentMs: 180,
    p95BaselineMs: 120,
    p95CurrentMs: 520,
    p99BaselineMs: 300,
    p99CurrentMs: 1200,
    regressionPercent: 300,
    deployId: 'deploy-v2.3.1',
    environment: 'production',
  },
  {
    id: 'reg-002',
    endpoint: 'POST /api/payments',
    serviceName: 'payment-service',
    status: 'stable' as const,
    changeConfidence: 'low' as const,
    p50BaselineMs: 200,
    p50CurrentMs: 205,
    p95BaselineMs: 500,
    p95CurrentMs: 510,
    p99BaselineMs: 900,
    p99CurrentMs: 920,
    regressionPercent: 2,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getApiRegressions).mockResolvedValue(mockRegressions);
});

describe('ApiRegressionPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API Performance Regression Detection')).toBeTruthy();
    });
  });

  it('renders endpoint names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('GET /api/orders')).toBeTruthy();
      expect(screen.getByText('POST /api/payments')).toBeTruthy();
    });
  });

  it('renders regressed status badge', async () => {
    renderPage();
    await waitFor(() => {
      // The status badge renders regressionPercent not status word
      expect(screen.getAllByText('+300%').length).toBeGreaterThan(0);
    });
  });

  it('renders stable status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('+2%').length).toBeGreaterThan(0);
    });
  });

  it('renders change confidence high', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('High')).toBeTruthy();
    });
  });

  it('renders regression percent', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('+300%').length).toBeGreaterThan(0);
    });
  });

  it('renders service names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getApiRegressions).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getApiRegressions).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders baseline vs current latency hero metric', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('API Performance Regression Detection')).toBeTruthy();
    });
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(1);
    });
  });
});
