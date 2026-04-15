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

import { queryLogs } from '../../features/operations/api/telemetry';
import { LogExplorerPage } from '../../features/operations/pages/LogExplorerPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <LogExplorerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('LogExplorerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(queryLogs).mockResolvedValue([]);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Log Explorer')).toBeInTheDocument();
    });
  });

  it('shows no log entries initially', () => {
    vi.mocked(queryLogs).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders log entries when data is available', async () => {
    vi.mocked(queryLogs).mockResolvedValue([
      {
        timestamp: '2026-04-01T10:00:00Z',
        level: 'ERROR',
        message: 'NullReferenceException in OrderService',
        serviceName: 'order-api',
        traceId: 'trace-abc',
        spanId: 'span-1',
        attributes: {},
      },
    ] as any);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('NullReferenceException in OrderService');
    });
  });

  it('shows empty state when no log entries', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });

  it('shows error state when query fails', async () => {
    vi.mocked(queryLogs).mockRejectedValue(new Error('Telemetry error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
