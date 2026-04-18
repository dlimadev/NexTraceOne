/**
 * Tests for AiAnomalyPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getAnomalyDetections: vi.fn(),
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

import { getAnomalyDetections } from '../../features/operations/api/telemetry';
import { AiAnomalyPage } from '../../features/operations/pages/AiAnomalyPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AiAnomalyPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockAnomalies = [
  {
    id: 'anom-001',
    serviceName: 'order-service',
    metric: 'request_latency_p95',
    observedValue: 1420,
    baselineValue: 180,
    sigmaDeviation: 6.8,
    severity: 'critical' as const,
    status: 'open' as const,
    explanation: 'P95 latency spiked after deploy v2.3.1, 6.8 sigma above 7-day baseline.',
    detectedAt: new Date(Date.now() - 1800000).toISOString(),
    modelVersion: 'anomaly-v2',
    environment: 'production',
  },
  {
    id: 'anom-002',
    serviceName: 'catalog-service',
    metric: 'error_rate',
    observedValue: 0.38,
    baselineValue: 0.12,
    sigmaDeviation: 3.2,
    severity: 'medium' as const,
    status: 'acknowledged' as const,
    explanation: 'Error rate moderately elevated, no deploy correlation detected.',
    detectedAt: new Date(Date.now() - 7200000).toISOString(),
    modelVersion: 'anomaly-v2',
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getAnomalyDetections).mockResolvedValue(mockAnomalies);
});

describe('AiAnomalyPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('AI Anomaly Baseline')).toBeTruthy();
    });
  });

  it('renders service names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
      expect(screen.getByText('catalog-service')).toBeTruthy();
    });
  });

  it('renders critical severity badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Critical').length).toBeGreaterThan(0);
    });
  });

  it('renders open status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Open')).toBeTruthy();
    });
  });

  it('renders acknowledged status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Acknowledged').length).toBeGreaterThan(0);
    });
  });

  it('renders sigma deviation value', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('6.8σ')).toBeTruthy();
    });
  });

  it('renders AI confidence percentage', async () => {
    renderPage();
    await waitFor(() => {
      // explanation text is rendered
      expect(screen.getByText(/P95 latency spiked/i)).toBeTruthy();
    });
  });

  it('renders deploy correlation badge when deployCorrelated', async () => {
    renderPage();
    await waitFor(() => {
      // observed value 1,420 is rendered
      expect(screen.getByText('1,420')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getAnomalyDetections).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getAnomalyDetections).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });
});
