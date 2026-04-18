/**
 * Tests for SloBurnRatePage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getSloBurnRates: vi.fn(),
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

import { getSloBurnRates } from '../../features/operations/api/telemetry';
import { SloBurnRatePage } from '../../features/operations/pages/SloBurnRatePage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SloBurnRatePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockBurnRates = [
  {
    id: 'slo-001',
    sloName: 'Order API Latency SLO',
    serviceName: 'order-service',
    budgetRemainingPercent: 42.3,
    burnRate1h: 8.4,
    burnRate6h: 4.2,
    burnRate24h: 2.1,
    burnRate72h: 1.8,
    depletedInHours: 18,
    alertThreshold: 1.5,
    status: 'critical' as const,
    environment: 'production',
  },
  {
    id: 'slo-002',
    sloName: 'Payment API Error Rate SLO',
    serviceName: 'payment-service',
    budgetRemainingPercent: 88.1,
    burnRate1h: 0.3,
    burnRate6h: 0.4,
    burnRate24h: 0.5,
    burnRate72h: 0.6,
    alertThreshold: 1.0,
    status: 'healthy' as const,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getSloBurnRates).mockResolvedValue(mockBurnRates);
});

describe('SloBurnRatePage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('SLO Error Budget Burn Rate')).toBeTruthy();
    });
  });

  it('renders SLO names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order API Latency SLO')).toBeTruthy();
      expect(screen.getByText('Payment API Error Rate SLO')).toBeTruthy();
    });
  });

  it('renders budget remaining percentage', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('42.3%')).toBeTruthy();
    });
  });

  it('renders alert firing badge', async () => {
    renderPage();
    await waitFor(() => {
      // budget depleted in 18h renders a warning badge
      expect(screen.getByText(/Budget depleted in 18h/i)).toBeTruthy();
    });
  });

  it('renders depletion projection', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/18h/)).toBeTruthy();
    });
  });

  it('renders fast burn rate column', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('8.4x')).toBeTruthy();
    });
  });

  it('renders service names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getSloBurnRates).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getSloBurnRates).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders alert threshold value', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('1.5x')).toBeTruthy();
    });
  });
});
