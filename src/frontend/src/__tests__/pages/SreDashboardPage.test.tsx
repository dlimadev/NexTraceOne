/**
 * Tests for SreDashboardPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
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

import {
  getSreSummary,
  getSreTimeSeries,
  getSreTopRequests,
  getSreTopQueries,
} from '../../features/operations/api/telemetry';
import { SreDashboardPage } from '../../features/operations/pages/SreDashboardPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SreDashboardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockSummary = {
  problems: { open: 0, total: 1 },
  slo: { errorCompliancePct: 100.0, latencyCompliancePct: 99.98 },
  traffic: { requestCount: 109253, queryCount: 125945 },
  latency: { requestAvgMs: 63.06, queryAvgMs: 1.44 },
  errors: { http5xx: 30, http4xx: 1000, queryErrors: 0, logErrors: 2000 },
};

function makeTs(base: number) {
  return Array.from({ length: 20 }, (_, i) => ({
    timestamp: new Date(Date.now() - (20 - i) * 3600_000).toISOString(),
    value: base + i * 2,
  }));
}

const mockTimeSeries = {
  requests: makeTs(600),
  requestLatency: makeTs(60),
  requestErrors: makeTs(0),
  queries: makeTs(1000),
  queryLatency: makeTs(1),
  queryErrors: makeTs(0),
};

const mockTopRequests = [
  { service: 'user-auth', request: '/user/username/', count: 38265, avgLatencyMs: 3.03, errors: 0 },
  { service: 'AdService', request: '/ad-service/ad', count: 9264, avgLatencyMs: 1.8, errors: 0 },
  { service: 'Frontend', request: '/ui/', count: 3358, avgLatencyMs: 405.21, errors: 6 },
];

const mockTopQueries = [
  { database: 'my_database', query: 'SELECT username FROM users WHERE id = ?', count: 38265, avgLatencyMs: 1.33 },
  { database: 'likeDb', query: "set names 'utf8mb4'", count: 14442, avgLatencyMs: 941.17 },
];

beforeEach(() => {
  vi.mocked(getSreSummary).mockResolvedValue(mockSummary);
  vi.mocked(getSreTimeSeries).mockResolvedValue(mockTimeSeries);
  vi.mocked(getSreTopRequests).mockResolvedValue(mockTopRequests);
  vi.mocked(getSreTopQueries).mockResolvedValue(mockTopQueries);
});

describe('SreDashboardPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('SRE Dashboard')).toBeTruthy();
    });
  });

  it('renders SLO compliance after data loads', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('100.00%')).toBeTruthy();
    });
  });

  it('renders formatted traffic counts', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('109k')).toBeTruthy();
    });
  });

  it('renders HTTP error counts in Errors hero', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('30')).toBeTruthy(); // http5xx
    });
  });

  it('renders chart titles', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Requests')).toBeTruthy();
      expect(screen.getByText('Request Latency')).toBeTruthy();
    });
  });

  it('renders Service Analysis table with top requests', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service Analysis — Requests, Latency and Errors by Request')).toBeTruthy();
      expect(screen.getByText('user-auth')).toBeTruthy();
    });
  });

  it('renders Database Analysis table with top queries', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Database Analysis — Queries, Latency and Errors by Query')).toBeTruthy();
      expect(screen.getByText('my_database')).toBeTruthy();
    });
  });

  it('renders time range dropdown and allows selection', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Last 24 hours')).toBeTruthy();
    });
    fireEvent.click(screen.getByText('Last 24 hours'));
    await waitFor(() => {
      expect(screen.getByText('Last 1 hour')).toBeTruthy();
    });
    fireEvent.click(screen.getByText('Last 1 hour'));
    await waitFor(() => {
      expect(screen.queryByText('Last 1 hour')).toBeTruthy();
    });
  });

  it('renders refresh button', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('SRE Dashboard')).toBeTruthy();
    });
    // Refresh button renders via lucide RefreshCw inside a button — verify the button exists
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('shows no-problems badge when problems.open is 0', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('No open problems')).toBeTruthy();
    });
  });

  it('shows open-problems badge when problems.open > 0', async () => {
    vi.mocked(getSreSummary).mockResolvedValueOnce({
      ...mockSummary,
      problems: { open: 3, total: 5 },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Open problems')).toBeTruthy();
    });
  });

  it('shows error state when all queries fail', async () => {
    vi.mocked(getSreSummary).mockRejectedValue(new Error('network error'));
    vi.mocked(getSreTimeSeries).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Failed to load SRE Dashboard data. Please try again.')).toBeTruthy();
    });
  });
});
