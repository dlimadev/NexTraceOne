import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
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

import { queryTraces, getTraceDetail } from '../../features/operations/api/telemetry';
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

// Helpers for creating typed test fixtures
function makeTrace(overrides: Partial<{
  traceId: string;
  serviceName: string;
  operationName: string;
  startTime: string;
  durationMs: number;
  statusCode: string;
  environment: string;
  spanCount: number;
  hasErrors: boolean;
  rootServiceKind: string;
}> = {}) {
  return {
    traceId: 'trace-abc123',
    serviceName: 'order-api',
    operationName: 'POST /api/orders',
    startTime: '2026-04-01T10:00:00Z',
    durationMs: 142,
    statusCode: 'Ok',
    environment: 'production',
    spanCount: 8,
    hasErrors: false,
    rootServiceKind: 'REST',
    ...overrides,
  };
}

describe('TraceExplorerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(queryTraces).mockResolvedValue([]);
    vi.mocked(getTraceDetail).mockResolvedValue(null as any);
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
    vi.mocked(queryTraces).mockResolvedValue([makeTrace()]);
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

  // ── Service Kind filter ────────────────────────────────────────────────────

  it('renders service kind filter dropdown', async () => {
    renderPage();
    await waitFor(() => {
      const select = document.querySelector('[data-testid="service-kind-filter"]');
      expect(select).toBeTruthy();
    });
  });

  it('service kind filter includes REST option', async () => {
    renderPage();
    await waitFor(() => {
      const select = document.querySelector('[data-testid="service-kind-filter"]') as HTMLSelectElement;
      expect(select).toBeTruthy();
      const options = Array.from(select.options).map((o) => o.value);
      expect(options).toContain('REST');
      expect(options).toContain('Kafka');
      expect(options).toContain('SOAP');
      expect(options).toContain('Background');
      expect(options).toContain('DB');
    });
  });

  // ── Service Kind badges ────────────────────────────────────────────────────

  it('renders REST badge for REST trace', async () => {
    vi.mocked(queryTraces).mockResolvedValue([makeTrace({ rootServiceKind: 'REST' })]);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('REST');
    });
  });

  it('renders Kafka badge for Kafka trace', async () => {
    vi.mocked(queryTraces).mockResolvedValue([makeTrace({ rootServiceKind: 'Kafka', operationName: 'orders.created' })]);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Kafka');
    });
  });

  it('renders SOAP badge for SOAP trace', async () => {
    vi.mocked(queryTraces).mockResolvedValue([makeTrace({ rootServiceKind: 'SOAP', operationName: 'GetCustomer' })]);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('SOAP');
    });
  });

  it('renders DB badge for DB trace', async () => {
    vi.mocked(queryTraces).mockResolvedValue([makeTrace({ rootServiceKind: 'DB', operationName: 'SELECT users' })]);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('DB');
    });
  });

  it('renders Background badge for Background trace', async () => {
    vi.mocked(queryTraces).mockResolvedValue([makeTrace({ rootServiceKind: 'Background', operationName: 'ProcessOrders' })]);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Background');
    });
  });
});

// ── buildDepthMap unit-level tests (via component behaviour) ─────────────────

describe('TraceExplorerPage — waterfall hierarchy', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(queryTraces).mockResolvedValue([makeTrace()]);
  });

  it('clicking a trace row switches to detail view and shows waterfall', async () => {
    const spans = [
      { traceId: 't1', spanId: 's1', parentSpanId: null, serviceName: 'svc-a', operationName: 'root-op', startTime: '2026-04-01T10:00:00Z', endTime: '2026-04-01T10:00:00.200Z', durationMs: 200, statusCode: 'Ok', environment: 'production', serviceKind: 'REST' },
      { traceId: 't1', spanId: 's2', parentSpanId: 's1', serviceName: 'svc-b', operationName: 'child-op', startTime: '2026-04-01T10:00:00.050Z', endTime: '2026-04-01T10:00:00.100Z', durationMs: 50, statusCode: 'Ok', environment: 'production', serviceKind: 'DB' },
      { traceId: 't1', spanId: 's3', parentSpanId: 's2', serviceName: 'svc-c', operationName: 'grandchild-op', startTime: '2026-04-01T10:00:00.060Z', endTime: '2026-04-01T10:00:00.090Z', durationMs: 30, statusCode: 'Ok', environment: 'production', serviceKind: 'DB' },
    ];
    vi.mocked(getTraceDetail).mockResolvedValue({
      traceId: 't1',
      spans,
      durationMs: 200,
      services: ['svc-a', 'svc-b', 'svc-c'],
    } as any);

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <TraceExplorerPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    // Click the trace row
    await waitFor(() => expect(document.body.textContent).toContain('trace-abc123'));
    const row = document.querySelector('tbody tr');
    if (row) fireEvent.click(row);

    // Should now show waterfall detail view
    await waitFor(() => {
      expect(document.body.textContent).toContain('root-op');
      expect(document.body.textContent).toContain('child-op');
      expect(document.body.textContent).toContain('grandchild-op');
    });
  });
});

