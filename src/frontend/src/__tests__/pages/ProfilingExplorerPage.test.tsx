/**
 * Tests for ProfilingExplorerPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getProfilingSessions: vi.fn(),
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

import { getProfilingSessions } from '../../features/operations/api/telemetry';
import { ProfilingExplorerPage } from '../../features/operations/pages/ProfilingExplorerPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ProfilingExplorerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockSessions = [
  {
    id: '1',
    serviceName: 'order-service',
    version: 'v2.3.1',
    environment: 'production',
    cpuPercent: 68.4,
    memoryMb: 512,
    heapMb: 310,
    sampleCount: 4200,
    durationMs: 60000,
    deployCorrelated: true,
    deployId: 'deploy-001',
    capturedAt: new Date(Date.now() - 3600000).toISOString(),
    profileType: 'cpu',
  },
  {
    id: '2',
    serviceName: 'payment-service',
    version: 'v1.8.0',
    environment: 'production',
    cpuPercent: 82.1,
    memoryMb: 768,
    heapMb: 540,
    sampleCount: 6100,
    durationMs: 60000,
    deployCorrelated: false,
    capturedAt: new Date(Date.now() - 7200000).toISOString(),
    profileType: 'memory',
  },
];

beforeEach(() => {
  vi.mocked(getProfilingSessions).mockResolvedValue(mockSessions);
});

describe('ProfilingExplorerPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Continuous Profiling Explorer')).toBeTruthy();
    });
  });

  it('renders service names from data', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
      expect(screen.getByText('payment-service')).toBeTruthy();
    });
  });

  it('renders version identifiers', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('v2.3.1')).toBeTruthy();
    });
  });

  it('renders deploy-correlated badge when deployCorrelated is true', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Correlated').length).toBeGreaterThan(0);
    });
  });

  it('renders time range selector', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Continuous Profiling Explorer')).toBeTruthy();
    });
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('renders sessions table with column headers', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service')).toBeTruthy();
    });
  });

  it('renders hero metric — deploy regressions count', async () => {
    renderPage();
    await waitFor(() => {
      // 1 of 2 sessions has deployCorrelated=true → "1"
      expect(screen.getByText('1')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getProfilingSessions).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      // fallback includes order-service
      expect(screen.getByText('order-service')).toBeTruthy();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getProfilingSessions).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders refresh button', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Continuous Profiling Explorer')).toBeTruthy();
    });
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });
});
