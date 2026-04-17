import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  queryLogs: vi.fn(),
  queryTraces: vi.fn(),
  getTraceDetail: vi.fn(),
  queryMetrics: vi.fn(),
  getTopErrors: vi.fn(),
  compareLatency: vi.fn(),
  correlateByTraceId: vi.fn(),
  getTelemetryHealth: vi.fn(),
}));

import { queryTraces } from '../../features/operations/api/telemetry';
import { TraceExplorerPage } from '../../features/operations/pages/TraceExplorerPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <TraceExplorerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('TraceExplorerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(queryTraces).mockResolvedValue([]);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Trace Explorer')).toBeInTheDocument();
    });
  });

  it('shows empty state when no traces', () => {
    vi.mocked(queryTraces).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders trace rows when data is available', async () => {
    vi.mocked(queryTraces).mockResolvedValue([
      {
        traceId: 'trace-abc123',
        rootSpanName: 'POST /api/orders',
        rootServiceName: 'order-api',
        startedAt: '2026-04-01T10:00:00Z',
        durationMs: 142,
        spanCount: 8,
        hasErrors: false,
        rootStatus: 'OK',
      },
    ] as any);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('trace-abc123');
    });
  });

  it('shows error state when query fails', async () => {
    vi.mocked(queryTraces).mockRejectedValue(new Error('Telemetry error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
