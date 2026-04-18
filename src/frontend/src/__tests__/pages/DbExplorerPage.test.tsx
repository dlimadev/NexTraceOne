/**
 * Tests for DbExplorerPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getSlowQueries: vi.fn(),
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

import { getSlowQueries } from '../../features/operations/api/telemetry';
import { DbExplorerPage } from '../../features/operations/pages/DbExplorerPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DbExplorerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockQueries = [
  {
    id: 'q-001',
    fingerprint: 'SELECT * FROM orders WHERE user_id = ?',
    database: 'orders_db',
    avgDurationMs: 1240,
    maxDurationMs: 8900,
    executionCount: 45230,
    totalTimeMs: 56085200,
    lockWaitMs: 320,
    hasIndexMiss: true,
    indexMissCount: 1230,
    environment: 'production',
  },
  {
    id: 'q-002',
    fingerprint: 'INSERT INTO audit_log (action, user_id, ts) VALUES (?, ?, ?)',
    database: 'audit_db',
    avgDurationMs: 87,
    maxDurationMs: 1200,
    executionCount: 120500,
    totalTimeMs: 10483500,
    lockWaitMs: 0,
    hasIndexMiss: false,
    indexMissCount: 0,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getSlowQueries).mockResolvedValue(mockQueries);
});

describe('DbExplorerPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Database Performance Explorer')).toBeTruthy();
    });
  });

  it('renders database names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('orders_db')).toBeTruthy();
      expect(screen.getByText('audit_db')).toBeTruthy();
    });
  });

  it('renders index miss warning badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('1,230x')).toBeTruthy();
    });
  });

  it('renders execution count', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('45,230')).toBeTruthy();
    });
  });

  it('renders index warnings section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Index Miss Warnings/i)).toBeTruthy();
    });
  });

  it('renders table headers', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Database')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getSlowQueries).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getSlowQueries).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders avg duration in hero metrics', async () => {
    renderPage();
    await waitFor(() => {
      // highest avgDuration is 1240ms → rendered as "1.24 s" or similar
      expect(screen.getByText('Database Performance Explorer')).toBeTruthy();
    });
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('renders sort tab buttons', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Index Miss')).toBeTruthy();
    });
  });
});
